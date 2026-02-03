namespace Locadora_Auto.Application.Models.Dto
{
    public abstract class SeguroBaseDto
    {
        public string Nome { get; set; } = null!;
        public decimal ValorDiaria { get; set; }
        public decimal Franquia { get; set; }
        public string Cobertura { get; set; } = null!;
        public string Descricao { get; set; } = null!;
    }
    public class SeguroDto : SeguroBaseDto
    {
        public int IdSeguro { get; set; }
    }

    public class CriarOuAtualizarSeguroDto : SeguroBaseDto
    {
    }
    public class LocacaoSeguroDto
    {
        public int IdSeguro { get; set; }
    }



}
