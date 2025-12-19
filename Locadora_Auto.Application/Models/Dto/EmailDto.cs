using Microsoft.AspNetCore.Http;

namespace Locadora_Auto.Application.Models.Dto
{
    public class EmailDto
    {
        public string? ToEmail { get; set; }
        public string? Subject { get; set; }
        public string? Body { get; set; }
        public List<IFormFile>? Attachments { get; set; }
    }
}
