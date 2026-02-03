namespace Locadora_Auto.Domain.Entidades
{
    public class Veiculo
    {
        public int IdVeiculo { get; private set; }
        public string Placa { get; private set; } = null!;
        public string Marca { get; private set; } = null!;
        public string Modelo { get; private set; } = null!;
        public int Ano { get; private set; }
        public string Chassi { get; private set; } = null!;
        public int IdCategoria { get; private set; }
        public int KmAtual { get; private set; }
        public bool Ativo { get; private set; }
        public bool Disponivel { get; private set; }
        public int FilialAtualId { get; private set; }

        //navegação
        public CategoriaVeiculo Categoria { get; set; } = null!;
        public Filial FilialAtual { get; set; } = null!;
        public ICollection<Locacao> Locacoes { get; set; } = new List<Locacao>();

        public static Veiculo Criar(string placa, string marca, string modelo, int ano, string chassi, int kmAtual,int idCategoria, int idFilialAtual)
        {
            if (string.IsNullOrWhiteSpace(placa))
                throw new InvalidOperationException("placa é obrigatório");
            if (string.IsNullOrWhiteSpace(marca))
                throw new InvalidOperationException("marca é obrigatório");
            if (string.IsNullOrWhiteSpace(modelo))
                throw new InvalidOperationException("modelo é obrigatório");
            if (!int.IsPositive(ano))
                throw new InvalidOperationException("ano tem que ser um numero positivo");
            if (string.IsNullOrWhiteSpace(chassi))
                throw new InvalidOperationException("chassi é obrigatório");
            if (!int.IsPositive(kmAtual))
                throw new InvalidOperationException("kmAtual tem que ser um numero positivo");
            if (!int.IsPositive(idCategoria))
                throw new InvalidOperationException("idCategoria tem que ser um numero positivo");
            if (!int.IsPositive(idFilialAtual))
                throw new InvalidOperationException("idFilialAtual tem que ser um numero positivo");

            return new Veiculo
            {
                Placa = placa.Trim().ToUpper(),
                Marca = marca.Trim().ToUpper(),
                Modelo = modelo.Trim().ToUpper(),
                Ano = ano,
                Chassi = chassi.Trim().ToUpper(),
                KmAtual = kmAtual,
                IdCategoria = idCategoria,
                FilialAtualId = idFilialAtual,
                Ativo = true,
                Disponivel = true
            };
        }

        public void Atualizar(int kmAtual,int idFilialAtual)
        {

            if (!int.IsPositive(kmAtual))
                throw new InvalidOperationException("kmAtual tem que ser um numero positivo");
            if (!int.IsPositive(idFilialAtual))
                throw new InvalidOperationException("idFilialAtual tem que ser um numero positivo");
            KmAtual = kmAtual;
            FilialAtualId = idFilialAtual;            
        }

        public void Ativar()
        {
            Ativo = true;
        }
        public void Desativar()
        {
            Ativo = false;
        }

        public void Disponibilizar()
        {
            Disponivel = true;
        }
        public void Indisponibilizar()
        {
            Disponivel = false;
        }


        public void Valida(string placa, string marca, string modelo, int ano, string chassi, int kmAtual, int idCategoria, int idFilialAtual)
        {
            if (string.IsNullOrWhiteSpace(placa))
                throw new InvalidOperationException("placa é obrigatório");
            if (string.IsNullOrWhiteSpace(marca))
                throw new InvalidOperationException("marca é obrigatório");
            if (string.IsNullOrWhiteSpace(modelo))
                throw new InvalidOperationException("modelo é obrigatório");
            if (!int.IsPositive(ano))
                throw new InvalidOperationException("ano tem que ser um numero positivo");
            if (string.IsNullOrWhiteSpace(chassi))
                throw new InvalidOperationException("chassi é obrigatório");
            if (!int.IsPositive(kmAtual))
                throw new InvalidOperationException("kmAtual tem que ser um numero positivo");
            if (!int.IsPositive(idCategoria))
                throw new InvalidOperationException("idCategoria tem que ser um numero positivo");
            if (!int.IsPositive(idFilialAtual))
                throw new InvalidOperationException("idFilialAtual tem que ser um numero positivo");

        }
    }

}
