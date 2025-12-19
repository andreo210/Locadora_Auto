namespace Locadora_Auto.Domain.Entidades
{
    public class LogMensagem
    {
        public int Id { get; set; }
        public int IdSolicitacao { get; set; }

        public string TipoAndamento { get; set; } = string.Empty;
        public int TipoAndamentoCodigo { get; set; }
        public string? DescricaoTipoAndamento { get; set; }
        public string? NomePessoa { get; set; }
        public string? EmailDestinatario { get; set; }
        public string? ProtocoloEmail { get; set; }
        public string? ObservacaoEmail { get; set; }
        public string? AssuntoEmail { get; set; }
        public string? CorpoEmail { get; set; }

        public string? NotificacaoCpf { get; set; }
        public string? ProtocoloNotificacao { get; set; }
        public string? MensagemNotificacao { get; set; }
        public string? StatusNotificacao { get; set; }

        public int? StatusRetornoEmail { get; set; }
        public string? ErroEmail { get; set; }

        public int? StatusRetornoNotificacao { get; set; }
        public string? ErroNotificacao { get; set; }

        public DateTime DataCadastro { get; set; }

        public LogMensagem() { }
        public LogMensagem(int idSolicitacao, string tipoAndamento, int tipoAndamentoCodigo, string? descricaoTipoAndamento, string? nomePessoa, string? emailDestinatario, string? protocoloEmail, string? observacaoEmail, string? assuntoEmail, string? corpoEmail, string? notificacaoCpf, string? protocoloNotificacao, string? mensagemNotificacao, string? statusNotificacao, int? statusRetornoEmail, string? erroEmail, int? statusRetornoNotificacao, string? erroNotificacao)
        {
            IdSolicitacao = idSolicitacao;
            TipoAndamento = tipoAndamento;
            TipoAndamentoCodigo = tipoAndamentoCodigo;
            DescricaoTipoAndamento = descricaoTipoAndamento;
            NomePessoa = nomePessoa;
            EmailDestinatario = emailDestinatario;
            ProtocoloEmail = protocoloEmail;
            ObservacaoEmail = observacaoEmail;
            AssuntoEmail = assuntoEmail;
            CorpoEmail = corpoEmail;
            NotificacaoCpf = notificacaoCpf;
            ProtocoloNotificacao = protocoloNotificacao;
            MensagemNotificacao = mensagemNotificacao;
            StatusNotificacao = statusNotificacao;
            StatusRetornoEmail = statusRetornoEmail;
            ErroEmail = erroEmail;
            StatusRetornoNotificacao = statusRetornoNotificacao;
            ErroNotificacao = erroNotificacao;
        }
    }
}
