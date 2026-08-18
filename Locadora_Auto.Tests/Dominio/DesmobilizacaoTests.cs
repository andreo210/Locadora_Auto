using Locadora_Auto.Domain.Entidades;
using Locadora_Auto.Tests.Fabricas;
using Xunit;

namespace Locadora_Auto.Tests.Dominio
{
    /// <summary>
    /// RN-56: o ativo deixa a frota, em definitivo.
    ///
    /// A palavra que carrega a regra é <b>terminal</b>. Um estado que se diz final e depois aceita
    /// uma transição qualquer é pior que não ter estado nenhum: o carro vendido volta a aparecer
    /// como frota e a conciliação de ativo passa a mentir. Por isso a guarda mora no
    /// <c>AplicarStatus</c>, que é a escrita única de status — e por isso metade destes testes é
    /// sobre o que <b>não</b> pode acontecer depois.
    /// </summary>
    public class DesmobilizacaoTests
    {
        private const int Responsavel = 7;
        private const string Motivo = "idade e custo de manutenção";

        private static DateTime Amanha => DateTime.UtcNow.AddDays(1);

        // ======================= o ato =======================

        [Fact]
        public void Desmobilizar_tira_o_carro_da_frota_com_motivo_data_e_responsavel()
        {
            var veiculo = Fabrica.Veiculo();

            veiculo.Desmobilizar(Motivo, Responsavel);

            Assert.Equal(StatusVeiculo.Desmobilizado, veiculo.Status);
            Assert.False(veiculo.Disponivel);
            Assert.Equal(Motivo, veiculo.MotivoDesmobilizacao);
            Assert.Equal(Responsavel, veiculo.IdFuncionarioDesmobilizacao);
            Assert.NotNull(veiculo.DataDesmobilizacao);
        }

        [Fact]
        public void Desmobilizar_tira_o_carro_do_cadastro_ativo()
        {
            // carro vendido não é frota: some de toda consulta por Ativo e — pela RN-55 — libera a
            // placa para o dia em que o Detran a reemitir
            var veiculo = Fabrica.Veiculo();

            veiculo.Desmobilizar(Motivo, Responsavel);

            Assert.False(veiculo.Ativo);
        }

        [Fact]
        public void Desmobilizar_registra_movimento_na_trilha()
        {
            var veiculo = Fabrica.Veiculo();

            veiculo.Desmobilizar(Motivo, Responsavel);
            var movimento = veiculo.Movimentos.Last();

            Assert.Equal(StatusVeiculo.Disponivel, movimento.StatusOrigem);
            Assert.Equal(StatusVeiculo.Desmobilizado, movimento.StatusDestino);
            Assert.Equal(TipoDocumentoOrigem.Desmobilizacao, movimento.TipoOrigem);
        }

        [Fact]
        public void Desmobilizar_sem_motivo_e_recusado()
        {
            var veiculo = Fabrica.Veiculo();

            Assert.Throws<DomainException>(() => veiculo.Desmobilizar("   ", Responsavel));
            Assert.Equal(StatusVeiculo.Disponivel, veiculo.Status);
        }

        [Fact]
        public void Desmobilizar_sem_responsavel_e_recusado()
        {
            var veiculo = Fabrica.Veiculo();

            Assert.Throws<DomainException>(() => veiculo.Desmobilizar(Motivo, idFuncionarioResponsavel: 0));
        }

        // ======================= de onde se pode =======================

        [Fact]
        public void Veiculo_locado_nao_pode_ser_desmobilizado()
        {
            // vender carro com cliente dentro é o pior desfecho possível
            var veiculo = Fabrica.Veiculo();
            veiculo.Locar(Fabrica.Contrato());

            Assert.Throws<DomainException>(() => veiculo.Desmobilizar(Motivo, Responsavel));
            Assert.Equal(StatusVeiculo.Locado, veiculo.Status);
        }

        [Fact]
        public void Veiculo_em_manutencao_nao_pode_ser_desmobilizado()
        {
            // a ordem está aberta e tem custo a apurar
            var veiculo = Fabrica.Veiculo();
            veiculo.IniciarManutencao(TipoManutencao.Corretiva, "motor");

            Assert.Throws<DomainException>(() => veiculo.Desmobilizar(Motivo, Responsavel));
        }

        [Fact]
        public void Veiculo_em_transferencia_nao_pode_ser_desmobilizado()
        {
            var veiculo = Fabrica.Veiculo();
            veiculo.EnviarParaTransferencia(3, Amanha, Responsavel);

            Assert.Throws<DomainException>(() => veiculo.Desmobilizar(Motivo, Responsavel));
        }

        [Fact]
        public void Bloqueado_para_venda_pode_ser_desmobilizado_e_o_bloqueio_e_encerrado()
        {
            // é o caminho normal da operação: primeiro sai da agenda, depois sai da frota. Deixar o
            // bloqueio aberto o manteria para sempre no indicador de bloqueios vencidos, contando
            // como carro esquecido quando na verdade ele foi vendido
            var veiculo = Fabrica.Veiculo();
            var bloqueio = veiculo.Bloquear(MotivoBloqueio.Desmobilizacao, Amanha, Responsavel);

            veiculo.Desmobilizar(Motivo, Responsavel);

            Assert.Equal(StatusVeiculo.Desmobilizado, veiculo.Status);
            Assert.False(bloqueio.EmAberto);
            Assert.False(veiculo.TemBloqueioEmAberto());
        }

        [Fact]
        public void Veiculo_no_patio_pode_ser_desmobilizado()
        {
            // voltou da última locação e vai direto para a venda
            var veiculo = Fabrica.Veiculo();
            var contrato = Fabrica.Contrato();
            veiculo.Locar(contrato);
            veiculo.RegistrarDevolucao(16_000, 1, contrato);

            veiculo.Desmobilizar(Motivo, Responsavel);

            Assert.Equal(StatusVeiculo.Desmobilizado, veiculo.Status);
        }

        // ======================= terminal de verdade =======================

        [Fact]
        public void Desmobilizado_nao_pode_ser_desmobilizado_de_novo()
        {
            var veiculo = Fabrica.Veiculo();
            veiculo.Desmobilizar(Motivo, Responsavel);

            Assert.Throws<DomainException>(() => veiculo.Desmobilizar("outro motivo", Responsavel));
        }

        [Fact]
        public void Desmobilizado_nao_pode_ser_reativado()
        {
            // reativar recolocaria na frota um carro que já não é da casa — e ainda esbarraria na
            // placa que a RN-55 liberou para recadastro quando ele saiu
            var veiculo = Fabrica.Veiculo();
            veiculo.Desmobilizar(Motivo, Responsavel);

            Assert.Throws<DomainException>(() => veiculo.Ativar());
            Assert.Equal(StatusVeiculo.Desmobilizado, veiculo.Status);
        }

        [Fact]
        public void Desmobilizado_nao_pode_ser_locado()
        {
            var veiculo = Fabrica.Veiculo();
            veiculo.Desmobilizar(Motivo, Responsavel);

            Assert.Throws<DomainException>(() => veiculo.Locar(Fabrica.Contrato()));
        }

        [Fact]
        public void Desmobilizado_nao_pode_ser_bloqueado()
        {
            var veiculo = Fabrica.Veiculo();
            veiculo.Desmobilizar(Motivo, Responsavel);

            Assert.Throws<DomainException>(() => veiculo.Bloquear(MotivoBloqueio.Documental, Amanha, Responsavel));
        }

        [Fact]
        public void Desmobilizado_nao_pode_ser_transferido()
        {
            var veiculo = Fabrica.Veiculo();
            veiculo.Desmobilizar(Motivo, Responsavel);

            Assert.Throws<DomainException>(() => veiculo.EnviarParaTransferencia(3, Amanha, Responsavel));
        }

        [Fact]
        public void Desmobilizado_nao_pode_ter_o_cadastro_alterado()
        {
            // Atualizar não passa pelo AplicarStatus, então precisa da guarda própria: mexer no
            // hodômetro ou na filial de um carro vendido corrompe dado de frota que já não é da casa
            var veiculo = Fabrica.Veiculo();
            veiculo.Desmobilizar(Motivo, Responsavel);

            Assert.Throws<DomainException>(() => veiculo.Atualizar(99_000, 2));
            Assert.Equal(15_000, veiculo.KmAtual);
        }

        [Fact]
        public void Desmobilizado_nao_entra_em_manutencao()
        {
            var veiculo = Fabrica.Veiculo();
            veiculo.Desmobilizar(Motivo, Responsavel);

            Assert.Throws<DomainException>(() => veiculo.IniciarManutencao(TipoManutencao.Revisao, "revisão"));
        }
    }
}
