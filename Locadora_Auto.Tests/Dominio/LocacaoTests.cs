using Locadora_Auto.Domain.Entidades;
using Locadora_Auto.Tests.Fabricas;
using Xunit;

namespace Locadora_Auto.Tests.Dominio
{
    /// <summary>
    /// Locação é a raiz do agregado que contém multa, caução, vistoria, seguro e adicional —
    /// todos com <c>Criar</c> internal, criados só por um método da locação. Os testes por isso
    /// entram sempre pela raiz, que é a mesma porta que os serviços usam.
    ///
    /// Duas regras aparecem repetidas aqui porque são as que o agregado existe para proteger:
    /// a ordem do ciclo de vida (multa só depois da devolução, vistoria só antes dela) e o efeito
    /// da locação sobre o veículo — criar tira da oferta, finalizar devolve, dano em vistoria
    /// manda para manutenção.
    /// </summary>
    public class LocacaoTests
    {
        /// <summary>
        /// Contrato do começo ao fim: retirada, devolução e conta fechada sem saldo a cobrar — ou
        /// seja, <c>Finalizada</c>. É o pré-requisito das multas e o ponto a partir do qual o
        /// contrato para de aceitar alteração.
        /// </summary>
        private static Locacao Finalizada(Veiculo? veiculo = null)
            => Fabrica.LocacaoFechada(veiculo: veiculo);

        // ======================= criação =======================

        [Fact]
        public void Criar_nasce_criada_e_leva_o_veiculo_para_locado()
        {
            var veiculo = Fabrica.Veiculo();

            var locacao = Fabrica.Locacao(veiculo: veiculo);

            Assert.Equal(StatusLocacao.Criada, locacao.Status);
            Assert.Equal(StatusVeiculo.Locado, veiculo.Status);
            Assert.False(veiculo.Disponivel);
            Assert.Null(locacao.DataFimReal);
            Assert.Null(locacao.ValorFinal);
        }

        [Fact]
        public void Criar_a_partir_de_uma_reserva_finaliza_a_reserva()
        {
            var (cliente, reserva) = Fabrica.ClienteComReserva();

            Fabrica.Locacao(cliente: cliente, reserva: reserva);

            // a reserva virou locação: não pode continuar segurando vaga na frota
            Assert.Equal(StatusReserva.Finalizado, reserva.Status);
            Assert.False(reserva.Ativo);
        }

        [Fact]
        public void Criar_com_cnh_vencida_e_recusado()
        {
            var cliente = Fabrica.Cliente(validadeCnh: DateTime.Today.AddDays(-1));

            Assert.Throws<ArgumentNullException>(() => Fabrica.Locacao(cliente: cliente));
        }

        [Fact]
        public void Criar_para_cliente_bloqueado_e_recusado()
        {
            var cliente = Fabrica.Cliente();
            cliente.Bloquear();

            Assert.Throws<ArgumentNullException>(() => Fabrica.Locacao(cliente: cliente));
        }

        [Fact]
        public void Criar_com_veiculo_em_manutencao_e_recusado()
        {
            var veiculo = Fabrica.Veiculo();
            veiculo.IniciarManutencao(TipoManutencao.Corretiva, "Troca de embreagem");

            Assert.Throws<DomainException>(() => Fabrica.Locacao(veiculo: veiculo));
        }

        [Fact]
        public void Criar_com_veiculo_ja_locado_e_recusado()
        {
            var veiculo = Fabrica.Veiculo();
            Fabrica.Locacao(veiculo: veiculo);

            // a checagem de período sobreposto é do serviço; o que o domínio garante é que a placa
            // já consumida não é consumida de novo
            Assert.Throws<DomainException>(() => Fabrica.Locacao(veiculo: veiculo));
        }

        [Fact]
        public void Criar_com_veiculo_inativo_e_recusado()
        {
            var veiculo = Fabrica.Veiculo();
            veiculo.Desativar();

            Assert.Throws<DomainException>(() => Fabrica.Locacao(veiculo: veiculo));
        }

        [Fact]
        public void Criar_com_fim_previsto_anterior_ao_inicio_e_recusado()
        {
            var inicio = DateTime.UtcNow;

            Assert.Throws<InvalidOperationException>(() =>
                Fabrica.Locacao(dataInicio: inicio, dataFimPrevista: inicio.AddHours(-1)));
        }

        // ======================= retirada =======================

        [Fact]
        public void Vistoria_de_retirada_poe_o_carro_na_rua()
        {
            // RN-57: o contrato nasce com a placa comprometida, mas o carro só é liberado ao
            // cliente quando existe a vistoria que servirá de base de comparação na volta
            var locacao = Fabrica.Locacao();
            Assert.Equal(StatusLocacao.Criada, locacao.Status);

            Fabrica.Retirar(locacao);

            Assert.Equal(StatusLocacao.EmAndamento, locacao.Status);
        }

        [Fact]
        public void Segunda_vistoria_de_retirada_e_recusada()
        {
            var locacao = Fabrica.LocacaoEmAndamento();

            Assert.Throws<DomainException>(() => Fabrica.Retirar(locacao));
        }

        [Fact]
        public void Vistoria_de_devolucao_com_o_carro_ainda_no_patio_e_recusada()
        {
            var locacao = Fabrica.Locacao();

            Assert.Throws<DomainException>(() => locacao.RegistrarVistoria(
                3, TipoVistoria.Devolucao, NivelCombustivel.Meio, 15_400, null));
        }

        // ======================= devolução =======================

        [Fact]
        public void Devolver_encerra_a_posse_sem_encerrar_o_contrato()
        {
            var veiculo = Fabrica.Veiculo();
            var locacao = Fabrica.LocacaoEmAndamento(veiculo: veiculo, kmInicial: 15_000);

            Fabrica.Devolver(locacao, kmFinal: 15_400, filialDevolucao: 2);

            // RN-58: receber o carro não fecha a conta. Enquanto isso gravava Finalizada, tudo o
            // que a apuração cobraria se perdia com o contrato já constando concluído
            Assert.Equal(StatusLocacao.Devolvida, locacao.Status);
            Assert.Null(locacao.ValorFinal);
            Assert.Equal(15_400, locacao.KmFinal);
            Assert.Equal(2, locacao.IdFilialDevolucao);

            // o carro devolvido não está disponível ainda: entra na fila do pátio
            Assert.Equal(StatusVeiculo.EmPreparacao, veiculo.Status);
            Assert.False(veiculo.Disponivel);
        }

        [Fact]
        public void Devolver_sem_vistoria_de_retirada_e_recusado()
        {
            // RN-57: sem o par de vistorias não há base comparável, e nenhuma cobrança de avaria,
            // km ou combustível se sustenta numa contestação
            var locacao = Fabrica.Locacao();

            Assert.Throws<InvalidOperationException>(() =>
                locacao.RegistrarDevolucao(locacao.DataFimPrevista, 15_400, filialDevolucao: 1));
        }

        [Fact]
        public void Devolver_sem_vistoria_de_devolucao_e_recusado()
        {
            var locacao = Fabrica.LocacaoEmAndamento();

            Assert.Throws<InvalidOperationException>(() =>
                locacao.RegistrarDevolucao(locacao.DataFimPrevista, 15_400, filialDevolucao: 1));
        }

        [Fact]
        public void Devolver_avanca_o_odometro_e_a_filial_do_veiculo()
        {
            var veiculo = Fabrica.Veiculo(idFilial: 1);
            var locacao = Fabrica.LocacaoEmAndamento(veiculo: veiculo, kmInicial: 15_000, idFilialRetirada: 1);

            Fabrica.Devolver(locacao, kmFinal: 15_400, filialDevolucao: 7);

            Assert.Equal(15_400, veiculo.KmAtual);
            Assert.Equal(7, veiculo.FilialAtualId);
        }

        [Fact]
        public void Devolver_com_avaria_manda_o_veiculo_para_a_oficina()
        {
            var veiculo = Fabrica.Veiculo();
            var locacao = Fabrica.LocacaoEmAndamento(veiculo: veiculo);
            locacao.RegistrarVistoria(3, TipoVistoria.Devolucao, NivelCombustivel.Meio, 15_400, null);
            Fabrica.DefinirId(locacao.Vistorias.Single(v => v.Tipo == TipoVistoria.Devolucao), 8);
            locacao.RegistrarDanoVistoria(8, "Risco na porta direita", TipoDano.Risco, 350m);

            locacao.RegistrarDevolucao(locacao.DataFimPrevista, kmFinal: 15_400, filialDevolucao: 1);

            var manutencao = Assert.Single(veiculo.Manutencoes);
            Assert.Equal(TipoManutencao.Corretiva, manutencao.Tipo);
            Assert.Equal(StatusVeiculo.EmManutencao, veiculo.Status);
            Assert.False(veiculo.Disponivel);
        }

        [Fact]
        public void Devolver_sem_avaria_nao_abre_manutencao()
        {
            var veiculo = Fabrica.Veiculo();

            Fabrica.Devolver(Fabrica.LocacaoEmAndamento(veiculo: veiculo));

            Assert.Empty(veiculo.Manutencoes);
            Assert.Equal(StatusVeiculo.EmPreparacao, veiculo.Status);
        }

        [Fact]
        public void Devolver_em_filial_diferente_da_retirada_e_permitido()
        {
            var locacao = Fabrica.LocacaoEmAndamento(idFilialRetirada: 1);

            Fabrica.Devolver(locacao, filialDevolucao: 9);

            Assert.Equal(1, locacao.IdFilialRetirada);
            Assert.Equal(9, locacao.IdFilialDevolucao);
        }

        [Fact]
        public void Devolver_com_km_menor_que_o_inicial_e_recusado()
        {
            var locacao = Fabrica.LocacaoEmAndamento(kmInicial: 15_000);
            locacao.RegistrarVistoria(3, TipoVistoria.Devolucao, NivelCombustivel.Meio, 14_999, null);

            Assert.Throws<InvalidOperationException>(() =>
                locacao.RegistrarDevolucao(locacao.DataFimPrevista, kmFinal: 14_999, filialDevolucao: 1));
        }

        [Fact]
        public void Devolver_com_data_anterior_ao_inicio_e_recusado()
        {
            var locacao = Fabrica.LocacaoEmAndamento();
            locacao.RegistrarVistoria(3, TipoVistoria.Devolucao, NivelCombustivel.Meio, 15_400, null);

            Assert.Throws<InvalidOperationException>(() =>
                locacao.RegistrarDevolucao(locacao.DataInicio.AddHours(-1), 15_400, 1));
        }

        [Fact]
        public void Devolver_duas_vezes_e_recusado()
        {
            var locacao = Finalizada();

            Assert.Throws<InvalidOperationException>(() =>
                locacao.RegistrarDevolucao(locacao.DataFimPrevista, 15_500, 1));
        }

        // ======================= fechamento =======================

        [Fact]
        public void Fechar_sem_saldo_leva_o_contrato_a_finalizada()
        {
            var locacao = Fabrica.Devolver(Fabrica.LocacaoEmAndamento());

            locacao.Fechar(0m);
            locacao.LiquidarSaldo();

            Assert.Equal(StatusLocacao.Finalizada, locacao.Status);
            Assert.Equal(0m, locacao.SaldoEmAberto());
        }

        [Fact]
        public void Fechar_com_saldo_a_cobrar_leva_a_com_saldo_residual()
        {
            var locacao = Fabrica.Devolver(Fabrica.LocacaoEmAndamento());

            locacao.Fechar(520m);
            locacao.LiquidarSaldo();

            Assert.Equal(StatusLocacao.ComSaldoResidual, locacao.Status);
            Assert.Equal(520m, locacao.SaldoEmAberto());
        }

        [Fact]
        public void Pagamento_confirmado_quita_o_saldo_residual()
        {
            var locacao = Fabrica.Devolver(Fabrica.LocacaoEmAndamento());
            locacao.Fechar(520m);
            locacao.LiquidarSaldo();

            locacao.AdicionarPagamento(520m, FormaPagamento.Pix);
            Fabrica.DefinirId(locacao.Pagamentos.Single(), 11);
            locacao.ConfirmarPagamento(11);

            Assert.Equal(StatusLocacao.Finalizada, locacao.Status);
            Assert.Equal(0m, locacao.SaldoEmAberto());
        }

        [Fact]
        public void Pagamento_parcial_deixa_o_contrato_com_saldo_residual()
        {
            var locacao = Fabrica.Devolver(Fabrica.LocacaoEmAndamento());
            locacao.Fechar(520m);
            locacao.LiquidarSaldo();

            locacao.AdicionarPagamento(200m, FormaPagamento.Dinheiro);
            Fabrica.DefinirId(locacao.Pagamentos.Single(), 11);
            locacao.ConfirmarPagamento(11);

            Assert.Equal(StatusLocacao.ComSaldoResidual, locacao.Status);
            Assert.Equal(320m, locacao.SaldoEmAberto());
        }

        [Fact]
        public void Pagamento_pendente_nao_quita_o_contrato()
        {
            // RN-28: dinheiro que ainda não caiu não abate saldo
            var locacao = Fabrica.Devolver(Fabrica.LocacaoEmAndamento());
            locacao.Fechar(520m);
            locacao.AdicionarPagamento(520m, FormaPagamento.Boleto);

            locacao.LiquidarSaldo();

            Assert.Equal(StatusLocacao.ComSaldoResidual, locacao.Status);
        }

        [Fact]
        public void Fechar_locacao_que_nao_foi_devolvida_e_recusado()
        {
            var locacao = Fabrica.LocacaoEmAndamento();

            Assert.Throws<InvalidOperationException>(() => locacao.Fechar(520m));
        }

        [Fact]
        public void Fechar_duas_vezes_e_recusado()
        {
            // doc 07 §6: Fechada → Devolvida é proibido. Correção é lançamento novo, não reabertura
            var locacao = Fabrica.Devolver(Fabrica.LocacaoEmAndamento());
            locacao.Fechar(520m);

            Assert.Throws<InvalidOperationException>(() => locacao.Fechar(600m));
        }

        // ======================= cancelamento =======================

        [Fact]
        public void Cancelar_devolve_o_veiculo_para_a_oferta_sem_preparacao()
        {
            var veiculo = Fabrica.Veiculo();
            var locacao = Fabrica.Locacao(veiculo: veiculo);

            locacao.Cancelar();

            // o contrato foi anulado e o carro não rodou: não há o que preparar
            Assert.Equal(StatusLocacao.Cancelada, locacao.Status);
            Assert.Equal(StatusVeiculo.Disponivel, veiculo.Status);
            Assert.True(veiculo.Disponivel);
        }

        [Fact]
        public void Cancelar_com_o_carro_na_rua_e_recusado()
        {
            // RN-59: cancelamento depois da retirada apagaria o contrato e o quilômetro rodado
            // junto — com o carro fora, a saída é registrar a devolução
            var locacao = Fabrica.LocacaoEmAndamento();

            Assert.Throws<InvalidOperationException>(() => locacao.Cancelar());
        }

        [Fact]
        public void Cancelar_contrato_ja_devolvido_e_recusado()
        {
            var locacao = Fabrica.Devolver(Fabrica.LocacaoEmAndamento());

            Assert.Throws<InvalidOperationException>(() => locacao.Cancelar());
        }

        // ======================= atraso =======================

        [Fact]
        public void MarcarComoAtrasada_so_vale_depois_do_fim_previsto()
        {
            var inicio = DateTime.UtcNow;
            var locacao = Fabrica.LocacaoEmAndamento(dataInicio: inicio, dataFimPrevista: inicio.AddDays(3));

            locacao.MarcarComoAtrasada(inicio.AddDays(3));
            Assert.Equal(StatusLocacao.EmAndamento, locacao.Status);

            locacao.MarcarComoAtrasada(inicio.AddDays(4));
            Assert.Equal(StatusLocacao.Atrasada, locacao.Status);
        }

        [Fact]
        public void MarcarComoAtrasada_conta_por_instante_e_nao_por_dia()
        {
            // hora é dado contratual: contrato que vence às 09:00 e volta às 23:00 do mesmo dia é
            // atraso de 14 horas, e a comparação por .Date dizia que não era
            var inicio = new DateTime(2026, 3, 10, 9, 0, 0, DateTimeKind.Utc);
            var locacao = Fabrica.LocacaoEmAndamento(dataInicio: inicio, dataFimPrevista: inicio.AddDays(2));

            locacao.MarcarComoAtrasada(inicio.AddDays(2).AddHours(14));

            Assert.Equal(StatusLocacao.Atrasada, locacao.Status);
        }

        [Fact]
        public void Contrato_atrasado_ainda_pode_ser_devolvido()
        {
            // RN-60: era estado sem saída — o contrato que mais urge fechar era o único que não
            // fechava, porque a devolução exigia Criada
            var inicio = DateTime.UtcNow;
            var locacao = Fabrica.LocacaoEmAndamento(dataInicio: inicio, dataFimPrevista: inicio.AddDays(3));
            locacao.MarcarComoAtrasada(inicio.AddDays(4));

            Fabrica.Devolver(locacao, dataFimReal: inicio.AddDays(4));

            Assert.Equal(StatusLocacao.Devolvida, locacao.Status);
        }

        [Fact]
        public void MarcarComoAtrasada_nao_mexe_em_locacao_ja_devolvida()
        {
            var locacao = Finalizada();

            locacao.MarcarComoAtrasada(locacao.DataFimPrevista.AddDays(10));

            Assert.Equal(StatusLocacao.Finalizada, locacao.Status);
        }

        // ======================= multa =======================

        [Fact]
        public void Multa_antes_da_devolucao_e_recusada()
        {
            var locacao = Fabrica.Locacao();

            Assert.Throws<DomainException>(() => locacao.AdicionarMulta(TipoMulta.Atraso, 150m));
        }

        [Fact]
        public void AdicionarMulta_depois_da_devolucao_nasce_pendente()
        {
            var locacao = Finalizada();

            locacao.AdicionarMulta(TipoMulta.MultaTransito, 293.47m);

            var multa = Assert.Single(locacao.Multas);
            Assert.Equal(StatusMulta.Pendente, multa.Status);
            Assert.Equal(293.47m, multa.Valor);
            Assert.Equal(TipoMulta.MultaTransito, multa.Tipo);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-50)]
        public void AdicionarMulta_sem_valor_positivo_e_recusada(decimal valor)
        {
            var locacao = Finalizada();

            Assert.Throws<DomainException>(() => locacao.AdicionarMulta(TipoMulta.Limpeza, valor));
        }

        [Fact]
        public void PagarMulta_encerra_a_cobranca()
        {
            var locacao = Finalizada();
            locacao.AdicionarMulta(TipoMulta.Atraso, 120m);
            var multa = locacao.Multas.Single();
            Fabrica.DefinirId(multa, 3);

            locacao.PagarMulta(3);

            Assert.Equal(StatusMulta.Paga, multa.Status);
        }

        [Fact]
        public void PagarMulta_duas_vezes_e_recusado()
        {
            var locacao = Finalizada();
            locacao.AdicionarMulta(TipoMulta.Atraso, 120m);
            Fabrica.DefinirId(locacao.Multas.Single(), 3);
            locacao.PagarMulta(3);

            Assert.Throws<DomainException>(() => locacao.PagarMulta(3));
        }

        [Fact]
        public void PagarMulta_de_outra_locacao_e_recusado()
        {
            var locacao = Finalizada();
            locacao.AdicionarMulta(TipoMulta.Atraso, 120m);

            Assert.Throws<DomainException>(() => locacao.PagarMulta(999));
        }

        [Fact]
        public void CancelarMulta_ja_paga_e_recusado()
        {
            var locacao = Finalizada();
            locacao.AdicionarMulta(TipoMulta.Outros, 80m);
            Fabrica.DefinirId(locacao.Multas.Single(), 5);
            locacao.PagarMulta(5);

            Assert.Throws<DomainException>(() => locacao.CancelarMulta(5));
        }

        // ======================= caução =======================

        [Fact]
        public void RegistrarCaucao_nasce_pendente()
        {
            var locacao = Fabrica.Locacao();

            locacao.RegistrarCaucao(1_500m);

            var caucao = Assert.Single(locacao.Caucoes);
            Assert.Equal(1_500m, caucao.Valor);
            Assert.Equal(Caucao.StatusCaucao.Pendente, caucao.Status);
        }

        [Fact]
        public void DeduzirCaucao_baixa_o_saldo()
        {
            var locacao = Fabrica.Locacao();
            locacao.RegistrarCaucao(1_500m);
            var caucao = locacao.Caucoes.Single();
            Fabrica.DefinirId(caucao, 4);

            locacao.DeduzirCaucao(4, 400m);

            Assert.Equal(1_100m, caucao.Valor);
            Assert.Equal(Caucao.StatusCaucao.Pendente, caucao.Status);
        }

        [Fact]
        public void Deduzir_a_caucao_inteira_bloqueia_o_saldo()
        {
            var locacao = Fabrica.Locacao();
            locacao.RegistrarCaucao(1_000m);
            var caucao = locacao.Caucoes.Single();
            Fabrica.DefinirId(caucao, 4);

            locacao.DeduzirCaucao(4, 1_000m);

            Assert.Equal(0m, caucao.Valor);
            Assert.Equal(Caucao.StatusCaucao.Bloqueada, caucao.Status);
        }

        [Fact]
        public void Deduzir_acima_do_saldo_e_recusado()
        {
            var locacao = Fabrica.Locacao();
            locacao.RegistrarCaucao(1_000m);
            Fabrica.DefinirId(locacao.Caucoes.Single(), 4);

            Assert.Throws<DomainException>(() => locacao.DeduzirCaucao(4, 1_000.01m));
        }

        [Fact]
        public void Deduzir_em_locacao_sem_caucao_e_recusado()
        {
            var locacao = Fabrica.Locacao();

            Assert.Throws<InvalidOperationException>(() => locacao.DeduzirCaucao(1, 100m));
        }

        // ======================= seguro =======================

        [Fact]
        public void AdicionarSeguro_recusa_o_segundo_seguro_ativo()
        {
            var locacao = Fabrica.Locacao();
            locacao.AdicionarSeguro(1);

            Assert.Throws<DomainException>(() => locacao.AdicionarSeguro(2));
        }

        [Fact]
        public void CancelarSeguro_libera_a_contratacao_de_outro()
        {
            var locacao = Fabrica.Locacao();
            locacao.AdicionarSeguro(1);
            Fabrica.DefinirId(locacao.Seguros.Single(), 7);

            locacao.CancelarSeguro(7);
            locacao.AdicionarSeguro(2);

            Assert.Equal(2, locacao.Seguros.Count);
            Assert.Equal(2, Assert.Single(locacao.Seguros, s => s.Ativo).IdSeguro);
        }

        [Fact]
        public void CancelarSeguro_duas_vezes_e_recusado()
        {
            var locacao = Fabrica.Locacao();
            locacao.AdicionarSeguro(1);
            Fabrica.DefinirId(locacao.Seguros.Single(), 7);
            locacao.CancelarSeguro(7);

            Assert.Throws<DomainException>(() => locacao.CancelarSeguro(7));
        }

        [Fact]
        public void Seguro_em_locacao_ja_devolvida_e_recusado()
        {
            var locacao = Finalizada();

            Assert.Throws<DomainException>(() => locacao.AdicionarSeguro(1));
        }

        // ======================= adicionais =======================

        [Fact]
        public void CalcularDias_usa_o_fim_previsto_enquanto_nao_houve_devolucao()
        {
            var inicio = DateTime.UtcNow;
            var locacao = Fabrica.Locacao(dataInicio: inicio, dataFimPrevista: inicio.AddDays(4));

            Assert.Equal(4, locacao.CalcularDias());
        }

        [Theory]
        [InlineData(24, 1)]
        [InlineData(25, 2)]     // período parcial conta diária cheia
        [InlineData(72, 3)]
        [InlineData(73, 4)]
        public void CalcularDias_arredonda_periodo_parcial_para_cima(int horas, int diasEsperados)
        {
            var inicio = DateTime.UtcNow;
            var locacao = Fabrica.Locacao(dataInicio: inicio, dataFimPrevista: inicio.AddHours(horas));

            Assert.Equal(diasEsperados, locacao.CalcularDias());
        }

        [Fact]
        public void CalcularTotalAdicionais_multiplica_diaria_por_quantidade_e_dias()
        {
            var inicio = DateTime.UtcNow;
            var locacao = Fabrica.Locacao(dataInicio: inicio, dataFimPrevista: inicio.AddDays(4));

            locacao.AdicionarAdicional(idAdicional: 5, valorDiaria: 25m, quantidade: 2);   // 25 * 2 * 4
            locacao.AdicionarAdicional(idAdicional: 6, valorDiaria: 10m, quantidade: 1);   // 10 * 1 * 4

            Assert.Equal(240m, locacao.CalcularTotalAdicionais());
        }

        [Fact]
        public void AdicionarAdicional_repetido_e_recusado()
        {
            var locacao = Fabrica.Locacao();
            locacao.AdicionarAdicional(idAdicional: 5, valorDiaria: 25m, quantidade: 1);

            Assert.Throws<DomainException>(() =>
                locacao.AdicionarAdicional(idAdicional: 5, valorDiaria: 25m, quantidade: 3));
        }

        [Fact]
        public void RemoverAdicional_tira_da_conta()
        {
            var locacao = Fabrica.Locacao();
            locacao.AdicionarAdicional(idAdicional: 5, valorDiaria: 25m, quantidade: 1);

            locacao.RemoverAdicional(5);

            Assert.Empty(locacao.Adicionais);
            Assert.Equal(0m, locacao.CalcularTotalAdicionais());
        }

        [Fact]
        public void RemoverAdicional_inexistente_e_recusado()
        {
            var locacao = Fabrica.Locacao();

            Assert.Throws<DomainException>(() => locacao.RemoverAdicional(99));
        }

        [Fact]
        public void Adicional_em_locacao_ja_devolvida_e_recusado()
        {
            var locacao = Finalizada();

            Assert.Throws<DomainException>(() =>
                locacao.AdicionarAdicional(idAdicional: 5, valorDiaria: 25m, quantidade: 1));
        }

        // ======================= vistoria =======================

        [Fact]
        public void RegistrarVistoria_guarda_km_e_combustivel()
        {
            var locacao = Fabrica.Locacao();

            locacao.RegistrarVistoria(
                idFuncionario: 3,
                tipo: TipoVistoria.Retirada,
                combustivel: NivelCombustivel.Cheio,
                km: 15_000,
                observacoes: "sem avarias");

            var vistoria = Assert.Single(locacao.Vistorias);
            Assert.Equal(TipoVistoria.Retirada, vistoria.Tipo);
            Assert.Equal(NivelCombustivel.Cheio, vistoria.Combustivel);
            Assert.Equal(15_000, vistoria.KmVeiculo);
            Assert.Empty(vistoria.Danos);
        }

        [Fact]
        public void Vistoriar_locacao_ja_fechada_e_recusado()
        {
            var locacao = Finalizada();

            Assert.Throws<DomainException>(() => locacao.RegistrarVistoria(
                3, TipoVistoria.Devolucao, NivelCombustivel.Meio, 15_400, null));
        }

        [Fact]
        public void Dano_na_vistoria_de_devolucao_nao_abre_manutencao_com_o_contrato_aberto()
        {
            var veiculo = Fabrica.Veiculo();
            var locacao = Fabrica.LocacaoEmAndamento(veiculo: veiculo);
            locacao.RegistrarVistoria(3, TipoVistoria.Devolucao, NivelCombustivel.Meio, 15_400, null);

            var vistoria = locacao.Vistorias.Single(v => v.Tipo == TipoVistoria.Devolucao);
            Fabrica.DefinirId(vistoria, 8);

            locacao.RegistrarDanoVistoria(8, "Risco na porta direita", TipoDano.Risco, 350m);

            // a avaria fica registrada, mas o carro ainda está com o cliente: a ordem corretiva só
            // abre no fechamento (ver Finalizar_com_avaria_na_devolucao_manda_o_veiculo_para_a_oficina)
            Assert.Single(vistoria.Danos);
            Assert.Equal(StatusVeiculo.Locado, veiculo.Status);
            Assert.Empty(veiculo.Manutencoes);
        }

        [Fact]
        public void Dano_na_vistoria_de_retirada_e_recusado()
        {
            var locacao = Fabrica.Locacao();
            locacao.RegistrarVistoria(3, TipoVistoria.Retirada, NivelCombustivel.Cheio, 15_000, null);
            Fabrica.DefinirId(locacao.Vistorias.Single(), 8);

            // dano na retirada seria do locatário anterior: só a vistoria de devolução aceita
            Assert.Throws<DomainException>(() =>
                locacao.RegistrarDanoVistoria(8, "Risco na porta", TipoDano.Risco, 350m));
        }

        [Fact]
        public void Dano_em_vistoria_inexistente_e_recusado()
        {
            var locacao = Fabrica.LocacaoEmAndamento();
            locacao.RegistrarVistoria(3, TipoVistoria.Devolucao, NivelCombustivel.Meio, 15_400, null);

            Assert.Throws<DomainException>(() =>
                locacao.RegistrarDanoVistoria(999, "Risco", TipoDano.Risco, 100m));
        }

        [Fact]
        public void RemoverDanoVistoria_tira_o_dano_da_vistoria()
        {
            var locacao = Fabrica.LocacaoEmAndamento();
            locacao.RegistrarVistoria(3, TipoVistoria.Devolucao, NivelCombustivel.Meio, 15_400, null);
            var vistoria = locacao.Vistorias.Single(v => v.Tipo == TipoVistoria.Devolucao);
            Fabrica.DefinirId(vistoria, 8);
            locacao.RegistrarDanoVistoria(8, "Risco na porta", TipoDano.Risco, 350m);
            var dano = vistoria.Danos.Single();
            Fabrica.DefinirId(dano, 2);

            locacao.RemoverDanoVistoria(idDano: 2, idVistoria: 8);

            Assert.Empty(vistoria.Danos);
        }

        // ======================= atualização =======================

        [Fact]
        public void AtualizarDados_troca_previsao_km_e_valor()
        {
            var inicio = DateTime.UtcNow;
            var locacao = Fabrica.Locacao(dataInicio: inicio, dataFimPrevista: inicio.AddDays(3));

            locacao.AtualizarDados(inicio.AddDays(5), kmInicial: 15_200, valorPrevisto: 700m);

            Assert.Equal(inicio.AddDays(5), locacao.DataFimPrevista);
            Assert.Equal(15_200, locacao.KmInicial);
            Assert.Equal(700m, locacao.ValorPrevisto);
        }

        [Fact]
        public void AtualizarDados_com_fim_anterior_ao_inicio_e_recusado()
        {
            var locacao = Fabrica.Locacao();

            Assert.Throws<InvalidOperationException>(() =>
                locacao.AtualizarDados(locacao.DataInicio.AddHours(-1), 15_000, 450m));
        }

        [Fact]
        public void AtualizarDados_de_locacao_devolvida_e_recusado()
        {
            var locacao = Finalizada();

            Assert.Throws<InvalidOperationException>(() =>
                locacao.AtualizarDados(locacao.DataFimPrevista.AddDays(1), 15_000, 450m));
        }
    }
}
