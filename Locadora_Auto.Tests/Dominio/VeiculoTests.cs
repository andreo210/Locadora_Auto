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
