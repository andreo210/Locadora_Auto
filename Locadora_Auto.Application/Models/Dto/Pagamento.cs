namespace Locadora_Auto.Application.Models.Dto
{
    public class AdicionarPagamentoDto
    {
        public int IdFormaPagamento { get; set; }
        public decimal Valor { get; set; }
    }

    public class PagamentoDto
    {
        public int IdPagamento { get; set; }
        public decimal Valor { get; set; }
        public DateTime DataPagamento { get; set; }
        public string Status { get; set; } = null!;
        public string? FormaPagamento { get; set; }
    }

    public class FormaPagamentoDto
    {
        public int IdFormaPagamento { get; set; }
        public string Descricao { get; set; } = null!;
    }


}
