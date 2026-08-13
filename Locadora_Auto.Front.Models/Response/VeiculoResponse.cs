namespace Locadora_Auto.Front.Models.Response
{
    public class VeiculoResponse
    {
        public int IdVeiculo { get; set; }
        public string? Placa { get; set; }
        public string? Marca { get; set; }
        public string? Modelo { get; set; }
        public int Ano { get; set; }
        public string? Chassi { get; set; }
        public int KmAtual { get; set; }
        public int IdStatus { get; set; }
        public string? Status { get; set; }
        public bool Ativo { get; set; }
        public bool Disponivel { get; set; }

        public int IdCategoria { get; set; }
        public string? Categoria { get; set; }

        public int IdFilialAtual { get; set; }
        public string? Filial { get; set; }

        public string Descricao => $"{Marca} {Modelo}".Trim();
    }
}
