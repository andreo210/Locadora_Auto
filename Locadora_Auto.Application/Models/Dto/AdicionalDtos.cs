namespace Locadora_Auto.Application.Models.Dto
{
    public class AdicionalDto
    {
        public int IdAdicional { get; set; }
        public string Nome { get; set; } = null!;
        public decimal ValorDiaria { get; set; }
        public bool Ativo { get; set; }
    }

    public class CriarAtualizarAdicionalDto
    {
        public string Nome { get; set; } = null!;
        public decimal ValorDiaria { get; set; }
    }
    public class LocacaoAdicionalDto
    {
        public int IdAdicional { get; set; }
        public int Quantidade { get; set; }
    }
}
