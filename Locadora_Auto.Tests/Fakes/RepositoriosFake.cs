using Locadora_Auto.Domain.Entidades;
using Locadora_Auto.Domain.IRepositorio;

namespace Locadora_Auto.Tests.Fakes
{
    /*
     * As interfaces concretas de repositório não declaram nada além de IRepositorioGlobal<T>,
     * então cada fake tipado é só a amarração do genérico com a interface que o serviço pede.
     * Se algum dia uma interface ganhar um método próprio, é aqui que ele entra.
     */

    public class AdicionalRepositoryFake : RepositorioFake<Adicional>, IAdicionalRepository
    {
        public AdicionalRepositoryFake(ArmazemFake? armazem = null) : base(armazem) { }
    }

    public class SeguroRepositoryFake : RepositorioFake<Seguro>, ISeguroRepository
    {
        public SeguroRepositoryFake(ArmazemFake? armazem = null) : base(armazem) { }
    }

    public class ReservaRepositoryFake : RepositorioFake<Reserva>, IReservaRepository
    {
        public ReservaRepositoryFake(ArmazemFake? armazem = null) : base(armazem) { }
    }

    public class CategoriaVeiculosRepositoryFake : RepositorioFake<CategoriaVeiculo>, ICategoriaVeiculosRepository
    {
        public CategoriaVeiculosRepositoryFake(ArmazemFake? armazem = null) : base(armazem) { }
    }

    public class FilialRepositoryFake : RepositorioFake<Filial>, IFilialRepository
    {
        public FilialRepositoryFake(ArmazemFake? armazem = null) : base(armazem) { }
    }

    public class VeiculosRepositoryFake : RepositorioFake<Veiculo>, IVeiculosRepository
    {
        public VeiculosRepositoryFake(ArmazemFake? armazem = null) : base(armazem) { }
    }

    public class LocacaoRepositoryFake : RepositorioFake<Locacao>, ILocacaoRepository
    {
        public LocacaoRepositoryFake(ArmazemFake? armazem = null) : base(armazem) { }
    }

    /// <summary>
    /// Cliente é raiz do agregado que contém as reservas, então este fake reproduz o cascade
    /// do EF: reserva criada por <c>Clientes.ReservarVeiculo</c> só existe dentro da coleção da
    /// raiz, e sem a propagação a consulta seguinte do serviço não a encontraria.
    /// </summary>
    public class ClienteRepositoryFake : RepositorioFake<Clientes>, IClienteRepository
    {
        public ClienteRepositoryFake(ArmazemFake? armazem = null) : base(armazem) { }

        public override Task<int> SalvarAsync(CancellationToken ct = default)
        {
            var reservas = Armazem.Tabela<Reserva>();

            foreach (var cliente in Tabela)
            {
                foreach (var reserva in cliente.Reservas)
                {
                    if (reservas.Contains(reserva)) continue;

                    ChavePrimaria.AtribuirSeVazia(reserva, Armazem.ProximoId<Reserva>());
                    reservas.Add(reserva);
                }
            }

            return base.SalvarAsync(ct);
        }
    }
}
