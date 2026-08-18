using Locadora_Auto.Application.Configuration.Ultils.NotificadorServices;
using Locadora_Auto.Application.Services.LocacaoServices;
using Locadora_Auto.Domain.Entidades;
using Locadora_Auto.Tests.Fabricas;
using Locadora_Auto.Tests.Fakes;
using Xunit;

namespace Locadora_Auto.Tests.Servicos
{
    /// <summary>
    /// RN-60, lado da varredura: <c>MarcarAtrasadasAsync</c>.
    ///
    /// A entidade já sabia virar <c>Atrasada</c> desde o bloco A1, e não tinha quem a chamasse —
    /// então a carteira de atraso era sempre zero. O que se fixa aqui é o recorte da varredura:
    /// ela pega <b>só</b> quem está com o carro na rua além do combinado, e nada mais. Pegar de
    /// menos deixa contrato sumido invisível; pegar de mais marca como atrasado quem já devolveu.
    /// </summary>
    public class VarreduraDeAtrasoTests
    {
        private sealed class Cenario
        {
            public required LocacaoService Service { get; init; }
            public required ArmazemFake Armazem { get; init; }
        }

        private static Cenario Montar()
        {
            var armazem = new ArmazemFake();

            armazem.Semear(Fabrica.Cliente());
            armazem.Semear(Fabrica.Categoria());
            armazem.Semear(Fabrica.Filial());
            armazem.Semear(Fabrica.Funcionario());

            var service = Fabrica.LocacaoService(armazem, new NotificadorService());

            return new Cenario { Service = service, Armazem = armazem };
        }

        /// <summary>Contrato com o carro na rua e fim previsto no passado.</summary>
        private static Locacao Vencida(ArmazemFake armazem, string placa)
        {
            var locacao = Fabrica.LocacaoEmAndamento(
                veiculo: Fabrica.Veiculo(placa: placa),
                dataInicio: DateTime.UtcNow.AddDays(-5),
                dataFimPrevista: DateTime.UtcNow.AddDays(-1));

            armazem.Semear(locacao);
            return locacao;
        }

        [Fact]
        public async Task Contrato_que_passou_do_fim_previsto_vira_atrasada()
        {
            var cenario = Montar();
            var locacao = Vencida(cenario.Armazem, "ABC1D23");

            var marcadas = await cenario.Service.MarcarAtrasadasAsync();

            Assert.Equal(1, marcadas);
            Assert.Equal(StatusLocacao.Atrasada, locacao.Status);
        }

        [Fact]
        public async Task Contrato_dentro_do_prazo_nao_e_tocado()
        {
            var cenario = Montar();

            var locacao = Fabrica.LocacaoEmAndamento(
                veiculo: Fabrica.Veiculo(placa: "XYZ9K88"),
                dataInicio: DateTime.UtcNow.AddDays(-1),
                dataFimPrevista: DateTime.UtcNow.AddDays(2));

            cenario.Armazem.Semear(locacao);

            var marcadas = await cenario.Service.MarcarAtrasadasAsync();

            Assert.Equal(0, marcadas);
            Assert.Equal(StatusLocacao.EmAndamento, locacao.Status);
        }

        [Fact]
        public async Task Contrato_ainda_no_balcao_nao_conta_como_atrasado()
        {
            // Criada é o contrato aberto sem vistoria de retirada: o carro não saiu, então não há
            // atraso de devolução — o que existe é contrato mal fechado no balcão, outro problema
            var cenario = Montar();

            var locacao = Fabrica.Locacao(
                veiculo: Fabrica.Veiculo(placa: "QRS4T56"),
                dataInicio: DateTime.UtcNow.AddDays(-5),
                dataFimPrevista: DateTime.UtcNow.AddDays(-1));

            cenario.Armazem.Semear(locacao);

            var marcadas = await cenario.Service.MarcarAtrasadasAsync();

            Assert.Equal(0, marcadas);
            Assert.Equal(StatusLocacao.Criada, locacao.Status);
        }

        [Fact]
        public async Task Varredura_e_idempotente()
        {
            // a segunda passada não pode recontar o que a primeira já marcou: o filtro é
            // EmAndamento, e o contrato já saiu dele
            var cenario = Montar();
            Vencida(cenario.Armazem, "ABC1D23");

            var primeira = await cenario.Service.MarcarAtrasadasAsync();
            var segunda = await cenario.Service.MarcarAtrasadasAsync();

            Assert.Equal(1, primeira);
            Assert.Equal(0, segunda);
        }

        [Fact]
        public async Task Varredura_sem_candidato_nao_grava()
        {
            var cenario = Montar();

            var marcadas = await cenario.Service.MarcarAtrasadasAsync();

            Assert.Equal(0, marcadas);
        }

        [Fact]
        public async Task Varias_locacoes_vencidas_sao_marcadas_na_mesma_passada()
        {
            var cenario = Montar();
            var primeira = Vencida(cenario.Armazem, "ABC1D23");
            var segunda = Vencida(cenario.Armazem, "XYZ9K88");

            var marcadas = await cenario.Service.MarcarAtrasadasAsync();

            Assert.Equal(2, marcadas);
            Assert.Equal(StatusLocacao.Atrasada, primeira.Status);
            Assert.Equal(StatusLocacao.Atrasada, segunda.Status);
        }
    }
}
