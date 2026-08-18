using Locadora_Auto.Domain.Entidades;

namespace Locadora_Auto.Domain.IRepositorio
{
    /// <summary>
    /// Leitura dos bloqueios da RN-52 fora do agregado.
    ///
    /// Existe pelo indicador de bloqueios vencidos (seção 12): a pergunta dele é "que carros da
    /// frota inteira passaram do prazo", e respondê-la pelo <c>Veiculo</c> obrigaria a carregar
    /// todos os veículos com <c>Include(v =&gt; v.Bloqueios)</c> só para descartar quase tudo.
    ///
    /// Só de leitura na prática: bloqueio nasce dentro de <see cref="Veiculo"/>, porque
    /// <c>BloqueioVeiculo.Criar</c> é internal. Os métodos de escrita que vêm do genérico não têm
    /// chamador e não devem ganhar um — abrir bloqueio pela borda deixaria o status do veículo e o
    /// documento fora de sincronia, que é exatamente o que a RN-37 proíbe.
    /// </summary>
    public interface IBloqueioVeiculoRepository : IRepositorioGlobal<BloqueioVeiculo>
    {
    }
}
