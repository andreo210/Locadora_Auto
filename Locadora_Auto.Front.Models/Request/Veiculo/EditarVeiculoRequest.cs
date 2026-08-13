namespace Locadora_Auto.Front.Models.Request.Veiculo
{
    public class EditarVeiculoRequest
    {
        public string? Marca { get; set; }
        public string? Modelo { get; set; }
        public int? Ano { get; set; }
        public int? KmAtual { get; set; }
        public int? IdFilialAtual { get; set; }
    }
}
