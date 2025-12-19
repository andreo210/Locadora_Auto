namespace Locadora_Auto.Application.Models.Dto
{
    public class CaucaoDto
    {
        public int IdCaucao { get; set; }
        public decimal Valor { get; set; }
        public string Status { get; set; } = null!;
    }

}
