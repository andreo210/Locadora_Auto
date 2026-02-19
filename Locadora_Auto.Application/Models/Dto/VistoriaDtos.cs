using Microsoft.AspNetCore.Http;

namespace Locadora_Auto.Application.Models.Dto
{

    public class VistoriaBaseDto
    {

        public int IdFuncionario { get; set; }      
        public int KmVeiculo { get; set; }
        public string? Observacoes { get; set; }
    }
    public class VistoriaDto : VistoriaBaseDto
    {
        public string? Tipo { get; set; }
        public string? NivelCombustivel { get; set; }
        public int IdVistoria { get; set; }
        public DateTime DataVistoria { get; set; }
        public int IdLocacao { get; set; }
    }

    public class CriarVistoriaDto : VistoriaBaseDto
    {
        public int Tipo { get; set; }
        public int NivelCombustivel { get; set; }
    }

    public class EnviarFotoVistoriaDto
    {
        public int IdVistoria { get; set; }
        public List<IFormFile>? Fotos { get; set; }
    }


}
