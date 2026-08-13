using Locadora_Auto.Domain.Entidades;
using Locadora_Auto.Tests.Fabricas;
using Xunit;

namespace Locadora_Auto.Tests.Dominio
{
    public class AdicionalTests
    {
        [Fact]
        public void Criar_nasce_ativo()
        {
            var adicional = Fabrica.Adicional("Cadeirinha", 25m);

            Assert.Equal("Cadeirinha", adicional.Nome);
            Assert.Equal(25m, adicional.ValorDiaria);
            Assert.True(adicional.Ativo);
        }

        [Fact]
        public void Criar_com_valor_negativo_e_recusado()
        {
            Assert.Throws<DomainException>(() => Adicional.Criar("GPS", -1m));
        }

        [Fact]
        public void Criar_com_valor_zero_e_aceito()
        {
            // adicional de cortesia é caso válido: só valor negativo é recusado
            var adicional = Adicional.Criar("Segundo condutor", 0m);

            Assert.Equal(0m, adicional.ValorDiaria);
        }

        [Fact]
        public void Atualizar_sem_nome_e_recusado()
        {
            var adicional = Fabrica.Adicional();

            Assert.Throws<DomainException>(() => adicional.Atualizar("", 30m));
        }

        [Fact]
        public void Atualizar_com_valor_negativo_e_recusado()
        {
            var adicional = Fabrica.Adicional();

            Assert.Throws<DomainException>(() => adicional.Atualizar("GPS", -5m));
        }

        [Fact]
        public void Desativar_e_ativar_alternam_a_oferta()
        {
            var adicional = Fabrica.Adicional();

            adicional.Desativar();
            Assert.False(adicional.Ativo);

            adicional.Ativar();
            Assert.True(adicional.Ativo);
        }
    }
}
