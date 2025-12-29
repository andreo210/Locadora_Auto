using Locadora_Auto.Application.Services.OAuth.Token;
using Microsoft.AspNetCore.Mvc;

namespace Locadora_Auto.Api.V1.Controllers
{
    [ApiController]
    [Route(".well-known")]
    public class JwksController : ControllerBase
    {
        private readonly RsaKeyService _keyService;

        public JwksController(RsaKeyService keyService)
        {
            _keyService = keyService;
        }

        [HttpGet("jwks.json")]
        public IActionResult Get()
        {
            return Ok(new
            {
                keys = new[] { _keyService.GetPublicJwk() }
            });
        }
    }

}
