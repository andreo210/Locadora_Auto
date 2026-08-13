using Locadora_Auto.Domain.Entidades;
using Locadora_Auto.Tests.Fakes;

namespace Locadora_Auto.Tests.Fabricas
{
    /// <summary>
    /// Entidades válidas para os testes, com valores padrão que passam nas validações de
    /// <c>Criar</c>. O teste só informa o que é relevante para o caso que está verificando —
    /// assim, quando uma validação nova entra na entidade, corrige-se um lugar só.
    /// </summary>
    public static class Fabrica
    {
        /// <summary>Data segura para "futuro": as entidades validam contra <c>DateTime.UtcNow</c>.</summary>
        public static DateTime DaquiADias(int dias) => DateTime.UtcNow.AddDays(dias);

        public static Endereco Endereco(string cidade = "São Paulo")
            => Domain.Entidades.Endereco.Criar("Rua das Flores", "100", "Centro", cidade, "SP", "01001-000");

        public static Clientes Cliente(bool ativo = true, DateTime? validadeCnh = null)
        {
            var cliente = Clientes.Criar(
                numeroHabilitacao: "12345678900",
                validadeCnh: validadeCnh ?? DateTime.Today.AddYears(2),
                endereco: Endereco());

            if (!ativo) cliente.Desativar();

            return cliente;
        }

        public static CategoriaVeiculo Categoria(string nome = "Hatch")
            => CategoriaVeiculo.Criar(nome, valorDiaria: 150m, limiteKm: 200, valorKmExcedente: 2m);

        public static Filial Filial(string nome = "Filial Centro")
            => Domain.Entidades.Filial.Criar(nome, "São Paulo", Endereco());

        public static Veiculo Veiculo(int idCategoria = 1, int idFilial = 1, string placa = "ABC1D23")
            => Domain.Entidades.Veiculo.Criar(placa, "Fiat", "Argo", 2022, "9BWZZZ377VT004251", 15_000, idCategoria, idFilial);

        public static Adicional Adicional(string nome = "Cadeirinha", decimal valorDiaria = 25m)
            => Domain.Entidades.Adicional.Criar(nome, valorDiaria);

        public static Seguro Seguro(string nome = "Proteção Total")
            => Domain.Entidades.Seguro.Criar(nome, "Cobertura ampla", valorDiaria: 40m, franquia: 1500m, cobertura: "Colisão, roubo e terceiros");

        /// <summary>
        /// Cria uma reserva pela raiz do agregado (única porta de entrada, já que
        /// <c>Reserva.Criar</c> é internal) e devolve as duas pontas para o teste.
        /// </summary>
        public static (Clientes cliente, Reserva reserva) ClienteComReserva(
            int idFilial = 1,
            int idCategoria = 1,
            int idReserva = 1)
        {
            var cliente = Cliente();
            DefinirId(cliente, 1);

            cliente.ReservarVeiculo(cliente.IdCliente, DaquiADias(3), DaquiADias(6), idFilial, idCategoria);

            var reserva = cliente.Reservas.Single();
            DefinirId(reserva, idReserva);

            return (cliente, reserva);
        }

        /// <summary>
        /// Escreve na chave primária mesmo com set privado. Em teste isso é necessário porque o id
        /// normalmente viria do banco, e sem ele todo filtro por id casaria com a entidade errada.
        /// </summary>
        public static void DefinirId(object entidade, int id) => ChavePrimaria.Definir(entidade, id);
    }
}
