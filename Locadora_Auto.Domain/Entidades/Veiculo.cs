namespace Locadora_Auto.Domain.Entidades
{
    public class Veiculo
    {
        public int IdVeiculo { get; set; }
        public string Placa { get; set; } = null!;
        public string Marca { get; set; } = null!;
        public string Modelo { get; set; } = null!;
        public int Ano { get; set; }
        public string Chassi { get; set; } = null!;
        public int IdCategoria { get; set; }
        public int KmAtual { get; set; }
        public bool Ativo { get; set; }
        public bool Disponivel { get; set; }
        public int FilialAtualId { get; set; }

        public CategoriaVeiculo Categoria { get; set; } = null!;
        public Filial FilialAtual { get; set; } = null!;
    }

}
