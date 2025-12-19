namespace Locadora_Auto.Application.Models.Dto
{
    public class SeguroDto
    {
        public int IdSeguro { get; set; }
        public string Nome { get; set; } = null!;
        public decimal ValorDiaria { get; set; }
        public decimal Franquia { get; set; }
        public string Descricao { get; set; } = null!;
    }
    public class LocacaoSeguroDto
    {
        public int IdSeguro { get; set; }
    }

}
