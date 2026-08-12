namespace Locadora_Auto.Front.Models.Request.Veiculo
{
    public class IniciarManutencaoRequest
    {
        public int IdTipoManutencao { get; set; }
        public string? Descricao { get; set; }
    }

    public class TerminarManutencaoRequest
    {
        public int IdManutencao { get; set; }
        public decimal Custo { get; set; }
    }

    public class AtualizarManutencaoRequest
    {
        public int IdManutencao { get; set; }
        public string? Descricao { get; set; }
    }
}
