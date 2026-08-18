using Locadora_Auto.Application.Configuration.Ultils.NotificadorServices;
using Locadora_Auto.Application.Models.Dto;
using Locadora_Auto.Domain.Entidades;
using Locadora_Auto.Tests.Fabricas;
using Locadora_Auto.Tests.Fakes;
using Xunit;

namespace Locadora_Auto.Tests.Servicos
{
    /// <summary>
    /// A RN-37 pelo lado da leitura. A gravação da trilha já está coberta em
    /// <c>Dominio/MovimentoDoAtivoTests</c>; o que se verifica aqui é a consulta — sem ela a
    /// tabela de auditoria cresce sem que ninguém confira, e erro de gravação só apareceria meses
    /// depois, quando o indicador saísse errado.
    /// </summary>
    public class TrilhaDoAtivoTests
    {
        private sealed class Cenario
        {
            public required VeiculoService Service { get; init; }
            public required NotificadorService Notificador { get; init; }
            public required Veiculo Veiculo { get; init; }
            public required ArmazemFake Armazem { get; init; }
            public required Filial Filial { get; init; }
        }

        /// <summary>
        /// Um veículo que já percorreu o ciclo inteiro: cadastro → locado → preparação → oferta.
        ///
        /// O veículo é semeado <b>antes</b> das transições porque é o id dele que cada movimento
        /// carimba — semear depois deixaria a trilha inteira apontando para o veículo 0.
        /// </summary>
        private static Cenario Montar()
        {
            var armazem = new ArmazemFake();

            var categoria = Fabrica.Categoria();
            armazem.Semear(categoria);

            var filial = Fabrica.Filial();
            armazem.Semear(filial);

            var veiculo = Fabrica.Veiculo(categoria.Id, filial.IdFilial);
            armazem.Semear(veiculo);

            var contrato = Fabrica.Contrato();
            veiculo.Locar(contrato);
            veiculo.RegistrarDevolucao(16_000, filial.IdFilial, contrato);
            veiculo.LiberarDaPreparacao();

            Fabrica.SemearTrilha(armazem, veiculo);

            var notificador = new NotificadorService();

            var service = Fabrica.VeiculoService(armazem, notificador);

            return new Cenario
            {
                Service = service,
                Notificador = notificador,
                Veiculo = veiculo,
                Armazem = armazem,
                Filial = filial
            };
        }

        [Fact]
        public async Task Trilha_devolve_o_ciclo_inteiro_do_mais_recente_para_o_mais_antigo()
        {
            var cenario = Montar();

            var pagina = await cenario.Service.ObterMovimentosAsync(
                cenario.Veiculo.IdVeiculo,
                new ConsultaPaginadaRequest());

            Assert.Equal(4, pagina.Total);
            Assert.Equal(
                new[] { "Disponivel", "EmPreparacao", "Locado", "Disponivel" },
                pagina.Items.Select(m => m.StatusDestino));

            // a última linha da página é a primeira da vida do carro: o cadastro, único movimento
            // sem situação de origem
            Assert.Null(pagina.Items.Last().StatusOrigem);
            Assert.Equal(nameof(TipoDocumentoOrigem.Cadastro), pagina.Items.Last().TipoOrigem);

            // e a primeira é a liberação do pátio, que não tem documento — quem responde "quem
            // liberou" ali é o autor
            Assert.Equal(nameof(TipoDocumentoOrigem.Patio), pagina.Items.First().TipoOrigem);
            Assert.Null(pagina.Items.First().IdLocacaoOrigem);
        }

        [Fact]
        public async Task Trilha_nao_mistura_movimento_de_outro_veiculo()
        {
            var cenario = Montar();

            // segundo carro no mesmo armazém, com trilha própria
            var outro = Fabrica.Veiculo(1, cenario.Filial.IdFilial, placa: "XYZ9W88");
            cenario.Armazem.Semear(outro);
            outro.IniciarManutencao(TipoManutencao.Preventiva, "revisão");
            Fabrica.SemearTrilha(cenario.Armazem, outro);

            var pagina = await cenario.Service.ObterMovimentosAsync(
                cenario.Veiculo.IdVeiculo,
                new ConsultaPaginadaRequest());

            Assert.Equal(4, pagina.Total);
            Assert.All(pagina.Items, m => Assert.Equal(cenario.Veiculo.IdVeiculo, m.IdVeiculo));
        }

        [Fact]
        public async Task Trilha_filtra_por_tipo_de_documento_de_origem()
        {
            var cenario = Montar();

            var pagina = await cenario.Service.ObterMovimentosAsync(
                cenario.Veiculo.IdVeiculo,
                new ConsultaPaginadaRequest(),
                idTipoOrigem: (int)TipoDocumentoOrigem.Contrato);

            // abertura e devolução — as duas transições que o contrato autoriza
            Assert.Equal(2, pagina.Total);
            Assert.All(pagina.Items, m => Assert.Equal(nameof(TipoDocumentoOrigem.Contrato), m.TipoOrigem));
        }

        [Fact]
        public async Task Trilha_respeita_a_janela_de_datas()
        {
            var cenario = Montar();

            var futuro = await cenario.Service.ObterMovimentosAsync(
                cenario.Veiculo.IdVeiculo,
                new ConsultaPaginadaRequest(),
                de: DateTime.UtcNow.AddMinutes(5));

            Assert.Equal(0, futuro.Total);

            var passado = await cenario.Service.ObterMovimentosAsync(
                cenario.Veiculo.IdVeiculo,
                new ConsultaPaginadaRequest(),
                de: DateTime.UtcNow.AddMinutes(-5));

            Assert.Equal(4, passado.Total);
        }

        [Fact]
        public async Task Trilha_pagina_sem_perder_a_contagem_total()
        {
            var cenario = Montar();

            var pagina = await cenario.Service.ObterMovimentosAsync(
                cenario.Veiculo.IdVeiculo,
                new ConsultaPaginadaRequest { ItensPorPagina = 2 });

            Assert.Equal(2, pagina.Items.Count);
            Assert.Equal(4, pagina.Total);
            Assert.Equal(2, pagina.TotalPaginas);
        }

        [Fact]
        public async Task Trilha_de_veiculo_inexistente_notifica_em_vez_de_devolver_vazio_calado()
        {
            var cenario = Montar();

            var pagina = await cenario.Service.ObterMovimentosAsync(999, new ConsultaPaginadaRequest());

            // vazio e "não existe" são respostas diferentes: quem separa as duas é o notificador,
            // que o CustomResponse transforma em ProblemDetails
            Assert.True(cenario.Notificador.TemNotificacao());
            Assert.Equal(0, pagina.Total);
            Assert.Empty(pagina.Items);
        }
    }
}
