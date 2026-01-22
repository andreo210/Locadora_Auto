using Locadora_Auto.Application.Models.Dto.Locadora_Auto.Application.Models.Dto;

namespace Locadora_Auto.Application.Models.Dto
{
    public class CriarVeiculoDto
    {
        public string Placa { get; set; } = null!;
        public string Marca { get; set; } = null!;
        public string Modelo { get; set; } = null!;
        public int Ano { get; set; }
        public string Chassi { get; set; } = null!;
        public int KmInicial { get; set; }

        public int IdCategoria { get; set; }
        public int IdFilialAtual { get; set; }
    }


    public class VeiculoDto
    {
        public int IdVeiculo { get; set; }
        public string Placa { get; set; } = null!;
        public string Marca { get; set; } = null!;
        public string Modelo { get; set; } = null!;
        public int Ano { get; set; }
        public int KmAtual { get; set; }

        public bool Ativo { get; set; }
        public bool Disponivel { get; set; }

        public int IdCategoria { get; set; }
        public string Categoria { get; set; } = null!;

        public int IdFilialAtual { get; set; }
        public string Filial { get; set; } = null!;
    }
    public class AtualizarVeiculoDto
    {
        public string? Marca { get; set; }
        public string? Modelo { get; set; }
        public int? Ano { get; set; }
        public int? KmAtual { get; set; }
        public int? IdFilialAtual { get; set; }
    }


}
