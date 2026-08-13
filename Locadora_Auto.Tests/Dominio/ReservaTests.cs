using Locadora_Auto.Domain.Entidades;
using Locadora_Auto.Tests.Fabricas;
using Xunit;

namespace Locadora_Auto.Tests.Dominio
{
    /// <summary>
    /// Reserva é entidade do agregado Cliente: <c>Reserva.Criar</c> é internal e a raiz é a única
    /// porta de entrada. Por isso os testes de criação passam por <c>Clientes.ReservarVeiculo</c> —
    /// testar pela porta que a aplicação usa é o que mantém a invariante honesta.
    /// </summary>
    public class ReservaTests
    {
        [Fact]
        public void Criar_pela_raiz_nasce_reservada_e_ativa()
        {
            var (cliente, reserva) = Fabrica.ClienteComReserva(idFilial: 7, idCategoria: 3);

            Assert.Single(cliente.Reservas);
            Assert.Equal(StatusReserva.Reservado, reserva.Status);
            Assert.True(reserva.Ativo);
            Assert.Equal(7, reserva.IdFilial);
            Assert.Equal(3, reserva.IdCategoria);
        }

        [Fact]
        public void Criar_com_data_de_inicio_no_passado_e_recusado()
        {
            var cliente = Fabrica.Cliente();

            var erro = Assert.Throws<InvalidOperationException>(() =>
                cliente.ReservarVeiculo(1, Fabrica.DaquiADias(-1), Fabrica.DaquiADias(5), 1, 1));

            Assert.Contains("inicio", erro.Message);
        }

        [Fact]
        public void Criar_com_fim_anterior_ao_inicio_e_recusado()
        {
            var cliente = Fabrica.Cliente();

            Assert.Throws<InvalidOperationException>(() =>
                cliente.ReservarVeiculo(1, Fabrica.DaquiADias(5), Fabrica.DaquiADias(2), 1, 1));
        }

        [Fact]
        public void Criar_sem_cliente_e_recusado()
        {
            var cliente = Fabrica.Cliente();

            Assert.Throws<InvalidOperationException>(() =>
                cliente.ReservarVeiculo(0, Fabrica.DaquiADias(2), Fabrica.DaquiADias(5), 1, 1));
        }

        [Fact]
        public void Cancelar_encerra_a_reserva()
        {
            var (_, reserva) = Fabrica.ClienteComReserva();

            reserva.Cancelar();

            Assert.Equal(StatusReserva.Cancelado, reserva.Status);
            Assert.False(reserva.Ativo);
        }

        [Fact]
        public void Cancelar_duas_vezes_e_recusado()
        {
            var (_, reserva) = Fabrica.ClienteComReserva();
            reserva.Cancelar();

            var erro = Assert.Throws<DomainException>(() => reserva.Cancelar());

            Assert.Contains("ativas", erro.Message);
        }

        [Fact]
        public void Finalizar_so_vale_para_reserva_ativa()
        {
            var (_, reserva) = Fabrica.ClienteComReserva();
            reserva.Finalizar();

            Assert.Equal(StatusReserva.Finalizado, reserva.Status);
            Assert.False(reserva.Ativo);
            Assert.Throws<DomainException>(() => reserva.Finalizar());
        }

        [Fact]
        public void Cancelar_reserva_finalizada_e_recusado()
        {
            var (_, reserva) = Fabrica.ClienteComReserva();
            reserva.Finalizar();

            Assert.Throws<DomainException>(() => reserva.Cancelar());
        }

        [Fact]
        public void Expirar_depois_da_data_de_inicio_marca_como_expirada()
        {
            var (_, reserva) = Fabrica.ClienteComReserva();   // início daqui a 3 dias

            reserva.Expirar(Fabrica.DaquiADias(5));

            Assert.Equal(StatusReserva.Expirado, reserva.Status);
            Assert.False(reserva.Ativo);
        }

        [Fact]
        public void Expirar_no_mesmo_dia_do_inicio_nao_expira()
        {
            var (_, reserva) = Fabrica.ClienteComReserva();

            // a comparação é por dia inteiro: no próprio dia do início a reserva continua valendo
            reserva.Expirar(reserva.DataInicio);

            Assert.Equal(StatusReserva.Reservado, reserva.Status);
            Assert.True(reserva.Ativo);
        }

        [Fact]
        public void Expirar_nao_mexe_em_reserva_ja_cancelada()
        {
            var (_, reserva) = Fabrica.ClienteComReserva();
            reserva.Cancelar();

            reserva.Expirar(Fabrica.DaquiADias(10));

            Assert.Equal(StatusReserva.Cancelado, reserva.Status);
        }
    }
}
