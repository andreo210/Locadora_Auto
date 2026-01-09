using Locadora_Auto.Application.Services.Email;
using Locadora_Auto.Domain.Entidades;
using Locadora_Auto.Domain.IRepositorio;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace Locadora_Auto.Application.Jobs;

#region Records (DTOs imutáveis)

/*
 * Representa os dados necessários para envio de e-mail
 * relacionados a uma solicitação.
 * 
 * Uso de record:
 * - Imutabilidade
 * - Semântica de valor
 * - Ideal para transporte de dados
 */
public record MensagemEmailSolicitacao(
    int TipoAndamento,
    string DescricaoTipoAndamento,
    string NomePessoa,
    string Email,
    string Protocolo,
    string Observacao);

/*
 * Representa uma mensagem completa a ser processada
 * pelo serviço de background.
 * 
 * Contém:
 * - Identificador da solicitação
 * - Dados de e-mail
 * - Dados de notificação
 */
public record Mensagem(
    int IdSolicitacao,
    MensagemEmailSolicitacao Email,
    Notificacao Notificacao);

/*
 * Representa os dados necessários para envio de notificação
 * por serviço externo (ex: push, SMS, API de notificação).
 */
public record Notificacao(
    string Cpf,
    string Protocolo,
    string Mensagem,
    string Status);

#endregion

#region Fila de Mensagens

/*
 * Abstração da fila de mensagens.
 * 
 * Permite:
 * - Enfileirar mensagens
 * - Consumir mensagens individualmente
 * - Consumir mensagens em lote
 * - Inspecionar fila (Peek)
 */
public interface IMessageQueue
{
    IReadOnlyCollection<Mensagem> PeekAll();
    void Enqueue(Mensagem message);
    bool TryDequeue(out Mensagem message);
    int TryDequeueBatch(ICollection<Mensagem> buffer, int maxCount);
}

/*
 * Implementação concreta da fila de mensagens.
 * 
 * Lifetime: Singleton
 * 
 * Observações importantes:
 * - Fila em memória (in-process)
 * - Mensagens são perdidas em caso de restart/crash
 * - Não suporta escala horizontal
 * 
 * Adequado apenas para cenários onde
 * perda de mensagem não é crítica.
 */
public class MessageQueue : IMessageQueue
{
    // Estrutura thread-safe para múltiplos produtores/consumidores
    private readonly ConcurrentQueue<Mensagem> _mensagens = new();

    // Permite inspeção do estado atual da fila (somente leitura)
    public IReadOnlyCollection<Mensagem> PeekAll() => _mensagens;

    /*
     * Enfileira uma nova mensagem.
     * Validação defensiva para evitar null.
     */
    public void Enqueue(Mensagem message)
    {
        ArgumentNullException.ThrowIfNull(message, nameof(message));
        _mensagens.Enqueue(message);
    }

#pragma warning disable CS8767
    /*
     * Remove uma mensagem da fila, se existir.
     * 
     * NotNullWhen(true):
     * - Garante para o compilador que message não será null
     *   quando o método retornar true.
     */
    public bool TryDequeue([NotNullWhen(true)] out Mensagem? message)
    {
        return _mensagens.TryDequeue(out message);
    }
#pragma warning restore CS8767

    /*
     * Remove múltiplas mensagens da fila de uma só vez.
     * 
     * Vantagens:
     * - Reduz overhead de processamento
     * - Permite processamento em batch
     */
    public int TryDequeueBatch(ICollection<Mensagem> buffer, int maxCount)
    {
        ArgumentNullException.ThrowIfNull(buffer);

        if (maxCount <= 0)
            return 0;

        int i = 0;
        while (i < maxCount && _mensagens.TryDequeue(out var msg))
        {
            buffer.Add(msg);
            i++;
        }

        return i;
    }
}

#endregion

#region Background Service

/*
 * Interface auxiliar para expor estado do serviço
 * (monitoramento / health check / diagnóstico).
 */
public interface IMessageSenderBackgroundService
{
    int CurrentlyProcessing { get; }
    bool Idle { get; }
}

/*
 * Serviço de background responsável por:
 * - Consumir mensagens da fila
 * - Enviar notificações
 * - Enviar e-mails
 * - Persistir logs
 * 
 * Implementa:
 * - BackgroundService (loop contínuo)
 * - IMessageSenderBackgroundService (status)
 */
public class MessageSenderBackgroundService(
    IMessageQueue queue,
    ILogger<MessageSenderBackgroundService> logger,
    IServiceProvider serviceProvider
) : BackgroundService, IMessageSenderBackgroundService
{
    // Tamanho máximo do lote processado por iteração
    private const int MaxBatchSize = 50;

    // Buffer reutilizável para evitar alocação excessiva
    private readonly List<Mensagem> _buffer = new(MaxBatchSize);

    // Delay quando a fila estiver vazia
    private readonly TimeSpan _idleDelay = TimeSpan.FromSeconds(1);

    // Quantidade de mensagens em processamento no momento
    public int CurrentlyProcessing { get; private set; }

    // Indica se o serviço está ocioso
    public bool Idle => CurrentlyProcessing == 0 && queue.PeekAll().Count == 0;

    /*
     * Loop principal do BackgroundService.
     * 
     * Executa enquanto a aplicação estiver ativa
     * e respeita o CancellationToken.
     */
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _buffer.Clear();

            // Tenta obter um lote de mensagens
            int dequeued = queue.TryDequeueBatch(_buffer, MaxBatchSize);

            // Se não houver mensagens, aguarda
            if (dequeued == 0)
            {
                try
                {
                    await Task.Delay(_idleDelay, stoppingToken);
                }
                catch (OperationCanceledException) { }
                continue;
            }

            CurrentlyProcessing = dequeued;

            try
            {
                await ProcessBatchAsync(_buffer);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro inesperado ao processar batch de mensagens.");
            }
            finally
            {
                CurrentlyProcessing = 0;
            }
        }
    }

    /*
     * Processa um lote de mensagens.
     * 
     * Cria um escopo de DI para resolver
     * serviços scoped com segurança.
     */
    private async Task ProcessBatchAsync(List<Mensagem> batch)
    {
        if (batch.Count == 0)
            return;

        using var scope = serviceProvider.CreateScope();

        var mailService = scope.ServiceProvider.GetRequiredService<IMailService>();
        var logMensagemRepository = scope.ServiceProvider.GetRequiredService<ILogMensagemRepository>();

        for (int i = 0; i < batch.Count; i++)
        {
            var mensagem = batch[i];
            CurrentlyProcessing = i + 1;

            try
            {
                await ProcessMessageAsync(
                    mensagem,
                    mailService,
                    logMensagemRepository);
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Erro ao processar mensagem (IdSolicitacao {IdSolicitacao}).",
                    mensagem.IdSolicitacao);
            }
        }
    }

    /*
     * Processa uma mensagem individual:
     * - Envia notificação
     * - Envia e-mail
     * - Registra log no banco
     */
    private async Task ProcessMessageAsync(
        Mensagem mensagem,
        IMailService mailService,
        ILogMensagemRepository logMensagemRepository)
    {
        int? statusRetornoNotificacao;
        int? statusRetornoEmail;
        string? erroNotificacao = null;
        string? erroEmail = null;



        #region Envio de Email

        try
        {
            await mailService.EnviarEmailSolicitacao(mensagem.Email);
            statusRetornoEmail = 1;
        }
        catch (Exception ex)
        {
            statusRetornoEmail = 0;
            erroEmail = ex.ToString();

            logger.LogError(
                ex,
                "Erro ao enviar e-mail para IdSolicitacao {IdSolicitacao}",
                mensagem.IdSolicitacao);
        }

        #endregion

        #region Montagem do Email para Log

        string? assunto = null;
        string? corpo = null;

        try
        {
            var (a, c) = mailService.MontarEmailSolicitacao(mensagem.Email);
            assunto = a;
            corpo = c;
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Falha ao montar o e-mail para log (IdSolicitacao {IdSolicitacao}).",
                mensagem.IdSolicitacao);

            erroEmail ??= ex.ToString();
        }

        #endregion

        #region Persistência do Log
        var logMensagem = new LogMensagem();
       //var logMensagem = new LogMensagem(
       //     mensagem.IdSolicitacao,
       //     mensagem.Email.TipoAndamento.ToString(),
       //     mensagem.Email.TipoAndamento,
       //     mensagem.Email.DescricaoTipoAndamento,
       //     mensagem.Email.NomePessoa,
       //     mensagem.Email.Email,
       //     mensagem.Email.Protocolo,
       //     mensagem.Email.Observacao,
       //     assunto,
       //     corpo,
       //     mensagem.Notificacao.Cpf,
       //     mensagem.Notificacao.Protocolo,
       //     mensagem.Notificacao.Mensagem,
       //     mensagem.Notificacao.Status,
       //     statusRetornoEmail,
       //     erroEmail);

        try
        {
            await logMensagemRepository.Inserir(logMensagem);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Erro ao inserir o log da mensagem (IdSolicitacao {IdSolicitacao}).",
                mensagem.IdSolicitacao);
        }

        #endregion
    }
}

#endregion

#region Collector (Unit of Work de Mensagens)

/*
 * Interface responsável por coletar mensagens
 * durante um fluxo de execução e publicá-las
 * de forma atômica no final.
 */
public interface IMessageCollector
{
    void Add(Mensagem message);
    void Commit();
}

/*
 * Implementação scoped do collector.
 * 
 * Permite:
 * - Acumular mensagens
 * - Enfileirar todas de uma vez no commit
 */
public class MessageCollector(IMessageQueue queue) : IMessageCollector
{
    private readonly List<Mensagem> _list = [];

    public void Add(Mensagem message) => _list.Add(message);

    public void Commit()
    {
        foreach (var m in _list)
        {
            queue.Enqueue(m);
        }

        _list.Clear();
    }
}

#endregion
