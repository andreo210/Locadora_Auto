using Microsoft.AspNetCore.Http;

namespace Locadora_Auto.Application.Models.Dto
{

    public class VistoriaBaseDto
    {

        public int IdFuncionario { get; set; }
        public int KmVeiculo { get; set; }
        public string? Observacoes { get; set; }

        /// <summary>
        /// RN-23: o vistoriador declara que o carro voltou precisando de limpeza especial. Só vale
        /// na vistoria de devolução, e sozinho não cobra nada — o fechamento também exige ao menos
        /// uma foto na mesma vistoria.
        /// </summary>
        public bool RequerLimpezaEspecial { get; set; }
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
