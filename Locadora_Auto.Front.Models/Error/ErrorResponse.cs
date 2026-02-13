using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Locadora_Auto.Front.Models.Error
{
    public class ErrorResponse
    {
        public string? Message { get; set; }
        public string? Title { get; set; }
        public int? Status { get; set; }
    }
}
