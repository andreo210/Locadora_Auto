namespace Locadora_Auto.Domain.Entidades
{
    public class Filial
    {
        public int IdFilial { get; private set; }
        public string Nome { get; private set; }
        public string Cidade { get; private set; }
        public bool Ativo { get; private set; }

        public int IdEndereco { get; set; }
        public Endereco Endereco { get; private set; } = null!;

        public ICollection<Veiculo> Veiculos { get; set; } = new List<Veiculo>();
        public ICollection<Locacao> LocacoesRetirada { get; set; } = new List<Locacao>();
        public ICollection<Locacao> LocacoesDevolucao { get; set; } = new List<Locacao>();

        public static Filial Criar(string nome, string cidade, Endereco endereco)
        {
            if (string.IsNullOrWhiteSpace(nome))
                throw new InvalidOperationException("Nome é obrigatório");
            if (string.IsNullOrWhiteSpace(cidade))
                throw new InvalidOperationException("cidade é obrigatório");

            if (endereco == null)
                throw new InvalidOperationException("endereço não pode ser nulo");

            return new Filial
            {
                Nome = nome,
                Cidade = cidade,
                Ativo = true,
                Endereco = Endereco.Criar(endereco.Logradouro, endereco.Numero, endereco.Bairro, endereco.Cidade, endereco.Estado, endereco.Cep, endereco.Complemento)
            };            
        }

        public void Atualizar(string nome, string cidade, Endereco endereco)
        {
            if (string.IsNullOrWhiteSpace(nome))
                throw new InvalidOperationException("Nome é obrigatório");
            if (string.IsNullOrWhiteSpace(cidade))
                throw new InvalidOperationException("cidade é obrigatório");
            if (endereco == null)
                throw new InvalidOperationException("endereço não pode ser nulo");

            Nome = nome;
            Cidade = cidade;
            Endereco.Atualizar(endereco.Logradouro, endereco.Numero, endereco.Bairro, endereco.Cidade, endereco.Estado, endereco.Cep, endereco.Complemento);
        }

        public void Ativar()
        {
            Ativo = true;
        }
        public void Desativar()
        {
            Ativo = false;
        }
    }

}
