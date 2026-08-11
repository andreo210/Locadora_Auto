using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Locadora_Auto.Front.Services.Utils.Notificacao
{
    public interface IConfirmDialogService
    {
        /// <summary>
        /// Evento disparado quando um diálogo de confirmação deve ser mostrado
        /// </summary>
        event Func<string, string, Task>? OnShow;

        /// <summary>
        /// Exibe um diálogo de confirmação e aguarda a resposta do usuário
        /// </summary>
        /// <param name="message">Mensagem a ser exibida</param>
        /// <param name="title">Título do diálogo</param>
        /// <returns>True se o usuário confirmou, False se cancelou</returns>
        Task<bool> ConfirmAsync(string message, string title = "Confirmação");

        /// <summary>
        /// Define o resultado da confirmação (chamado pelo componente ConfirmDialog)
        /// </summary>
        /// <param name="result">True para confirmado, False para cancelado</param>
        void SetResult(bool result);
    }

    public class ConfirmDialogService : IConfirmDialogService
    {
        private TaskCompletionSource<bool>? _tcs;

        public event Func<string, string, Task>? OnShow;

        /// <summary>
        /// Exibe o diálogo de confirmação e aguarda resposta
        /// </summary>
        public async Task<bool> ConfirmAsync(string message, string title = "Confirmação")
        {
            // Cria um novo TaskCompletionSource para aguardar a resposta
            _tcs = new TaskCompletionSource<bool>();

            // Dispara o evento para mostrar o diálogo
            if (OnShow != null)
            {
                await OnShow.Invoke(message, title);
            }

            // Aguarda o usuário responder (Confirm ou Cancel)
            return await _tcs.Task;
        }

        /// <summary>
        /// Define o resultado da confirmação (chamado pelo componente ConfirmDialog)
        /// </summary>
        public void SetResult(bool result)
        {
            if (_tcs != null && !_tcs.Task.IsCompleted)
            {
                _tcs.SetResult(result);
                _tcs = null;
            }
        }

        /// <summary>
        /// Cancela o diálogo atual sem resultado
        /// </summary>
        public void CancelCurrent()
        {
            if (_tcs != null && !_tcs.Task.IsCompleted)
            {
                _tcs.SetResult(false);
                _tcs = null;
            }
        }
    }
}
