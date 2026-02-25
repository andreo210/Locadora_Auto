using Locadora_Auto.Front.Models.Request;
using Locadora_Auto.Front.Models.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Locadora_Auto.Front.Services.Servicos.Funcionario
{
    public interface IFuncionarioService
    {
        Task<FuncionarioResponse?> Inserir(FuncionarioRequest request);
    }
}
