using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Locadora_Auto.Application.Models.Dto
{
    public class FotoDto
    {
        public int? IdFoto { get; set; }
        public int? IdEntidade { get; set; }
        public string? Entidade { get; set; }
        public string? NomeArquivo { get; set; }
        public string? Raiz { get; set; }
        public string? Diretorio { get; set; }
        public string? Extensao { get; set; }
        public DateTime DataUpload { get; set; }
        public long? QuantidadeBytes { get; set; }
    }

    public class EnviarFotoDto
    {
        public int IdTipo{ get; set; }
        public int IdEntidade { get; set; }
        public List<IFormFile> file { get; set; }
    }
}
