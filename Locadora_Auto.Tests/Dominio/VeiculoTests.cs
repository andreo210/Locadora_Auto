using Locadora_Auto.Domain.Entidades;
using Locadora_Auto.Tests.Fabricas;
using Xunit;

namespace Locadora_Auto.Tests.Dominio
{
    public class VeiculoTests
    {
        [Fact]
        public void Criar_normaliza_placa_marca_e_modelo()
        {
            var veiculo = Veiculo.Criar(" abc1d23 ", "fiat", "argo", 2022, "9bwzzz377vt004251", 0, 1, 1);

            Assert.Equal("ABC1D23", veiculo.Placa);
            Assert.Equal("FIAT", veiculo.Marca);
            Assert.Equal("ARGO", veiculo.Modelo);
        }

        [Fact]
        public void Criar_nasce_disponivel_e_ativo()
        {
            var veiculo = Fabrica.Veiculo();

            Assert.True(veiculo.Ativo);
            Assert.True(veiculo.Disponivel);
            Assert.Equal(StatusVeiculo.Disponivel, veiculo.Status);
        }

        [Fact]
        public void Criar_com_km_negativo_e_recusado()
        {
            // zero km é válido; só valor negativo é recusado
            Assert.Throws<InvalidOperationException>(() =>
                Veiculo.Criar("ABC1D23", "Fiat", "Argo", 2022, "9BWZZZ377VT004251", -1, 1, 1));
        }

        [Fact]
        public void Criar_sem_placa_e_recusado()
        {
            Assert.Throws<InvalidOperationException>(() =>
                Veiculo.Criar("  ", "Fiat", "Argo", 2022, "9BWZZZ377VT004251", 0, 1, 1));
        }

        [Fact]
        public void Desativar_tira_o_veiculo_da_oferta()
        {
            var veiculo = Fabrica.Veiculo();

            veiculo.Desativar();

            Assert.False(veiculo.Ativo);
            Assert.False(veiculo.Disponivel);
            Assert.Equal(StatusVeiculo.Indisponivel, veiculo.Status);
        }

        [Fact]
        public void Ativar_devolve_o_veiculo_para_a_oferta()
        {
            var veiculo = Fabrica.Veiculo();
            veiculo.Desativar();

            veiculo.Ativar();

            Assert.True(veiculo.Ativo);
            Assert.True(veiculo.Disponivel);
            Assert.Equal(StatusVeiculo.Disponivel, veiculo.Status);
        }

        [Fact]
        public void Atualizar_deixa_marca_e_modelo_como_estao_quando_nao_vem_preenchidos()
        {
            var veiculo = Fabrica.Veiculo();

            veiculo.Atualizar(kmAtual: 30_000, idFilialAtual: 2);

            Assert.Equal(30_000, veiculo.KmAtual);
            Assert.Equal(2, veiculo.FilialAtualId);
            Assert.Equal("FIAT", veiculo.Marca);
            Assert.Equal("ARGO", veiculo.Modelo);
        }

        // ======================= ciclo do ativo =======================

        [Fact]
        public void Locar_tira_o_veiculo_da_oferta()
        {
            var veiculo = Fabrica.Veiculo();

            veiculo.Locar(Fabrica.Contrato());

            Assert.Equal(StatusVeiculo.Locado, veiculo.Status);
            Assert.False(veiculo.Disponivel);
        }

        [Theory]
        [InlineData(StatusVeiculo.Locado)]
        [InlineData(StatusVeiculo.EmManutencao)]
        [InlineData(StatusVeiculo.EmPreparacao)]
        public void Locar_veiculo_que_nao_esta_na_oferta_e_recusado(StatusVeiculo status)
        {
            var veiculo = EmStatus(status);

            Assert.Throws<DomainException>(() => veiculo.Locar(Fabrica.Contrato()));
        }

        [Fact]
        public void Locar_veiculo_inativo_e_recusado()
        {
            var veiculo = Fabrica.Veiculo();
            veiculo.Desativar();

            Assert.Throws<DomainException>(() => veiculo.Locar(Fabrica.Contrato()));
        }

        [Fact]
        public void Devolver_manda_para_preparacao_e_move_km_e_filial()
        {
            var veiculo = Fabrica.Veiculo(idFilial: 1);
            var contrato = Fabrica.Contrato();
            veiculo.Locar(contrato);

            veiculo.RegistrarDevolucao(kmFinal: 15_900, idFilialDevolucao: 4, contrato);

            // devolvido não é disponível: o carro ainda precisa de vistoria, limpeza e abastecimento
            Assert.Equal(StatusVeiculo.EmPreparacao, veiculo.Status);
            Assert.False(veiculo.Disponivel);
            Assert.Equal(15_900, veiculo.KmAtual);
            Assert.Equal(4, veiculo.FilialAtualId);
        }

        [Fact]
        public void Devolver_veiculo_que_nao_esta_locado_e_recusado()
        {
            var veiculo = Fabrica.Veiculo();

            Assert.Throws<DomainException>(() => veiculo.RegistrarDevolucao(15_900, 1, Fabrica.Contrato()));
        }

        [Fact]
        public void LiberarDaPreparacao_devolve_o_veiculo_para_a_oferta()
        {
            var veiculo = Fabrica.Veiculo();
            var contrato = Fabrica.Contrato();
            veiculo.Locar(contrato);
            veiculo.RegistrarDevolucao(kmFinal: 15_900, idFilialDevolucao: 1, contrato);

            veiculo.LiberarDaPreparacao();

            Assert.Equal(StatusVeiculo.Disponivel, veiculo.Status);
            Assert.True(veiculo.Disponivel);
        }

        [Fact]
        public void LiberarDaPreparacao_nao_devolve_veiculo_inativo_para_a_oferta()
        {
            var veiculo = Fabrica.Veiculo();
            var contrato = Fabrica.Contrato();
            veiculo.Locar(contrato);
            veiculo.RegistrarDevolucao(kmFinal: 15_900, idFilialDevolucao: 1, contrato);
            veiculo.Desativar();

            veiculo.LiberarDaPreparacao();

            Assert.Equal(StatusVeiculo.Indisponivel, veiculo.Status);
            Assert.False(veiculo.Disponivel);
        }

        [Fact]
        public void LiberarDaPreparacao_de_veiculo_que_nao_esta_em_preparacao_e_recusado()
        {
            var veiculo = Fabrica.Veiculo();

            Assert.Throws<DomainException>(() => veiculo.LiberarDaPreparacao());
        }

        [Fact]
        public void Desativar_veiculo_locado_tira_da_oferta_mas_mantem_o_status()
        {
            var veiculo = Fabrica.Veiculo();
            veiculo.Locar(Fabrica.Contrato());

            veiculo.Desativar();

            // o carro está com o cliente: desativar não o traz de volta, só impede a próxima locação
            Assert.Equal(StatusVeiculo.Locado, veiculo.Status);
            Assert.False(veiculo.Disponivel);
        }

        [Fact]
        public void Ativar_nao_devolve_para_a_oferta_veiculo_que_esta_locado()
        {
            var veiculo = Fabrica.Veiculo();
            veiculo.Locar(Fabrica.Contrato());
            veiculo.Desativar();

            veiculo.Ativar();

            Assert.True(veiculo.Ativo);
            Assert.Equal(StatusVeiculo.Locado, veiculo.Status);
            Assert.False(veiculo.Disponivel);
        }

        [Fact]
        public void Km_nao_retrocede_na_devolucao()
        {
            var veiculo = Fabrica.Veiculo();   // nasce com 15.000
            var contrato = Fabrica.Contrato();
            veiculo.Locar(contrato);

            Assert.Throws<DomainException>(() => veiculo.RegistrarDevolucao(kmFinal: 14_999, idFilialDevolucao: 1, contrato));
        }

        [Fact]
        public void Km_nao_retrocede_na_atualizacao_do_cadastro()
        {
            var veiculo = Fabrica.Veiculo();

            Assert.Throws<DomainException>(() => veiculo.Atualizar(kmAtual: 14_999, idFilialAtual: 1));
        }

        /// <summary>Leva o veículo até o status pedido pelas transições, sem escrever no estado à mão.</summary>
        private static Veiculo EmStatus(StatusVeiculo status)
        {
            var veiculo = Fabrica.Veiculo();

            switch (status)
            {
                case StatusVeiculo.Locado:
                    veiculo.Locar(Fabrica.Contrato());
                    break;
                case StatusVeiculo.EmManutencao:
                    veiculo.IniciarManutencao(TipoManutencao.Corretiva, "Troca de embreagem");
                    break;
                case StatusVeiculo.EmPreparacao:
                    var contrato = Fabrica.Contrato();
                    veiculo.Locar(contrato);
                    veiculo.RegistrarDevolucao(kmFinal: 15_900, idFilialDevolucao: 1, contrato);
                    break;
                case StatusVeiculo.Indisponivel:
                    veiculo.Desativar();
                    break;
            }

            return veiculo;
        }

        [Fact]
        public void IniciarManutencao_indisponibiliza_e_abre_a_ordem()
        {
            var veiculo = Fabrica.Veiculo();

            veiculo.IniciarManutencao(TipoManutencao.Revisao, "Revisão de 30 mil km");

            var manutencao = Assert.Single(veiculo.Manutencoes);
            Assert.Equal(StatusManutencao.Aberta, manutencao.Status);
            Assert.Equal(StatusVeiculo.EmManutencao, veiculo.Status);
            Assert.False(veiculo.Disponivel);
        }

        [Fact]
        public void TerminaManutencao_devolve_veiculo_ativo_para_a_oferta()
        {
            var veiculo = Fabrica.Veiculo();
            veiculo.IniciarManutencao(TipoManutencao.Corretiva, "Troca de embreagem");
            var manutencao = veiculo.Manutencoes.Single();
            Fabrica.DefinirId(manutencao, 10);

            veiculo.TerminaManutencao(custo: 1_250m, idManutencao: 10);

            Assert.Equal(StatusManutencao.Finalizada, manutencao.Status);
            Assert.Equal(1_250m, manutencao.Custo);
            Assert.Equal(StatusVeiculo.Disponivel, veiculo.Status);
            Assert.True(veiculo.Disponivel);
        }

        [Fact]
        public void TerminaManutencao_nao_devolve_veiculo_inativo_para_a_oferta()
        {
            var veiculo = Fabrica.Veiculo();
            veiculo.IniciarManutencao(TipoManutencao.Corretiva, "Troca de embreagem");
            Fabrica.DefinirId(veiculo.Manutencoes.Single(), 10);
            veiculo.Desativar();

            veiculo.TerminaManutencao(custo: 800m, idManutencao: 10);

            Assert.Equal(StatusVeiculo.Indisponivel, veiculo.Status);
            Assert.False(veiculo.Disponivel);
        }

        [Fact]
        public void CancelarManutencao_encerra_a_ordem_sem_custo()
        {
            var veiculo = Fabrica.Veiculo();
            veiculo.IniciarManutencao(TipoManutencao.Preventiva, "Alinhamento");
            var manutencao = veiculo.Manutencoes.Single();
            Fabrica.DefinirId(manutencao, 4);

            veiculo.CancelarManutencao(4);

            Assert.Equal(StatusManutencao.Cancelada, manutencao.Status);
            Assert.Equal(0m, manutencao.Custo);
            Assert.True(veiculo.Disponivel);
        }

        [Fact]
        public void Manutencao_inexistente_e_recusada()
        {
            var veiculo = Fabrica.Veiculo();
            veiculo.IniciarManutencao(TipoManutencao.Preventiva, "Alinhamento");

            Assert.Throws<DomainException>(() => veiculo.TerminaManutencao(100m, idManutencao: 999));
        }
    }
}
