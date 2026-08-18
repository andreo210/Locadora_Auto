using Locadora_Auto.Domain.Entidades;

namespace Locadora_Auto.Domain.IRepositorio
{
    /// <summary>
    /// Grava e conta as tentativas de sobreposição recusadas (seção 12). É o único destes
    /// repositórios do bloco do ativo que realmente escreve: <see cref="RecusaSobreposicao"/> não
    /// pertence ao agregado do veículo — ela registra um fato do balcão, e quem o observa é o
    /// serviço de locação, que a recusou.
    /// </summary>
    public interface IRecusaSobreposicaoRepository : IRepositorioGlobal<RecusaSobreposicao>
    {
    }
}
