using Locadora_Auto.Application.Models.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Locadora_Auto.Application.Handlers
{
    public interface ISolicitacaoService
    {
        Task CriarSolicitacaoAsync(SolicitacaoDto solicitacao);
    }
}
