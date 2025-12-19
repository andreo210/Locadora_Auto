using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;

namespace Locadora_Auto.Api.Configurations
{
    public class ClaimsTransformer : IClaimsTransformation
    {
        private string ValidAudience;
        public ClaimsTransformer() { }
        public ClaimsTransformer(string validAudience)
        {
            ValidAudience = validAudience;
        }
        public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
        {
            ClaimsIdentity claimsIdentity = (ClaimsIdentity)principal.Identity;
            // flatten resource_access because Microsoft identity model doesn't support nested claims
            // by map it to Microsoft identity model, because automatic JWT bearer token mapping already processed here
            if (claimsIdentity.IsAuthenticated && claimsIdentity.HasClaim((claim) => claim.Type == "resource_access"))
            {
                var userRole = claimsIdentity.FindFirst((claim) => claim.Type == "resource_access");
                var content = Newtonsoft.Json.Linq.JObject.Parse(userRole.Value);
                var x = content[ValidAudience]?["roles"];
                if (x != null)
                {
                    foreach (var role in content[ValidAudience]["roles"])
                    {
                        claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, role.ToString()));
                    }
                }
            }
            return Task.FromResult(principal);
        }
    }
}
