using Locadora_Auto.Api.Extensions;
using Locadora_Auto.Api.Middleware;
using Locadora_Auto.Application.Extensions;
using Locadora_Auto.Infra.Data;
using Locadora_Auto.Infra.Extensions;
using Microsoft.AspNetCore.Mvc.ApiExplorer;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var config = builder.Configuration;
var env = builder.Environment;




/***********************************/
/*        SERVICES                 */
/***********************************/


// serviço de configurações de login.
//services.AddApplicationAuthentication(config, env);

//injetar serviços do apps
services.AddInjecaoDependenciaApplicationsConfig();

//injetar serviço do Infra.service
services.AddHttpServices(config);

//injeção de dependencia do Infra
services.AddMySqlDbContext<LocadoraDbContext>(config["ConnectionStrings:dbModelo"] ?? "");
services.AddSqlServerRepositories();

//adiciona o controle de identidade
services.AddIdentityConfiguration();


//elmah core
//services.AddElmahConfig(config);

//health check
//services.AddHealthChecksConfig(builder.Configuration);

//serviços de configurações do apiConfig(configuraçoes basicas)
services.AddApiConfig();

//serviços de configurações do versionamento de api
services.AddVersionamentoConfig();

//serviço de configurações do hangfire
//services.AddHangFireConfig(config);

//configurações do swagger
builder.Services.AddSwaggerConfig();

/**************************************/
/*        APPLICATION                 */
/**************************************/



//usar os serviços 
var app = builder.Build();

//usar configuração do swagger
var apiVersionDescriptionProvider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
app.UseSwaggerConfig(apiVersionDescriptionProvider);


// Configura o serviço com base no appsettings

//usar configuração do health check
//app.UseHealthChecksConfig();

//usar configurações do apiConfig(configuraçoes basicas)
app.UseApiConfig();

//configuração Hangfire
//app.UseHangFireConfig();



//configuração de login
app.UseAuthenticationConfig();

//UseCorsConfig();

//elmah core
//app.UseElmahConfig();

//captura de erros
app.UseMiddleware<ExceptionMiddleware>();

app.MapControllers();


app.Run();
