namespace Locadora_Auto.Front.Models.Response
{
    public class FilialResponse
    {
        public int IdFilial { get; set; }
        public string? Nome { get; set; }
        public string? Cidade { get; set; }
        public bool Ativo { get; set; }
        public EnderecoResponse? Endereco { get; set; }
        public int TotalVeiculos { get; set; }
        public int VeiculosDisponivel { get; set; }
        public int TotalLocacoesMes { get; set; }
        public DateTime DataCriacao { get; set; }
        public DateTime? DataModificacao { get; set; }
    }
}
