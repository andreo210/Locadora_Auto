using Locadora_Auto.Application.Models.Dto;
using Locadora_Auto.Application.Services.OAuth.Token;
using Locadora_Auto.Application.Services.OAuth.Users;
using Microsoft.AspNetCore.Mvc;

namespace Locadora_Auto.Api.V1.Controllers
{
    [Route("api/identidade")]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ITokenService _tokenService;
        private readonly IUserService _userService;
        //private readonly SignInManager<UsuarioEntity> _signInManager;
        //private readonly UserManager<UsuarioEntity> _userManager;
        //private readonly AppSettings _appSettings;
        //private readonly IUsuarioRepository _usuarioRepository;
        //private readonly ITokenRepository _tokenRepository;
        //private readonly IMessageBus _bus;


        public AuthController(
            //SignInManager<UsuarioEntity> signInManager,
            //UserManager<UsuarioEntity> userManager,
            //IOptions<AppSettings> appSettings,
            ITokenService tokenService,
            IUserService userService
            
            //IUsuarioRepository usuarioRepository,
            //ITokenRepository tokenRepository,
            //IMessageBus bus            
            )
        {
            //_signInManager = signInManager;
            //_userManager = userManager;
            //_appSettings = appSettings.Value;
            _tokenService = tokenService;
            _userService = userService;
            //_usuarioRepository = usuarioRepository;
            // _tokenRepository = tokenRepository;
            //_bus = bus;            
        }






       



        


    }
}