using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Locadora_Auto.Application.Models.Dto
{
    public class DocumentosDto
    {
        public IFormFile? File { get; set; }
        public int? IdTipoDocumento { get; set; }

        public DocumentosDto() { }
        public DocumentosDto(IFormFile? file, int? idTipoDocumento)
        {
            File = file;
            IdTipoDocumento = idTipoDocumento;
        }
    }
}
