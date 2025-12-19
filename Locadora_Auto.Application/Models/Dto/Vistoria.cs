namespace Locadora_Auto.Application.Models.Dto
{
    public class VistoriaDto
    {
        public int IdVistoria { get; set; }
        public int IdLocacao { get; set; }
        public string Tipo { get; set; } = null!;
        public string? Observacoes { get; set; }
        public DateTime DataVistoria { get; set; }
    }

}
