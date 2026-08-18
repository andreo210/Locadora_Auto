using Locadora_Auto.Domain.Entidades;

namespace Locadora_Auto.Domain.IRepositorio
{
    /// <summary>
    /// Leitura das transferências da RN-48/RN-49 fora do agregado, pelo mesmo motivo do
    /// <see cref="IBloqueioVeiculoRepository"/>: a pergunta do gestor de frota é "que viagens da
    /// rede estão atrasadas", e respondê-la pelo <c>Veiculo</c> carregaria a frota inteira.
    ///
    /// Só de leitura na prática: transferência nasce dentro de <see cref="Veiculo"/>, porque
    /// <c>TransferenciaVeiculo.Criar</c> é internal.
    /// </summary>
    public interface ITransferenciaVeiculoRepository : IRepositorioGlobal<TransferenciaVeiculo>
    {
    }
}
