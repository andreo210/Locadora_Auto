namespace Locadora_Auto.Front.Models.Request.Veiculo
{
    public class CriarVeiculoRequest
    {
        public string? Placa { get; set; }
        public string? Marca { get; set; }
        public string? Modelo { get; set; }
        public int Ano { get; set; } = DateTime.UtcNow.Year;
        public string? Chassi { get; set; }
        public int KmInicial { get; set; }

        public int IdCategoria { get; set; }
        public int IdFilialAtual { get; set; }
    }
}
