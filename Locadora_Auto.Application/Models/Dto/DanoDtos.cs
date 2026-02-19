namespace Locadora_Auto.Application.Models.Dto
{
    public class DanoDto
    {
        public int IdDano { get; set; }
        public int IdVistoria { get; set; }
        public string Descricao { get; set; } = null!;
        public decimal ValorEstimado { get; set; }
        public bool CobertoSeguro { get; set; }
    }

    public class CriarDanoDto
    {
        public int IdVistoria { get; set; }
        public string Descricao { get; set; } = null!;
        public decimal ValorEstimado { get; set; }
        public int codigoTipoDano { get; set; }
    }
    public class RemoverDanoDto
    {
        public int IdVistoria { get; set; }
        public int IdDano { get; set; }
    }

}
