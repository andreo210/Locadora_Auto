namespace Locadora_Auto.Application.Models.Dto
{
    public class MultaDto
    {
        public int IdMulta { get; set; }
        public string? Tipo { get; set; }
        public decimal Valor { get; set; }
        public int IdLocacao { get; set; }
        public string? Status { get; set; }

    }
    public class CriarMultaDto
    {
        public int Tipo { get; set; }
        public decimal Valor { get; set; }
    }
    public class CompensarMultaDto
    {
        public int IdMulta { get; set; }
        public int IdLocacao { get; set; }
    }
}
