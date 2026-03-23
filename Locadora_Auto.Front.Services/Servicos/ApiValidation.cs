using Locadora_Auto.Front.Models.Error;
using Locadora_Auto.Front.Services.Models;
using Locadora_Auto.Front.Services.Utils.Notificacao;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Locadora_Auto.Front.Services.Servicos
{
    public interface IApiValidation
    {
    }
    public class ApiValidation : IApiValidation
    {
        private INotificationService? _notificationService;

        public ApiValidation(INotificationService? notificationService)
        {
            _notificationService = notificationService;
        }

        // Tratamento de erros especializado para ValidationProblemDetails

    }
}
