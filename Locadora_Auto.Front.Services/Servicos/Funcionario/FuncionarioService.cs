using Locadora_Auto.Front.Models.Request;
using Locadora_Auto.Front.Models.Response;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Locadora_Auto.Front.Services.Servicos.Funcionario
{
    public class FuncionarioService : IFuncionarioService
    {
        private readonly IApiHttpService _api;

        public FuncionarioService(IApiHttpService api)
        {
            _api = api;
        }

        public async Task<FuncionarioResponse?> Inserir(FuncionarioRequest request)
        {
            var (objeto, code) = await _api.PostAsync<FuncionarioResponse, FuncionarioRequest>("api/v1/Funcionarios", request);
            if (code == HttpStatusCode.Created || code == HttpStatusCode.OK)
            {
                return objeto;
            }
            // Para qualquer outro status, retorna null (a notificação já foi mostrada)
            return null;
        }
    }
}
