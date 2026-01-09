using Locadora_Auto.Application.Jobs;
using Locadora_Auto.Application.Models.Dto;

namespace Locadora_Auto.Application.Handlers
{
    public class SolicitacaoService : ISolicitacaoService
    {
        private readonly IMessageCollector _messageCollector;

        public SolicitacaoService(IMessageCollector messageCollector)
        {
            _messageCollector = messageCollector;
        }

        //exemplo de colocar uma mensagem na fila
        public async Task CriarSolicitacaoAsync(SolicitacaoDto solicitacao)
        {
            // Persistência da solicitação (exemplo)
            // await _repositorio.AddAsync(solicitacao);

            // Cria mensagem de e-mail
            var email = new MensagemEmailSolicitacao(
                TipoAndamento: 1,
                DescricaoTipoAndamento: "Solicitação criada",
                NomePessoa: solicitacao.Nome,
                Email: solicitacao.Email,
                Protocolo: solicitacao.Protocolo,
                Observacao: "Solicitação criada com sucesso");

            // Cria mensagem de notificação
            var notificacao = new Notificacao(
                Cpf: solicitacao.Cpf,
                Protocolo: solicitacao.Protocolo,
                Mensagem: "Sua solicitação foi criada",
                Status: "CRIADA");

            // Adiciona à coleção
            _messageCollector.Add(new Mensagem(
                solicitacao.Id,
                email,
                notificacao));
        }
    }

}
