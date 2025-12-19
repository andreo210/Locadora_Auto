namespace Locadora_Auto.Api.Middleware
{
    /// <summary>
    /// Middleware que restringe o acesso à interface do Swagger apenas para usuários autenticados.
    /// Retorna HTTP 401 (Unauthorized) caso o usuário não esteja autenticado.
    /// </summary>
    public class SwaggerAuthorizedMiddleware
    {
        private readonly RequestDelegate _next;

        /// <summary>
        /// Inicializa o middleware com o próximo delegate do pipeline.
        /// </summary>
        /// <param name="next">Delegate que representa o próximo middleware na cadeia de execução.</param>
        public SwaggerAuthorizedMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        /// <summary>
        /// Executa a lógica do middleware, verificando se o acesso ao Swagger está autorizado.
        /// </summary>
        /// <param name="context">Contexto da requisição HTTP.</param>
        /// <returns>Uma tarefa que representa a conclusão da execução do middleware.</returns>
        public async Task Invoke(HttpContext context)
        {
            // Verifica se a requisição é para o Swagger e se o usuário está autenticado
            if (context.Request.Path.StartsWithSegments("/swagger")
                && !context.User.Identity.IsAuthenticated)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            // Continua o pipeline normalmente
            await _next.Invoke(context);
        }
    }

}
