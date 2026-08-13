namespace {{RootNamespace}}.Front.Models.Tabelas
{
    /// <summary>Botão exibido na coluna de ações de cada linha.</summary>
    public class AcaoTabela<TItem>
    {
        /// <summary>Vira o <c>title</c> do botão — é o que o usuário lê no hover.</summary>
        public string Titulo { get; set; } = string.Empty;

        /// <summary>Classe do Bootstrap Icons (ex.: <c>bi-pencil</c>).</summary>
        public string Icone { get; set; } = string.Empty;

        /// <summary>Sufixo de <c>btn-outline-*</c> (ex.: <c>primary</c>, <c>danger</c>).</summary>
        public string Cor { get; set; } = "primary";

        public Func<TItem, Task> Acao { get; set; } = null!;
    }

    /// <summary>Item do menu que aparece quando há linhas selecionadas.</summary>
    public class AcaoEmMassa
    {
        public string Titulo { get; set; } = string.Empty;

        public string Icone { get; set; } = string.Empty;

        public string Cor { get; set; } = "primary";

        /// <summary>Recebe os ids selecionados como texto — converta na página.</summary>
        public Func<List<string>, Task> Acao { get; set; } = null!;

        public bool MostrarQuantidade { get; set; } = true;
    }
}
