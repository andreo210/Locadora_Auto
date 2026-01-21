using Locadora_Auto.Application.Models.Dto.Locadora_Auto.Application.Models.Dto;

namespace Locadora_Auto.Application.Models.Dto
{
    public class VeiculoCreateDto
    {
        public string Placa { get; set; } = null!;
        public string Chassi { get; set; } = null!;
        public int IdCategoria { get; set; }
        public int IdFilialAtual { get; set; }
        public int KmAtual { get; set; }
    }

    public class VeiculoDto
    {
        public int IdVeiculo { get; set; }
        public string Placa { get; set; } = null!;
        public string Chassi { get; set; } = null!;
        public int KmAtual { get; set; }
        public string Status { get; set; } = null!;
        public CategoriaVeiculoDto Categoria { get; set; } = null!;
        public FilialDto FilialAtual { get; set; } = null!;
    }

}
