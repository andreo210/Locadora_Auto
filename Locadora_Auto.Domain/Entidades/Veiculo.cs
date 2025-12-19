namespace Locadora_Auto.Domain.Entidades
{
    public class Veiculo
    {
        public int IdVeiculo { get; set; }
        public string Placa { get; set; } = null!;
        public string Chassi { get; set; } = null!;
        public int IdCategoria { get; set; }
        public int KmAtual { get; set; }
        public string Status { get; set; } = null!;
        public int IdFilialAtual { get; set; }

        public CategoriaVeiculo Categoria { get; set; } = null!;
        public Filial FilialAtual { get; set; } = null!;
    }

}
