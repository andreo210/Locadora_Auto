namespace Locadora_Auto.Application.Models.Dto
{
    public class FuncionarioDto
    {
        public int IdFuncionario { get; set; }
        public string Matricula { get; set; } = null!;
        public string Nome { get; set; } = null!;
        public string? Cargo { get; set; }
        public string Status { get; set; } = null!;
    }

}
