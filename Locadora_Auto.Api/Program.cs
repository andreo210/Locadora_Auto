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


// servi�o de configura��es de login.
//services.AddApplicationAuthentication(config, env);

//injetar servi�os do apps
services.AddInjecaoDependenciaApplicationsConfig();

//injetar servi�o do Infra.service
services.AddHttpServices(config);

//inje��o de dependencia do Infra
services.AddPostgresDbContext<LocadoraDbContext>(config["ConnectionStrings:dbModelo"] ?? "");
services.AddSqlServerRepositories();

//adiciona o controle de identidade
services.AddIdentityConfiguration();


//elmah core
//services.AddElmahConfig(config);

//health check
//services.AddHealthChecksConfig(builder.Configuration);

//servi�os de configura��es do apiConfig(configura�oes basicas)
services.AddApiConfig();

//servi�os de configura��es do versionamento de api
services.AddVersionamentoConfig();

//servi�o de configura��es do hangfire
//services.AddHangFireConfig(config);

//configura��es do swagger
builder.Services.AddSwaggerConfig();

/**************************************/
/*        APPLICATION                 */
/**************************************/



//usar os servi�os 
var app = builder.Build();

//usar configura��o do swagger
var apiVersionDescriptionProvider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
app.UseSwaggerConfig(apiVersionDescriptionProvider);


// Configura o servi�o com base no appsettings

//usar configura��o do health check
//app.UseHealthChecksConfig();

//usar configura��es do apiConfig(configura�oes basicas)
app.UseApiConfig();

//configura��o Hangfire
//app.UseHangFireConfig();



//configura��o de login
app.UseAuthenticationConfig();

//UseCorsConfig();

//elmah core
//app.UseElmahConfig();

//captura de erros
app.UseMiddleware<ExceptionMiddleware>();

app.MapControllers();


app.Run();
