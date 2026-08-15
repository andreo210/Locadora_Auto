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

        /// <param name="tempoPreparacaoMinutos">
        /// <c>null</c> deixa o padrão da casa (<c>Filial.PreparacaoPadraoMinutos</c>). Informe só
        /// nos testes em que o tempo de preparação é o objeto da verificação.
        /// </param>
        public static Filial Filial(string nome = "Filial Centro", int? tempoPreparacaoMinutos = null)
            => Domain.Entidades.Filial.Criar(nome, "São Paulo", Endereco(), tempoPreparacaoMinutos);

        public static Veiculo Veiculo(int idCategoria = 1, int idFilial = 1, string placa = "ABC1D23")
            => Domain.Entidades.Veiculo.Criar(placa, "Fiat", "Argo", 2022, "9BWZZZ377VT004251", 15_000, idCategoria, idFilial);

        public static Adicional Adicional(string nome = "Cadeirinha", decimal valorDiaria = 25m)
            => Domain.Entidades.Adicional.Criar(nome, valorDiaria);

        public static Seguro Seguro(string nome = "Proteção Total")
            => Domain.Entidades.Seguro.Criar(nome, "Cobertura ampla", valorDiaria: 40m, franquia: 1500m, cobertura: "Colisão, roubo e terceiros");

        public static Funcionario Funcionario(string matricula = "F-0001", string cargo = "Atendente")
            => Domain.Entidades.Funcionario.Criar(matricula, cargo);

        /// <summary>
        /// Locação já criada, com cliente apto, veículo disponível e funcionário — o mínimo que
        /// <c>Locacao.Criar</c> exige. O teste passa só a peça que ele precisa inspecionar depois
        /// (<c>Fabrica.Locacao(veiculo: veiculo)</c>) ou variar (as datas).
        ///
        /// <paramref name="dataFimPrevista"/> é derivada de <paramref name="dataInicio"/> e não de
        /// um <c>UtcNow</c> novo: <c>CalcularDias</c> arredonda horas para cima, então dois
        /// <c>UtcNow</c> diferentes fariam 72h virar 72,0001h — e o teste esperaria 3 e receberia 4.
        /// </summary>
        public static Locacao Locacao(
            Clientes? cliente = null,
            Veiculo? veiculo = null,
            Funcionario? funcionario = null,
            Reserva? reserva = null,
            DateTime? dataInicio = null,
            DateTime? dataFimPrevista = null,
            int kmInicial = 15_000,
            decimal valorPrevisto = 450m,
            int idFilialRetirada = 1)
        {
            var inicio = dataInicio ?? DateTime.UtcNow;

            return Domain.Entidades.Locacao.Criar(
                cliente ?? Cliente(),
                veiculo ?? Veiculo(),
                funcionario ?? Funcionario(),
                reserva!,
                idFilialRetirada,
                inicio,
                dataFimPrevista ?? inicio.AddDays(3),
                kmInicial,
                valorPrevisto);
        }

        /// <summary>
        /// Contrato descartável, para os testes do <c>Veiculo</c> que precisam apenas satisfazer o
        /// documento de origem que a RN-37 exige nas transições. Ele nasce sobre um veículo
        /// próprio, e não sobre o que está sob teste — quem quer contrato e veículo casados usa
        /// <c>Fabrica.Locacao(veiculo: veiculo)</c>, que é o caminho real.
        /// </summary>
        public static Locacao Contrato() => Locacao();

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
