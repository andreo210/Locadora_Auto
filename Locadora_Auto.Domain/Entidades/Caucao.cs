namespace Locadora_Auto.Domain.Entidades
{
    public class Caucao
    {
        public int IdCaucao { get; set; }
        public int IdLocacao { get; set; }
        public decimal Valor { get; set; }
        public string Status { get; set; } = null!;

        public Locacao Locacao { get; set; } = null!;
    }

}
