namespace {{RootNamespace}}.Front.Services.Utils.Notificacao
{
    public interface IConfirmDialogService
    {
        /// <summary>Disparado quando o diálogo deve aparecer. O <c>ConfirmDialog</c> assina.</summary>
        event Func<string, string, Task>? OnShow;

        /// <summary>Exibe a confirmação e só retorna quando o usuário responde.</summary>
        Task<bool> ConfirmAsync(string message, string title = "Confirmação");

        /// <summary>Chamado pelo componente ao clicar em Sim/Não.</summary>
        void SetResult(bool result);
    }

    /// <summary>
    /// Substitui o <c>confirm()</c> do navegador, que em Blazor Server bloqueia o
    /// circuito SignalR enquanto está aberto — nenhum evento sobe e, se algo falhar
    /// nesse meio-tempo, a aba fica surda até o reload.
    ///
    /// Aqui o diálogo é um modal Razor comum e a espera é um TaskCompletionSource:
    /// o await se comporta como o do confirm(), sem travar o circuito.
    /// </summary>
    public class ConfirmDialogService : IConfirmDialogService
    {
        private TaskCompletionSource<bool>? _resposta;

        public event Func<string, string, Task>? OnShow;

        public async Task<bool> ConfirmAsync(string message, string title = "Confirmação")
        {
            // Um diálogo aberto e outro chamado por cima: encerra o primeiro como "não",
            // senão o await anterior nunca completa e a página fica pendurada.
            CancelarAtual();

            _resposta = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            if (OnShow is null)
                return false;   // ninguém montou o ConfirmDialog: não confirme por omissão

            await OnShow.Invoke(message, title);

            return await _resposta.Task;
        }

        public void SetResult(bool result)
        {
            if (_resposta is { Task.IsCompleted: false })
            {
                _resposta.SetResult(result);
                _resposta = null;
            }
        }

        public void CancelarAtual() => SetResult(false);
    }
}
