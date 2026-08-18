using Locadora_Auto.Application.Configuration.Ultils.NotificadorServices;
using Locadora_Auto.Domain.Entidades;
using Locadora_Auto.Tests.Fabricas;
using Locadora_Auto.Tests.Fakes;
using Xunit;

namespace Locadora_Auto.Tests.Servicos
{
    /// <summary>
    /// RN-45, parte automática: a varredura que solta o carro que o pátio esqueceu em preparação.
    ///
    /// A transição em si já está coberta em <c>Dominio/VeiculoTests</c> e o registro dela em
    /// <c>Dominio/MovimentoDoAtivoTests</c>. O que se verifica aqui é a decisão que só o serviço
    /// tem como tomar, porque depende de três fontes que o domínio não enxerga junto: quem está no
    /// pátio, qual o prazo da filial de cada um e quando cada carro entrou.
    ///
    /// Errar essa decisão tem dois desfechos ruins e opostos — soltar cedo devolve à oferta um
    /// carro sujo, e não soltar deixa a frota encolher em silêncio.
    /// </summary>
    public class LiberacaoAutomaticaPreparacaoTests
    {
        private sealed class Cenario
        {
            public required VeiculoService Service { get; init; }
            public required VeiculosRepositoryFake Veiculos { get; init; }
            public required NotificadorService Notificador { get; init; }
            public required ArmazemFake Armazem { get; init; }
        }

        private static Cenario Montar(ArmazemFake armazem)
        {
            var notificador = new NotificadorService();
            var veiculos = new VeiculosRepositoryFake(armazem);

            return new Cenario
            {
                Armazem = armazem,
                Notificador = notificador,
                Veiculos = veiculos,
                Service = Fabrica.VeiculoService(armazem, notificador, veiculos)
            };
        }

        /// <summary>
        /// Um carro devolvido e parado no pátio da filial, com a trilha já no armazém — é assim que
        /// a varredura o encontra em produção.
        /// </summary>
        /// <param name="entrouEm">
        /// Quando ele entrou em preparação. <c>null</c> deixa o instante real da transição, que é o
        /// caso "acabou de chegar".
        /// </param>
        /// <param name="comTrilha">
        /// <c>false</c> reproduz o carro que entrou no pátio antes da trilha da RN-37 existir: está
        /// em <c>EmPreparacao</c> e não tem carimbo nenhum de entrada.
        /// </param>
        private static Veiculo VeiculoNoPatio(
            ArmazemFake armazem,
            Filial filial,
            DateTime? entrouEm = null,
            bool ativo = true,
            bool comTrilha = true,
            string placa = "ABC1D23")
        {
            // semeado antes das transições: é o id dele que cada movimento seguinte carimba
            var veiculo = Fabrica.Veiculo(idFilial: filial.IdFilial, placa: placa);
            armazem.Semear(veiculo);

            var contrato = Fabrica.Contrato();
            veiculo.Locar(contrato);
            veiculo.RegistrarDevolucao(kmFinal: 16_000, idFilialDevolucao: filial.IdFilial, contrato);

            if (!ativo) veiculo.Desativar();

            if (!comTrilha) return veiculo;

            Fabrica.SemearTrilha(armazem, veiculo);

            if (entrouEm.HasValue)
                Fabrica.DatarMovimento(EntradaNoPatio(veiculo), entrouEm.Value);

            return veiculo;
        }

        private static MovimentoVeiculo EntradaNoPatio(Veiculo veiculo)
            => veiculo.Movimentos.Last(m => m.StatusDestino == StatusVeiculo.EmPreparacao);

        // ======================= o prazo decide =======================

        [Fact]
        public async Task Prazo_vencido_devolve_o_veiculo_a_oferta()
        {
            var armazem = new ArmazemFake();
            var filial = Fabrica.Filial(tempoPreparacaoMinutos: 120);
            armazem.Semear(filial);

            var veiculo = VeiculoNoPatio(armazem, filial, entrouEm: DateTime.UtcNow.AddMinutes(-121));
            var cenario = Montar(armazem);

            var resultado = await cenario.Service.LiberarPreparacoesVencidasAsync();

            Assert.Equal(1, resultado.Analisados);
            Assert.Equal(1, resultado.Liberados);
            Assert.Equal(0, resultado.AindaNoPrazo);

            Assert.Equal(StatusVeiculo.Disponivel, veiculo.Status);
            Assert.True(veiculo.Disponivel);
            Assert.Equal(1, cenario.Veiculos.Salvamentos);
        }

        [Fact]
        public async Task Veiculo_dentro_do_prazo_continua_no_patio()
        {
            var armazem = new ArmazemFake();
            var filial = Fabrica.Filial(tempoPreparacaoMinutos: 120);
            armazem.Semear(filial);

            var veiculo = VeiculoNoPatio(armazem, filial, entrouEm: DateTime.UtcNow.AddMinutes(-30));
            var cenario = Montar(armazem);

            var resultado = await cenario.Service.LiberarPreparacoesVencidasAsync();

            Assert.Equal(0, resultado.Liberados);
            Assert.Equal(1, resultado.AindaNoPrazo);

            Assert.Equal(StatusVeiculo.EmPreparacao, veiculo.Status);

            // nada mudou: gravar aqui seria escrita à toa a cada tick do agendador
            Assert.Equal(0, cenario.Veiculos.Salvamentos);
        }

        [Fact]
        public async Task Cada_filial_usa_o_proprio_prazo()
        {
            // é a razão de o parâmetro ser de filial e não da casa: o mesmo carro parado há uma
            // hora está atrasado no aeroporto com equipe dedicada e adiantado na loja de rua
            var armazem = new ArmazemFake();

            var lojaDeRua = Fabrica.Filial("Loja de Rua", tempoPreparacaoMinutos: 120);
            var aeroporto = Fabrica.Filial("Aeroporto", tempoPreparacaoMinutos: 30);
            armazem.Semear(lojaDeRua, aeroporto);

            var umaHoraAtras = DateTime.UtcNow.AddMinutes(-60);
            var naLoja = VeiculoNoPatio(armazem, lojaDeRua, umaHoraAtras, placa: "AAA1A11");
            var noAeroporto = VeiculoNoPatio(armazem, aeroporto, umaHoraAtras, placa: "BBB2B22");

            var cenario = Montar(armazem);

            var resultado = await cenario.Service.LiberarPreparacoesVencidasAsync();

            Assert.Equal(2, resultado.Analisados);
            Assert.Equal(1, resultado.Liberados);
            Assert.Equal(1, resultado.AindaNoPrazo);

            Assert.Equal(StatusVeiculo.EmPreparacao, naLoja.Status);
            Assert.Equal(StatusVeiculo.Disponivel, noAeroporto.Status);
        }

        [Fact]
        public async Task Carimbo_usado_e_o_da_entrada_mais_recente()
        {
            // o mesmo carro entra e sai do pátio a cada ciclo de locação. Olhar a primeira entrada
            // soltaria na hora um carro que acabou de chegar, só porque ele já esteve ali em março
            var armazem = new ArmazemFake();
            var filial = Fabrica.Filial(tempoPreparacaoMinutos: 120);
            armazem.Semear(filial);

            var veiculo = Fabrica.Veiculo(idFilial: filial.IdFilial);
            armazem.Semear(veiculo);

            var primeiroContrato = Fabrica.Contrato();
            veiculo.Locar(primeiroContrato);
            veiculo.RegistrarDevolucao(16_000, filial.IdFilial, primeiroContrato);
            veiculo.LiberarDaPreparacao();

            var segundoContrato = Fabrica.Contrato();
            veiculo.Locar(segundoContrato);
            veiculo.RegistrarDevolucao(17_000, filial.IdFilial, segundoContrato);

            Fabrica.SemearTrilha(armazem, veiculo);

            var entradas = veiculo.Movimentos
                .Where(m => m.StatusDestino == StatusVeiculo.EmPreparacao)
                .ToList();

            Fabrica.DatarMovimento(entradas[0], DateTime.UtcNow.AddDays(-10)); // ciclo antigo
            Fabrica.DatarMovimento(entradas[1], DateTime.UtcNow.AddMinutes(-10)); // acabou de chegar

            var cenario = Montar(armazem);

            var resultado = await cenario.Service.LiberarPreparacoesVencidasAsync();

            Assert.Equal(0, resultado.Liberados);
            Assert.Equal(StatusVeiculo.EmPreparacao, veiculo.Status);
        }

        // ======================= quem a varredura não toca =======================

        [Fact]
        public async Task Veiculo_fora_da_preparacao_nao_e_tocado()
        {
            // a varredura é do pátio: carro na rua não é solto pelo relógio, nem carro na oficina
            var armazem = new ArmazemFake();
            var filial = Fabrica.Filial(tempoPreparacaoMinutos: 0);
            armazem.Semear(filial);

            var disponivel = Fabrica.Veiculo(idFilial: filial.IdFilial, placa: "AAA1A11");
            armazem.Semear(disponivel);

            var locado = Fabrica.Veiculo(idFilial: filial.IdFilial, placa: "BBB2B22");
            armazem.Semear(locado);
            locado.Locar(Fabrica.Contrato());

            var naOficina = Fabrica.Veiculo(idFilial: filial.IdFilial, placa: "CCC3C33");
            armazem.Semear(naOficina);
            naOficina.IniciarManutencao(TipoManutencao.Revisao, "Revisão de 30 mil km");

            var cenario = Montar(armazem);

            var resultado = await cenario.Service.LiberarPreparacoesVencidasAsync();

            Assert.Equal(0, resultado.Analisados);
            Assert.Equal(0, resultado.Liberados);

            Assert.Equal(StatusVeiculo.Disponivel, disponivel.Status);
            Assert.Equal(StatusVeiculo.Locado, locado.Status);
            Assert.Equal(StatusVeiculo.EmManutencao, naOficina.Status);
            Assert.Equal(0, cenario.Veiculos.Salvamentos);
        }

        [Fact]
        public async Task Veiculo_inativo_sai_da_preparacao_para_indisponivel()
        {
            // RN-53: o prazo vence igual, mas a saída do pátio não repõe na oferta um carro que a
            // casa já tirou de circulação
            var armazem = new ArmazemFake();
            var filial = Fabrica.Filial(tempoPreparacaoMinutos: 120);
            armazem.Semear(filial);

            var veiculo = VeiculoNoPatio(
                armazem, filial, entrouEm: DateTime.UtcNow.AddMinutes(-200), ativo: false);

            var cenario = Montar(armazem);

            var resultado = await cenario.Service.LiberarPreparacoesVencidasAsync();

            Assert.Equal(1, resultado.Liberados);
            Assert.Equal(StatusVeiculo.Bloqueado, veiculo.Status);
            Assert.False(veiculo.Disponivel);
        }

        // ======================= trilha e ruído =======================

        [Fact]
        public async Task Liberacao_por_prazo_deixa_movimento_com_a_origem_prazo()
        {
            var armazem = new ArmazemFake();
            var filial = Fabrica.Filial(tempoPreparacaoMinutos: 120);
            armazem.Semear(filial);

            var veiculo = VeiculoNoPatio(armazem, filial, entrouEm: DateTime.UtcNow.AddMinutes(-200));
            var cenario = Montar(armazem);

            await cenario.Service.LiberarPreparacoesVencidasAsync();

            var movimento = veiculo.Movimentos.Last();
            Assert.Equal(StatusVeiculo.EmPreparacao, movimento.StatusOrigem);
            Assert.Equal(StatusVeiculo.Disponivel, movimento.StatusDestino);
            Assert.Equal(TipoDocumentoOrigem.Prazo, movimento.TipoOrigem);
        }

        [Fact]
        public async Task Veiculo_sem_carimbo_de_entrada_e_liberado_e_contado_a_parte()
        {
            // carro que entrou no pátio antes da RN-37: sem carimbo, mas parado desde antes da
            // implantação — o prazo venceu por construção. Deixá-lo ali seria condená-lo a nunca
            // mais voltar à oferta, que é o defeito que esta varredura existe para fechar
            var armazem = new ArmazemFake();
            var filial = Fabrica.Filial(tempoPreparacaoMinutos: 120);
            armazem.Semear(filial);

            var veiculo = VeiculoNoPatio(armazem, filial, comTrilha: false);
            var cenario = Montar(armazem);

            var resultado = await cenario.Service.LiberarPreparacoesVencidasAsync();

            Assert.Equal(1, resultado.Liberados);
            Assert.Equal(1, resultado.SemCarimbo);
            Assert.Equal(StatusVeiculo.Disponivel, veiculo.Status);
        }

        [Fact]
        public async Task Varredura_nao_notifica()
        {
            // é lote disparado por agendador, não requisição: recusa individual aqui não teria a
            // quem responder, e uma notificação pendente contaminaria a resposta de quem viesse
            // depois no mesmo escopo
            var armazem = new ArmazemFake();
            var filial = Fabrica.Filial(tempoPreparacaoMinutos: 120);
            armazem.Semear(filial);

            VeiculoNoPatio(armazem, filial, entrouEm: DateTime.UtcNow.AddMinutes(-200));
            var cenario = Montar(armazem);

            await cenario.Service.LiberarPreparacoesVencidasAsync();

            Assert.False(cenario.Notificador.TemNotificacao());
        }

        [Fact]
        public async Task Patio_vazio_nao_grava_nada()
        {
            var armazem = new ArmazemFake();
            armazem.Semear(Fabrica.Filial());

            var cenario = Montar(armazem);

            var resultado = await cenario.Service.LiberarPreparacoesVencidasAsync();

            Assert.Equal(0, resultado.Analisados);
            Assert.Equal(0, cenario.Veiculos.Salvamentos);
        }
    }
}
