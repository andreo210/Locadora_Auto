namespace Locadora_Auto.Front.Models.Request.Seguro
{
    public class EditarSeguroRequest
    {
        public string? Nome { get; set; }
        public string? Descricao { get; set; }
        public decimal ValorDiaria { get; set; }
        public decimal Franquia { get; set; }
        public string? Cobertura { get; set; }
    }
}
