namespace Locadora_Auto.Front.Models.Tabelas
{
    public class AcaoTabela<TItem>
    {
        public string Titulo { get; set; } = string.Empty;
        public string Icone { get; set; } = string.Empty;
        public string Cor { get; set; } = "primary";
        public Func<TItem, Task> Acao { get; set; } = null!;
    }

    public class AcaoEmMassa
    {
        public string Titulo { get; set; } = string.Empty;
        public string Icone { get; set; } = string.Empty;
        public string Cor { get; set; } = "primary";
        public Func<List<string>, Task> Acao { get; set; } = null!;
        public bool MostrarQuantidade { get; set; } = true;
    }
}
