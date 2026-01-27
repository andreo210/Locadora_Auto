namespace Locadora_Auto.Domain.Entidades
{
    public class Filial
    {
        public int IdFilial { get; set; }
        public string Nome { get; set; } = null!;
        public string Cidade { get; set; } = null!;
        public bool Ativo { get; set; }

        public int IdEndereco { get; set; }
        public Endereco Endereco { get; set; } = null!;

        public ICollection<Veiculo> Veiculos { get; set; } = new List<Veiculo>();
        public ICollection<Locacao> LocacoesRetirada { get; set; } = new List<Locacao>();
        public ICollection<Locacao> LocacoesDevolucao { get; set; } = new List<Locacao>();
    }

}
