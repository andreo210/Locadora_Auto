using Locadora_Auto.Domain.Entidades;

namespace Locadora_Auto.Domain.IRepositorio
{
    /// <summary>
    /// Leitura da trilha do ativo (RN-37). Existe para consultar movimento sem carregar o agregado
    /// inteiro — a trilha de um carro velho tem centenas de linhas, e paginar dentro de um
    /// <c>Include</c> não é possível.
    ///
    /// Só de leitura na prática: movimento nasce dentro de <see cref="Veiculo"/>, porque
    /// <c>MovimentoVeiculo.Criar</c> é internal. Os métodos de escrita que vêm do genérico não têm
    /// chamador e não devem ganhar um — gravar movimento pela borda é o "status trocado à mão" que
    /// a RN-37 existe para impedir.
    /// </summary>
    public interface IMovimentoVeiculoRepository : IRepositorioGlobal<MovimentoVeiculo>
    {
    }
}
