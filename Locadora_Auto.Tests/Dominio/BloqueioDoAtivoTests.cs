using Locadora_Auto.Domain.Entidades;
using Locadora_Auto.Tests.Fabricas;
using Xunit;

namespace Locadora_Auto.Tests.Dominio
{
    /// <summary>
    /// RN-52: todo bloqueio tem motivo, data prevista de liberação e responsável.
    ///
    /// Antes disso, tirar um carro da oferta era mudar o status e mais nada — e o defeito não era
    /// o carro sair da oferta, era ninguém saber por quê nem até quando. Cada teste aqui fixa uma
    /// das três exigências, ou a volta: bloqueio <b>suspende</b> a situação do ativo, e liberar
    /// devolve o carro para onde ele estava, não para a venda.
    /// </summary>
    public class BloqueioDoAtivoTests
    {
        private const int Responsavel = 7;

        private static DateTime Amanha => DateTime.UtcNow.AddDays(1);

        private static MovimentoVeiculo UltimoMovimento(Veiculo veiculo) => veiculo.Movimentos.Last();

        /// <summary>Veículo na fila do pátio, pelo caminho real: locar e devolver.</summary>
        private static Veiculo NoPatio()
        {
            var veiculo = Fabrica.Veiculo();
            var contrato = Fabrica.Contrato();

            veiculo.Locar(contrato);
            veiculo.RegistrarDevolucao(16_000, 1, contrato);

            return veiculo;
        }

        // ======================= as três exigências =======================

        [Fact]
        public void Bloqueio_tira_o_veiculo_da_oferta_com_motivo_prazo_e_responsavel()
        {
            var veiculo = Fabrica.Veiculo();

            var bloqueio = veiculo.Bloquear(MotivoBloqueio.Documental, Amanha, Responsavel, "licenciamento vencido");

            Assert.Equal(StatusVeiculo.Bloqueado, veiculo.Status);
            Assert.False(veiculo.Disponivel);

            Assert.Equal(MotivoBloqueio.Documental, bloqueio.Motivo);
            Assert.Equal(Responsavel, bloqueio.IdFuncionarioResponsavel);
            Assert.True(bloqueio.EmAberto);
            Assert.Equal("licenciamento vencido", bloqueio.Observacao);
        }

        [Fact]
        public void Bloqueio_sem_prazo_futuro_e_recusado()
        {
            var veiculo = Fabrica.Veiculo();

            // bloqueio que nasce vencido não é prazo, é esquecimento com data
            Assert.Throws<DomainException>(
                () => veiculo.Bloquear(MotivoBloqueio.Comercial, DateTime.UtcNow.AddMinutes(-1), Responsavel));

            Assert.Equal(StatusVeiculo.Disponivel, veiculo.Status);
        }

        [Fact]
        public void Bloqueio_sem_responsavel_e_recusado()
        {
            var veiculo = Fabrica.Veiculo();

            Assert.Throws<DomainException>(
                () => veiculo.Bloquear(MotivoBloqueio.Comercial, Amanha, idFuncionarioResponsavel: 0));

            Assert.Equal(StatusVeiculo.Disponivel, veiculo.Status);
        }

        // ======================= trilha (RN-37) =======================

        [Fact]
        public void Bloqueio_registra_movimento_com_origem_no_proprio_bloqueio()
        {
            var veiculo = Fabrica.Veiculo();

            var bloqueio = veiculo.Bloquear(MotivoBloqueio.Evento, Amanha, Responsavel);
            var movimento = UltimoMovimento(veiculo);

            Assert.Equal(StatusVeiculo.Disponivel, movimento.StatusOrigem);
            Assert.Equal(StatusVeiculo.Bloqueado, movimento.StatusDestino);
            Assert.Equal(TipoDocumentoOrigem.Bloqueio, movimento.TipoOrigem);
            Assert.Same(bloqueio, movimento.BloqueioOrigem);
        }

        [Fact]
        public void Liberacao_tambem_cita_o_bloqueio_na_trilha()
        {
            // é o par de movimentos que permite medir quanto tempo cada motivo segurou a frota
            var veiculo = Fabrica.Veiculo();
            var bloqueio = veiculo.Bloquear(MotivoBloqueio.Evento, Amanha, Responsavel);

            veiculo.LiberarBloqueio(bloqueio.IdBloqueioVeiculo);
            var movimento = UltimoMovimento(veiculo);

            Assert.Equal(TipoDocumentoOrigem.Bloqueio, movimento.TipoOrigem);
            Assert.Same(bloqueio, movimento.BloqueioOrigem);
        }

        // ======================= um por vez =======================

        [Fact]
        public void Segundo_bloqueio_em_aberto_e_recusado()
        {
            var veiculo = Fabrica.Veiculo();
            veiculo.Bloquear(MotivoBloqueio.Documental, Amanha, Responsavel);

            Assert.Throws<DomainException>(
                () => veiculo.Bloquear(MotivoBloqueio.Comercial, Amanha, Responsavel));
        }

        [Fact]
        public void Depois_de_liberar_pode_bloquear_de_novo()
        {
            var veiculo = Fabrica.Veiculo();
            var primeiro = veiculo.Bloquear(MotivoBloqueio.Documental, Amanha, Responsavel);
            veiculo.LiberarBloqueio(primeiro.IdBloqueioVeiculo);

            var segundo = veiculo.Bloquear(MotivoBloqueio.Comercial, Amanha, Responsavel);

            Assert.Equal(2, veiculo.Bloqueios.Count);
            Assert.False(primeiro.EmAberto);
            Assert.True(segundo.EmAberto);
        }

        // ======================= de onde se pode bloquear =======================

        [Fact]
        public void Veiculo_em_manutencao_nao_pode_ser_bloqueado()
        {
            // já está fora da oferta com uma OS respondendo por ele; sobrepor o bloqueio apagaria
            // de qual ordem ele depende
            var veiculo = Fabrica.Veiculo();
            veiculo.IniciarManutencao(TipoManutencao.Preventiva, "revisão");

            Assert.Throws<DomainException>(
                () => veiculo.Bloquear(MotivoBloqueio.Documental, Amanha, Responsavel));
        }

        [Fact]
        public void Veiculo_locado_pode_ser_bloqueado_por_nao_devolucao()
        {
            // doc 08 §5: carro não devolvido além do limiar sai da oferta com motivo próprio, para
            // não ficar Locado indefinidamente contaminando a utilização
            var veiculo = Fabrica.Veiculo();
            veiculo.Locar(Fabrica.Contrato());

            var bloqueio = veiculo.Bloquear(MotivoBloqueio.NaoDevolvido, Amanha, Responsavel);

            Assert.Equal(StatusVeiculo.Bloqueado, veiculo.Status);
            Assert.Equal(StatusVeiculo.Locado, bloqueio.StatusAnterior);
        }

        // ======================= a volta =======================

        [Fact]
        public void Liberar_devolve_o_veiculo_disponivel_para_a_oferta()
        {
            var veiculo = Fabrica.Veiculo();
            var bloqueio = veiculo.Bloquear(MotivoBloqueio.Comercial, Amanha, Responsavel);

            veiculo.LiberarBloqueio(bloqueio.IdBloqueioVeiculo);

            Assert.Equal(StatusVeiculo.Disponivel, veiculo.Status);
            Assert.True(veiculo.Disponivel);
            Assert.False(bloqueio.EmAberto);
            Assert.NotNull(bloqueio.DataLiberacao);
        }

        [Fact]
        public void Liberar_veiculo_bloqueado_no_patio_devolve_ao_patio()
        {
            // o carro continua sem conferência: soltá-lo direto na oferta venderia carro sujo
            var veiculo = NoPatio();
            var bloqueio = veiculo.Bloquear(MotivoBloqueio.Documental, Amanha, Responsavel);

            veiculo.LiberarBloqueio(bloqueio.IdBloqueioVeiculo);

            Assert.Equal(StatusVeiculo.EmPreparacao, veiculo.Status);
            Assert.False(veiculo.Disponivel);
        }

        [Fact]
        public void Liberar_veiculo_bloqueado_locado_devolve_a_locado()
        {
            // o contrato continua aberto e o carro continua na rua
            var veiculo = Fabrica.Veiculo();
            veiculo.Locar(Fabrica.Contrato());
            var bloqueio = veiculo.Bloquear(MotivoBloqueio.NaoDevolvido, Amanha, Responsavel);

            veiculo.LiberarBloqueio(bloqueio.IdBloqueioVeiculo);

            Assert.Equal(StatusVeiculo.Locado, veiculo.Status);
            Assert.False(veiculo.Disponivel);
        }

        [Fact]
        public void Liberar_bloqueio_de_veiculo_inativo_nao_o_devolve_a_oferta()
        {
            // RN-53: toda saída de indisponibilidade só oferta se o veículo estiver ativo
            var veiculo = Fabrica.Veiculo();
            var bloqueio = veiculo.Bloquear(MotivoBloqueio.Comercial, Amanha, Responsavel);
            veiculo.Desativar();

            veiculo.LiberarBloqueio(bloqueio.IdBloqueioVeiculo);

            Assert.Equal(StatusVeiculo.Bloqueado, veiculo.Status);
            Assert.False(veiculo.Disponivel);
        }

        [Fact]
        public void Liberar_veiculo_que_nao_esta_bloqueado_e_recusado()
        {
            var veiculo = Fabrica.Veiculo();

            Assert.Throws<DomainException>(() => veiculo.LiberarBloqueio(1));
        }

        // ======================= a porta dos fundos =======================

        [Fact]
        public void Reativar_veiculo_nao_libera_o_bloqueio()
        {
            // sem esta guarda, Ativar() devolveria à venda um carro que alguém tirou dela com
            // motivo, prazo e responsável registrados
            var veiculo = Fabrica.Veiculo();
            veiculo.Bloquear(MotivoBloqueio.Sinistro, Amanha, Responsavel);
            veiculo.Desativar();

            veiculo.Ativar();

            Assert.True(veiculo.Ativo);
            Assert.Equal(StatusVeiculo.Bloqueado, veiculo.Status);
            Assert.True(veiculo.TemBloqueioEmAberto());
        }

        [Fact]
        public void Reativar_veiculo_apenas_desativado_devolve_a_oferta()
        {
            // o contraponto do teste acima: sem bloqueio em aberto, reativar volta a ofertar
            var veiculo = Fabrica.Veiculo();
            veiculo.Desativar();

            veiculo.Ativar();

            Assert.Equal(StatusVeiculo.Disponivel, veiculo.Status);
            Assert.True(veiculo.Disponivel);
        }

        // ======================= vencimento =======================

        [Fact]
        public void Bloqueio_em_aberto_passado_do_prazo_esta_vencido()
        {
            var veiculo = Fabrica.Veiculo();
            var bloqueio = veiculo.Bloquear(MotivoBloqueio.Documental, Amanha, Responsavel);

            Assert.False(bloqueio.Vencido(DateTime.UtcNow));
            Assert.True(bloqueio.Vencido(DateTime.UtcNow.AddDays(2)));
        }

        [Fact]
        public void Bloqueio_liberado_nunca_conta_como_vencido()
        {
            // o indicador conta quem sumiu da oferta e ninguém percebeu, não quem já voltou
            var veiculo = Fabrica.Veiculo();
            var bloqueio = veiculo.Bloquear(MotivoBloqueio.Documental, Amanha, Responsavel);
            veiculo.LiberarBloqueio(bloqueio.IdBloqueioVeiculo);

            Assert.False(bloqueio.Vencido(DateTime.UtcNow.AddDays(30)));
        }
    }
}
