using {{RootNamespace}}.Application.Configuration.Ultils.NotificadorServices;
using Xunit;

namespace {{RootNamespace}}.Tests.Assercoes
{
    /// <summary>
    /// Açúcar sobre o notificador. Faz o mesmo que <c>Assert.True(notificador.TemNotificacao())</c>,
    /// só que a falha diz o que aconteceu: sem isso, um teste que esperava recusa e recebeu sucesso
    /// reporta apenas "Expected: True, Actual: False" e obriga a abrir o serviço para descobrir o
    /// motivo. Aqui a mensagem já traz as notificações que o serviço reportou.
    ///
    /// Opcional: asserção direta com <c>Assert</c> continua correta. Use um estilo só por projeto.
    /// </summary>
    public static class AssercoesDeNotificacao
    {
        /// <summary>
        /// Falha se o serviço não reportou nada. Com <paramref name="trecho"/>, exige que alguma
        /// mensagem o contenha — sempre um trecho, nunca a frase inteira, para reescrever o texto
        /// da notificação não quebrar o teste.
        /// </summary>
        public static void DeveNotificar(this INotificadorService notificador, string? trecho = null)
        {
            var mensagens = notificador.ObterNotificacoes().Select(n => n.Mensagem).ToList();

            Assert.True(
                mensagens.Count > 0,
                "Esperava notificação de regra de negócio, mas o serviço não reportou nenhuma.");

            if (trecho is null) return;

            Assert.True(
                mensagens.Any(m => m.Contains(trecho, StringComparison.OrdinalIgnoreCase)),
                $"Nenhuma notificação contém \"{trecho}\". Recebidas: {Listar(mensagens)}");
        }

        /// <summary>
        /// Caminho feliz. Vem antes das outras asserções: se o serviço notificou, o resto do teste
        /// falharia por consequência e a mensagem apontaria para o sintoma, não para a causa.
        /// </summary>
        public static void NaoDeveNotificar(this INotificadorService notificador)
        {
            var mensagens = notificador.ObterNotificacoes().Select(n => n.Mensagem).ToList();

            Assert.True(
                mensagens.Count == 0,
                $"Esperava sucesso, mas o serviço notificou: {Listar(mensagens)}");
        }

        /// <summary>
        /// Quantas regras o serviço acumulou. Existe porque o notificador reporta todas de uma vez:
        /// é assim que se prova que um DTO com dois campos errados devolve os dois erros, e não só
        /// o primeiro.
        /// </summary>
        public static void DeveTerNotificacoes(this INotificadorService notificador, int quantidade)
        {
            var mensagens = notificador.ObterNotificacoes().Select(n => n.Mensagem).ToList();

            Assert.True(
                mensagens.Count == quantidade,
                $"Esperava {quantidade} notificação(ões), veio(vieram) {mensagens.Count}: {Listar(mensagens)}");
        }

        private static string Listar(IEnumerable<string> mensagens) => string.Join(" | ", mensagens);
    }
}
