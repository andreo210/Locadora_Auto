using PetShop.Identidade.Api.Models.Views;
using PetShop.Identidade.Business.Models;
using System.Security.Claims;

namespace Locadora_Auto.Application.Configuration.Ultils.Token
{
    public interface ITokenService
    {
        Task<TokenView> GerarToken(string email);
    }
}
