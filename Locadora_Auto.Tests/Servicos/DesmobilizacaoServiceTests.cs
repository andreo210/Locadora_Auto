using Locadora_Auto.Application.Configuration.Ultils.NotificadorServices;
using Locadora_Auto.Application.Models.Dto;
using Locadora_Auto.Domain.Entidades;
using Locadora_Auto.Tests.Fabricas;
using Locadora_Auto.Tests.Fakes;
using Xunit;

namespace Locadora_Auto.Tests.Servicos
{
    /// <summary>
    /// A porta da RN-56.
    ///
    /// A guarda que só existe aqui é a do <b>contrato</b>, e ela é a razão de esta camada ter teste
    /// próprio: o status do veículo é um retrato de agora e não enxerga período. Um carro
    /// <c>Disponivel</c> hoje pode ter contrato vendido para a semana que vem, e desmobilizá-lo
    /// criaria cliente no balcão sem carro — a mesma falha que a RN-40 fecha do outro lado.
    /// </summary>
    public class DesmobilizacaoServiceTests
    {
        private sealed class Cenario
        {
            public required VeiculoService Service { get; init; }
            public required NotificadorService Notificador { get; init; }
            public required ArmazemFake Armazem { get; init; }
            public required Veiculo Veiculo { get; init; }
            public required Funcionario Funcionario { get; init; }
        }

        private static Cenario Montar()
        {
            var armazem = new ArmazemFake();

            var categoria = Fabrica.Categoria();
            armazem.Semear(categoria);

            var filial = Fabrica.Filial();
            armazem.Semear(filial);

            var funcionario = Fabrica.Funcionario();
            armazem.Semear(funcionario);

            var veiculo = Fabrica.Veiculo(categoria.Id, filial.IdFilial);
            armazem.Semear(veiculo);

            var notificador = new NotificadorService();

            return new Cenario
            {
                Service = Fabrica.VeiculoService(armazem, notificador),
                Notificador = notificador,
                Armazem = armazem,
                Veiculo = veiculo,
                Funcionario = funcionario
            };
        }

        private static DesmobilizarVeiculoDto Dto(Funcionario responsavel) => new()
        {
            Motivo = "idade e custo de manutenção",
            IdFuncionarioResponsavel = responsavel.IdFuncionario
        };

        [Fact]
        public async Task Desmobilizacao_valida_tira_o_carro_da_frota()
        {
            var cenario = Montar();

            var sucesso = await cenario.Service.DesmobilizarAsync(cenario.Veiculo.IdVeiculo, Dto(cenario.Funcionario));

            Assert.True(sucesso);
            Assert.False(cenario.Notificador.TemNotificacao());
            Assert.Equal(StatusVeiculo.Desmobilizado, cenario.Veiculo.Status);
            Assert.False(cenario.Veiculo.Ativo);
        }

        /// <summary>
        /// O caso que só o serviço enxerga: o carro está disponível <b>agora</b>, e mesmo assim não
        /// pode sair da frota.
        /// </summary>
        [Fact]
        public async Task Contrato_futuro_ja_vendido_impede_a_desmobilizacao()
        {
            var cenario = Montar();

            var contrato = Fabrica.Locacao(
                veiculo: Fabrica.Veiculo(placa: "OUT0R01"),
                dataInicio: DateTime.UtcNow.AddDays(10));

            // o contrato aponta para o veículo do cenário: é o que a consulta do serviço procura
            typeof(Locacao).GetProperty(nameof(Locacao.IdVeiculo))!
                .SetValue(contrato, cenario.Veiculo.IdVeiculo);

            cenario.Armazem.Semear(contrato);

            var sucesso = await cenario.Service.DesmobilizarAsync(cenario.Veiculo.IdVeiculo, Dto(cenario.Funcionario));

            Assert.False(sucesso);
            Assert.True(cenario.Notificador.TemNotificacao());
            Assert.Equal(StatusVeiculo.Disponivel, cenario.Veiculo.Status);
        }

        [Fact]
        public async Task Contrato_encerrado_nao_impede_a_desmobilizacao()
        {
            // o histórico não trava a baixa do ativo: o que trava é contrato não encerrado
            var cenario = Montar();

            var contrato = Fabrica.LocacaoFechada(veiculo: Fabrica.Veiculo(placa: "OUT0R02"));
            typeof(Locacao).GetProperty(nameof(Locacao.IdVeiculo))!
                .SetValue(contrato, cenario.Veiculo.IdVeiculo);

            cenario.Armazem.Semear(contrato);

            var sucesso = await cenario.Service.DesmobilizarAsync(cenario.Veiculo.IdVeiculo, Dto(cenario.Funcionario));

            Assert.True(sucesso);
            Assert.False(cenario.Notificador.TemNotificacao());
        }

        [Fact]
        public async Task Veiculo_locado_notifica_em_vez_de_estourar()
        {
            var cenario = Montar();
            cenario.Veiculo.Locar(Fabrica.Contrato());

            var sucesso = await cenario.Service.DesmobilizarAsync(cenario.Veiculo.IdVeiculo, Dto(cenario.Funcionario));

            Assert.False(sucesso);
            Assert.True(cenario.Notificador.TemNotificacao());
            Assert.Equal(StatusVeiculo.Locado, cenario.Veiculo.Status);
        }

        [Fact]
        public async Task Motivo_vazio_notifica()
        {
            var cenario = Montar();
            var dto = Dto(cenario.Funcionario);
            dto.Motivo = "  ";

            var sucesso = await cenario.Service.DesmobilizarAsync(cenario.Veiculo.IdVeiculo, dto);

            Assert.False(sucesso);
            Assert.True(cenario.Notificador.TemNotificacao());
        }

        [Fact]
        public async Task Responsavel_inexistente_notifica()
        {
            var cenario = Montar();
            var dto = Dto(cenario.Funcionario);
            dto.IdFuncionarioResponsavel = 999;

            var sucesso = await cenario.Service.DesmobilizarAsync(cenario.Veiculo.IdVeiculo, dto);

            Assert.False(sucesso);
            Assert.True(cenario.Notificador.TemNotificacao());
        }

        [Fact]
        public async Task Desmobilizar_de_novo_notifica_em_vez_de_estourar()
        {
            var cenario = Montar();
            await cenario.Service.DesmobilizarAsync(cenario.Veiculo.IdVeiculo, Dto(cenario.Funcionario));

            var segunda = await cenario.Service.DesmobilizarAsync(cenario.Veiculo.IdVeiculo, Dto(cenario.Funcionario));

            Assert.False(segunda);
            Assert.True(cenario.Notificador.TemNotificacao());
        }

        [Fact]
        public async Task Desmobilizado_sai_da_lista_de_disponiveis()
        {
            var cenario = Montar();

            await cenario.Service.DesmobilizarAsync(cenario.Veiculo.IdVeiculo, Dto(cenario.Funcionario));

            var disponiveis = await cenario.Service.ObterDisponiveisAsync();
            Assert.Empty(disponiveis);
        }
    }
}
