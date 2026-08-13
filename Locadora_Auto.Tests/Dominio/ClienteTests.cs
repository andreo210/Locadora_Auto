using Locadora_Auto.Domain.Entidades;
using Locadora_Auto.Tests.Fabricas;
using Xunit;

namespace Locadora_Auto.Tests.Dominio
{
    public class ClienteTests
    {
        [Fact]
        public void Criar_nasce_habilitado_e_ativo()
        {
            var cliente = Fabrica.Cliente();

            Assert.True(cliente.Ativo);
            Assert.Equal(StatusCliente.Habilitado, cliente.Status);
            Assert.Equal(0, cliente.TotalLocacoes);
            Assert.NotNull(cliente.Endereco);
        }

        [Fact]
        public void Criar_sem_habilitacao_e_recusado()
        {
            Assert.Throws<InvalidOperationException>(() =>
                Clientes.Criar("  ", DateTime.Today.AddYears(1), Fabrica.Endereco()));
        }

        [Fact]
        public void Criar_sem_endereco_e_recusado()
        {
            Assert.Throws<InvalidOperationException>(() =>
                Clientes.Criar("12345678900", DateTime.Today.AddYears(1), null!));
        }

        [Fact]
        public void PodeLocar_quando_habilitado_e_com_cnh_valida()
        {
            var cliente = Fabrica.Cliente(validadeCnh: DateTime.Today.AddYears(1));

            Assert.True(cliente.PodeLocar());
        }

        [Fact]
        public void Nao_pode_locar_com_cnh_vencida()
        {
            var cliente = Fabrica.Cliente(validadeCnh: DateTime.Today.AddDays(-1));

            Assert.False(cliente.PodeLocar());
        }

        [Fact]
        public void Nao_pode_locar_bloqueado()
        {
            var cliente = Fabrica.Cliente();
            cliente.Bloquear();

            Assert.False(cliente.PodeLocar());
            Assert.Equal(StatusCliente.Bloqueado, cliente.Status);
        }

        [Fact]
        public void Nao_pode_locar_inadimplente()
        {
            var cliente = Fabrica.Cliente();
            cliente.MarcarInadimplente();

            Assert.False(cliente.PodeLocar());
        }

        [Fact]
        public void Regularizar_devolve_o_direito_de_locar()
        {
            var cliente = Fabrica.Cliente();
            cliente.MarcarInadimplente();

            cliente.Regularizar();

            Assert.True(cliente.PodeLocar());
        }

        [Fact]
        public void Reservas_so_entram_pela_raiz()
        {
            var cliente = Fabrica.Cliente();

            cliente.ReservarVeiculo(1, Fabrica.DaquiADias(2), Fabrica.DaquiADias(4), idFilial: 1, idCategoria: 1);

            Assert.Single(cliente.Reservas);

            // a coleção é somente leitura: quem quiser reservar passa pelo método que valida
            Assert.IsAssignableFrom<IReadOnlyCollection<Reserva>>(cliente.Reservas);
        }
    }
}
