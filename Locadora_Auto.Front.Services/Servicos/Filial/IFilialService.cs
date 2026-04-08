using Locadora_Auto.Front.Models.Request.Filial;
using Locadora_Auto.Front.Models.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Locadora_Auto.Front.Services.Servicos.Filial
{
    public interface IFilialService
    {
        Task<FilialResponse?> Inserir(CriarFilialRequest request);
    }
}
