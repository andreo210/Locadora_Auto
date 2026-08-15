using Locadora_Auto.Domain.Entidades;
using Locadora_Auto.Tests.Fabricas;
using Xunit;

namespace Locadora_Auto.Tests.Dominio
{
    /// <summary>
    /// RN-37: toda transição do ativo registra documento de origem, autor e data. O autor sai da
    /// auditoria do <c>SaveChangesAsync</c> e por isso não aparece aqui; o que estes testes
    /// protegem é o par que o domínio precisa garantir sozinho — que a transição vira movimento e
    /// que o movimento sabe de onde veio.
    ///
    /// A parte "transição sem origem é proibida" não tem teste porque virou erro de compilação:
    /// <c>AplicarStatus</c> exige o <c>TipoDocumentoOrigem</c>, e não existe caminho para trocar o
    /// status sem passar por ele.
    /// </summary>
    public class MovimentoDoAtivoTests
    {
        /// <summary>Último movimento registrado — é o que a transição sob teste acabou de gerar.</summary>
        private static MovimentoVeiculo UltimoMovimento(Veiculo veiculo)
            => veiculo.Movimentos.Last();

        // ======================= cadastro =======================

        [Fact]
        public void Cadastro_do_veiculo_ja_nasce_com_movimento_de_entrada_na_frota()
        {
            var veiculo = Fabrica.Veiculo();

            var movimento = Assert.Single(veiculo.Movimentos);
            Assert.Null(movimento.StatusOrigem);   // antes de existir não havia situação anterior
            Assert.Equal(StatusVeiculo.Disponivel, movimento.StatusDestino);
            Assert.Equal(TipoDocumentoOrigem.Cadastro, movimento.TipoOrigem);
        }

        [Fact]
        public void Desativar_e_ativar_registram_movimento_de_cadastro()
        {
            var veiculo = Fabrica.Veiculo();

            veiculo.Desativar();
            var saida = UltimoMovimento(veiculo);

            veiculo.Ativar();
            var volta = UltimoMovimento(veiculo);

            Assert.Equal(StatusVeiculo.Disponivel, saida.StatusOrigem);
            Assert.Equal(StatusVeiculo.Indisponivel, saida.StatusDestino);
            Assert.Equal(TipoDocumentoOrigem.Cadastro, saida.TipoOrigem);

            Assert.Equal(StatusVeiculo.Indisponivel, volta.StatusOrigem);
            Assert.Equal(StatusVeiculo.Disponivel, volta.StatusDestino);
        }

        [Fact]
        public void Reaplicar_o_mesmo_estado_nao_vira_movimento()
        {
            // ativar um carro que já está na oferta não é transição: registrar viraria ruído na
            // trilha e falsearia o indicador de movimentos do §12
            var veiculo = Fabrica.Veiculo();

            veiculo.Ativar();

            Assert.Single(veiculo.Movimentos);   // só o do cadastro
        }

        // ======================= contrato =======================

        [Fact]
        public void Abrir_contrato_registra_movimento_com_origem_no_contrato()
        {
            var veiculo = Fabrica.Veiculo();

            var contrato = Fabrica.Locacao(veiculo: veiculo);

            var movimento = UltimoMovimento(veiculo);
            Assert.Equal(StatusVeiculo.Disponivel, movimento.StatusOrigem);
            Assert.Equal(StatusVeiculo.Locado, movimento.StatusDestino);
            Assert.Equal(TipoDocumentoOrigem.Contrato, movimento.TipoOrigem);
            Assert.Same(contrato, movimento.LocacaoOrigem);
        }

        [Fact]
        public void Devolucao_registra_movimento_para_a_preparacao_com_origem_no_contrato()
        {
            var veiculo = Fabrica.Veiculo();
            var contrato = Fabrica.Locacao(veiculo: veiculo);

            contrato.Finalizar(contrato.DataFimPrevista, kmFinal: 15_400, valorFinal: 520m, filialDevolucao: 1);

            var movimento = UltimoMovimento(veiculo);
            Assert.Equal(StatusVeiculo.Locado, movimento.StatusOrigem);
            Assert.Equal(StatusVeiculo.EmPreparacao, movimento.StatusDestino);
            Assert.Equal(TipoDocumentoOrigem.Contrato, movimento.TipoOrigem);
            Assert.Same(contrato, movimento.LocacaoOrigem);
        }

        [Fact]
        public void Cancelar_contrato_registra_a_volta_a_oferta_com_origem_no_contrato()
        {
            var veiculo = Fabrica.Veiculo();
            var contrato = Fabrica.Locacao(veiculo: veiculo);

            contrato.Cancelar();

            var movimento = UltimoMovimento(veiculo);
            Assert.Equal(StatusVeiculo.Locado, movimento.StatusOrigem);
            Assert.Equal(StatusVeiculo.Disponivel, movimento.StatusDestino);
            Assert.Same(contrato, movimento.LocacaoOrigem);
        }

        [Fact]
        public void Contrato_ja_gravado_entra_no_movimento_tambem_pelo_id()
        {
            // documento que está nascendo vai só pela navegação, e quem resolve a chave é o EF no
            // insert; documento que já tem id entra pelos dois, e é o id que a consulta usa
            var veiculo = Fabrica.Veiculo();
            var contrato = Fabrica.Contrato();
            Fabrica.DefinirId(contrato, 42);

            veiculo.Locar(contrato);

            var movimento = UltimoMovimento(veiculo);
            Assert.Equal(42, movimento.IdLocacaoOrigem);
            Assert.Same(contrato, movimento.LocacaoOrigem);
        }

        [Fact]
        public void Contrato_ainda_sem_id_entra_no_movimento_so_pela_navegacao()
        {
            var veiculo = Fabrica.Veiculo();

            var contrato = Fabrica.Locacao(veiculo: veiculo);

            var movimento = UltimoMovimento(veiculo);
            Assert.Null(movimento.IdLocacaoOrigem);          // gravar zero apontaria para lugar nenhum
            Assert.Same(contrato, movimento.LocacaoOrigem);
        }

        // ======================= pátio =======================

        [Fact]
        public void Liberacao_do_patio_registra_movimento_com_origem_no_patio()
        {
            var veiculo = Fabrica.Veiculo();
            var contrato = Fabrica.Locacao(veiculo: veiculo);
            contrato.Finalizar(contrato.DataFimPrevista, 15_400, 520m, filialDevolucao: 1);

            veiculo.LiberarDaPreparacao();

            var movimento = UltimoMovimento(veiculo);
            Assert.Equal(StatusVeiculo.EmPreparacao, movimento.StatusOrigem);
            Assert.Equal(StatusVeiculo.Disponivel, movimento.StatusDestino);

            // a liberação não tem documento: o ato do pátio é o próprio registro, e quem responde
            // "quem liberou" é o autor que a auditoria grava
            Assert.Equal(TipoDocumentoOrigem.Patio, movimento.TipoOrigem);
            Assert.Null(movimento.LocacaoOrigem);
            Assert.Null(movimento.ManutencaoOrigem);
        }

        // ======================= oficina =======================

        [Fact]
        public void Abrir_manutencao_registra_movimento_com_origem_na_ordem_recem_aberta()
        {
            var veiculo = Fabrica.Veiculo();

            veiculo.IniciarManutencao(TipoManutencao.Revisao, "Revisão de 30 mil km");

            var movimento = UltimoMovimento(veiculo);
            Assert.Equal(StatusVeiculo.EmManutencao, movimento.StatusDestino);
            Assert.Equal(TipoDocumentoOrigem.OrdemServico, movimento.TipoOrigem);
            Assert.Same(veiculo.Manutencoes.Single(), movimento.ManutencaoOrigem);
        }

        [Fact]
        public void Encerrar_manutencao_registra_a_volta_a_oferta_com_origem_na_ordem()
        {
            var veiculo = Fabrica.Veiculo();
            veiculo.IniciarManutencao(TipoManutencao.Corretiva, "Troca de embreagem");
            var ordem = veiculo.Manutencoes.Single();
            Fabrica.DefinirId(ordem, 10);

            veiculo.TerminaManutencao(custo: 1_250m, idManutencao: 10);

            var movimento = UltimoMovimento(veiculo);
            Assert.Equal(StatusVeiculo.EmManutencao, movimento.StatusOrigem);
            Assert.Equal(StatusVeiculo.Disponivel, movimento.StatusDestino);
            Assert.Equal(TipoDocumentoOrigem.OrdemServico, movimento.TipoOrigem);
            Assert.Equal(10, movimento.IdManutencaoOrigem);
        }

        // ======================= trilha =======================

        [Fact]
        public void Ciclo_completo_deixa_a_trilha_em_ordem_e_sem_transicao_orfa()
        {
            // é o indicador do §12: "transições sem documento de origem" tem de ser zero
            var veiculo = Fabrica.Veiculo();
            var contrato = Fabrica.Locacao(veiculo: veiculo);
            contrato.Finalizar(contrato.DataFimPrevista, 15_400, 520m, filialDevolucao: 1);
            veiculo.LiberarDaPreparacao();

            Assert.Equal(
                new[]
                {
                    StatusVeiculo.Disponivel,
                    StatusVeiculo.Locado,
                    StatusVeiculo.EmPreparacao,
                    StatusVeiculo.Disponivel
                },
                veiculo.Movimentos.Select(m => m.StatusDestino));

            Assert.All(veiculo.Movimentos, m => Assert.NotEqual(default, m.TipoOrigem));
            Assert.All(veiculo.Movimentos, m => Assert.NotEqual(default, m.DataMovimento));
        }

        [Fact]
        public void Movimento_com_origem_em_contrato_exige_o_contrato()
        {
            // a guarda existe para o caso de um caminho novo do domínio passar o tipo sem o
            // documento — o compilador garante o tipo, não o par
            Assert.Throws<DomainException>(() =>
                MovimentoVeiculo.Criar(
                    idVeiculo: 1,
                    StatusVeiculo.Disponivel,
                    StatusVeiculo.Locado,
                    TipoDocumentoOrigem.Contrato));
        }

        [Fact]
        public void Movimento_com_origem_em_ordem_de_servico_exige_a_ordem()
        {
            Assert.Throws<DomainException>(() =>
                MovimentoVeiculo.Criar(
                    idVeiculo: 1,
                    StatusVeiculo.Disponivel,
                    StatusVeiculo.EmManutencao,
                    TipoDocumentoOrigem.OrdemServico));
        }
    }
}
