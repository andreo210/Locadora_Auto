using Locadora_Auto.Domain.Entidades;
using Locadora_Auto.Tests.Fabricas;
using Xunit;

namespace Locadora_Auto.Tests.Dominio
{
    /// <summary>
    /// RN-48/RN-49: remanejamento programado de frota.
    ///
    /// A regra inteira está no intervalo: o veículo sai da oferta da origem <b>antes</b> de entrar
    /// na do destino, e enquanto roda não conta em filial nenhuma. Contá-lo nas duas é overbooking
    /// involuntário — as duas filiais vendem o mesmo carro e uma delas descobre no balcão.
    ///
    /// A RN-48 é o contraponto e tem teste próprio aqui: devolução one-way <b>não</b> passa por
    /// <c>EmTransferencia</c>, porque a taxa de retorno já pagou o desequilíbrio e prender o carro
    /// cobraria duas vezes pelo mesmo fato.
    /// </summary>
    public class TransferenciaEntreFiliaisTests
    {
        private const int Origem = 1;
        private const int Destino = 3;
        private const int Responsavel = 7;

        private static DateTime Amanha => DateTime.UtcNow.AddDays(1);

        private static MovimentoVeiculo UltimoMovimento(Veiculo veiculo) => veiculo.Movimentos.Last();

        private static Veiculo Disponivel() => Fabrica.Veiculo(idFilial: Origem);

        // ======================= envio =======================

        [Fact]
        public void Envio_tira_o_veiculo_da_oferta_sem_mudar_a_filial_atual()
        {
            var veiculo = Disponivel();

            veiculo.EnviarParaTransferencia(Destino, Amanha, Responsavel);

            Assert.Equal(StatusVeiculo.EmTransferencia, veiculo.Status);
            Assert.False(veiculo.Disponivel);

            // enquanto roda, quem responde pelo carro é a filial de origem: trocar aqui faria o
            // destino contá-lo como frota antes de ele existir lá
            Assert.Equal(Origem, veiculo.FilialAtualId);
        }

        [Fact]
        public void Envio_registra_movimento_com_origem_na_transferencia()
        {
            var veiculo = Disponivel();

            var transferencia = veiculo.EnviarParaTransferencia(Destino, Amanha, Responsavel);
            var movimento = UltimoMovimento(veiculo);

            Assert.Equal(StatusVeiculo.Disponivel, movimento.StatusOrigem);
            Assert.Equal(StatusVeiculo.EmTransferencia, movimento.StatusDestino);
            Assert.Equal(TipoDocumentoOrigem.Transferencia, movimento.TipoOrigem);
            Assert.Same(transferencia, movimento.TransferenciaOrigem);
        }

        [Fact]
        public void Transferencia_para_a_propria_filial_e_recusada()
        {
            var veiculo = Disponivel();

            Assert.Throws<DomainException>(
                () => veiculo.EnviarParaTransferencia(Origem, Amanha, Responsavel));
        }

        [Fact]
        public void Transferencia_sem_prazo_futuro_e_recusada()
        {
            var veiculo = Disponivel();

            Assert.Throws<DomainException>(
                () => veiculo.EnviarParaTransferencia(Destino, DateTime.UtcNow.AddMinutes(-1), Responsavel));
        }

        [Fact]
        public void Transferencia_sem_responsavel_e_recusada()
        {
            var veiculo = Disponivel();

            Assert.Throws<DomainException>(
                () => veiculo.EnviarParaTransferencia(Destino, Amanha, idFuncionarioResponsavel: 0));
        }

        [Theory]
        [InlineData(StatusVeiculo.Locado)]
        [InlineData(StatusVeiculo.EmPreparacao)]
        [InlineData(StatusVeiculo.EmManutencao)]
        [InlineData(StatusVeiculo.Bloqueado)]
        public void So_veiculo_disponivel_pode_pegar_a_estrada(StatusVeiculo situacao)
        {
            // carro locado está com o cliente, em preparação está sujo, em oficina está desmontado
            // e bloqueado tem motivo próprio: nenhum deles pode simplesmente sair viajando
            var veiculo = Disponivel();

            switch (situacao)
            {
                case StatusVeiculo.Locado:
                    veiculo.Locar(Fabrica.Contrato());
                    break;
                case StatusVeiculo.EmPreparacao:
                    var contrato = Fabrica.Contrato();
                    veiculo.Locar(contrato);
                    veiculo.RegistrarDevolucao(16_000, Origem, contrato);
                    break;
                case StatusVeiculo.EmManutencao:
                    veiculo.IniciarManutencao(TipoManutencao.Preventiva, "revisão");
                    break;
                case StatusVeiculo.Bloqueado:
                    veiculo.Bloquear(MotivoBloqueio.Documental, Amanha, Responsavel);
                    break;
            }

            Assert.Throws<DomainException>(
                () => veiculo.EnviarParaTransferencia(Destino, Amanha, Responsavel));
        }

        [Fact]
        public void Veiculo_inativo_nao_pode_ser_transferido()
        {
            var veiculo = Disponivel();
            veiculo.Desativar();

            Assert.Throws<DomainException>(
                () => veiculo.EnviarParaTransferencia(Destino, Amanha, Responsavel));
        }

        [Fact]
        public void Segunda_transferencia_em_transito_e_recusada()
        {
            var veiculo = Disponivel();
            veiculo.EnviarParaTransferencia(Destino, Amanha, Responsavel);

            Assert.Throws<DomainException>(
                () => veiculo.EnviarParaTransferencia(5, Amanha, Responsavel));
        }

        // ======================= chegada =======================

        [Fact]
        public void Chegada_move_o_veiculo_para_o_destino_e_o_devolve_a_oferta()
        {
            var veiculo = Disponivel();
            var transferencia = veiculo.EnviarParaTransferencia(Destino, Amanha, Responsavel);

            veiculo.ConfirmarChegadaTransferencia(transferencia.IdTransferenciaVeiculo, 15_400);

            Assert.Equal(StatusVeiculo.Disponivel, veiculo.Status);
            Assert.True(veiculo.Disponivel);
            Assert.Equal(Destino, veiculo.FilialAtualId);
            Assert.Equal(15_400, veiculo.KmAtual);
            Assert.Equal(StatusTransferencia.Concluida, transferencia.Status);
            Assert.NotNull(transferencia.DataChegada);
        }

        [Fact]
        public void Chegada_com_km_menor_que_o_atual_e_recusada()
        {
            // RN-54: o trecho foi rodado, então o hodômetro só pode ter subido
            var veiculo = Disponivel();
            var transferencia = veiculo.EnviarParaTransferencia(Destino, Amanha, Responsavel);

            Assert.Throws<DomainException>(
                () => veiculo.ConfirmarChegadaTransferencia(transferencia.IdTransferenciaVeiculo, 14_000));

            Assert.Equal(StatusVeiculo.EmTransferencia, veiculo.Status);
            Assert.Equal(Origem, veiculo.FilialAtualId);
        }

        [Fact]
        public void Chegada_de_veiculo_inativo_nao_o_devolve_a_oferta()
        {
            // RN-53: chegar não oferta um carro desativado no meio do caminho
            var veiculo = Disponivel();
            var transferencia = veiculo.EnviarParaTransferencia(Destino, Amanha, Responsavel);
            veiculo.Desativar();

            veiculo.ConfirmarChegadaTransferencia(transferencia.IdTransferenciaVeiculo, 15_400);

            Assert.Equal(StatusVeiculo.Bloqueado, veiculo.Status);
            Assert.False(veiculo.Disponivel);
            Assert.Equal(Destino, veiculo.FilialAtualId);
        }

        // ======================= cancelamento =======================

        [Fact]
        public void Cancelar_devolve_o_veiculo_a_oferta_da_origem()
        {
            var veiculo = Disponivel();
            var transferencia = veiculo.EnviarParaTransferencia(Destino, Amanha, Responsavel);

            veiculo.CancelarTransferencia(transferencia.IdTransferenciaVeiculo);

            Assert.Equal(StatusVeiculo.Disponivel, veiculo.Status);
            Assert.Equal(Origem, veiculo.FilialAtualId);
            Assert.Equal(StatusTransferencia.Cancelada, transferencia.Status);
        }

        [Fact]
        public void Transferencia_cancelada_nao_e_apagada()
        {
            // a trilha já registrou a saída da oferta; apagar a transferência deixaria aquele
            // movimento sem documento de origem, que é o que a RN-37 proíbe
            var veiculo = Disponivel();
            var transferencia = veiculo.EnviarParaTransferencia(Destino, Amanha, Responsavel);
            veiculo.CancelarTransferencia(transferencia.IdTransferenciaVeiculo);

            Assert.Single(veiculo.Transferencias);
            Assert.All(veiculo.Movimentos.Where(m => m.TipoOrigem == TipoDocumentoOrigem.Transferencia),
                m => Assert.NotNull(m.TransferenciaOrigem));
        }

        [Fact]
        public void Depois_de_chegar_o_veiculo_pode_ser_transferido_de_novo()
        {
            var veiculo = Disponivel();
            var ida = veiculo.EnviarParaTransferencia(Destino, Amanha, Responsavel);
            veiculo.ConfirmarChegadaTransferencia(ida.IdTransferenciaVeiculo, 15_400);

            var volta = veiculo.EnviarParaTransferencia(Origem, Amanha, Responsavel);

            Assert.Equal(2, veiculo.Transferencias.Count);
            Assert.Equal(Destino, volta.IdFilialOrigem);
            Assert.Equal(Origem, volta.IdFilialDestino);
        }

        // ======================= atraso =======================

        [Fact]
        public void Transferencia_em_transito_passada_do_prazo_esta_atrasada()
        {
            var veiculo = Disponivel();
            var transferencia = veiculo.EnviarParaTransferencia(Destino, Amanha, Responsavel);

            Assert.False(transferencia.Atrasada(DateTime.UtcNow));
            Assert.True(transferencia.Atrasada(DateTime.UtcNow.AddDays(2)));
        }

        [Fact]
        public void Transferencia_concluida_nunca_conta_como_atrasada()
        {
            var veiculo = Disponivel();
            var transferencia = veiculo.EnviarParaTransferencia(Destino, Amanha, Responsavel);
            veiculo.ConfirmarChegadaTransferencia(transferencia.IdTransferenciaVeiculo, 15_400);

            Assert.False(transferencia.Atrasada(DateTime.UtcNow.AddDays(30)));
        }

        // ======================= RN-48: one-way não é transferência =======================

        [Fact]
        public void Devolucao_one_way_nao_passa_por_transferencia()
        {
            // a taxa de retorno (RN-21) já pagou o desequilíbrio; prender o carro em trânsito
            // cobraria duas vezes pelo mesmo fato
            var veiculo = Disponivel();
            var contrato = Fabrica.Contrato();
            veiculo.Locar(contrato);

            veiculo.RegistrarDevolucao(16_000, Destino, contrato);

            Assert.Equal(StatusVeiculo.EmPreparacao, veiculo.Status);
            Assert.Equal(Destino, veiculo.FilialAtualId);
            Assert.Empty(veiculo.Transferencias);
        }
    }
}
