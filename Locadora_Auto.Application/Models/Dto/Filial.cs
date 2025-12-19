namespace Locadora_Auto.Application.Models.Dto
{
    public class FilialDto
    {
        public int IdFilial { get; set; }
        public string Nome { get; set; } = null!;
        public string Cidade { get; set; } = null!;
        public bool Ativo { get; set; }
    }
}
