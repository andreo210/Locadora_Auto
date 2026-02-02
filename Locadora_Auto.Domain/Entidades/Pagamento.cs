namespace Locadora_Auto.Domain.Entidades
{
    public class Pagamento
    {
        public int IdPagamento { get; private set; }
        public decimal Valor { get; private set; }
        public DateTime DataPagamento { get; private set; }
        public StatusPagamento Status { get; private set; }
        public FormaPagamento FormaPagamento { get; private set; }

        protected Pagamento() { } // EF

        internal Pagamento(decimal valor, FormaPagamento formaPagamento)
        {
            if (valor <= 0)
                throw new DomainException("Valor do pagamento deve ser maior que zero");

            if (!Enum.IsDefined(typeof(FormaPagamento), formaPagamento))
                throw new DomainException("Forma de pagamento inválida");

            Valor = valor;
            FormaPagamento = formaPagamento;
            Status = StatusPagamento.Pendente;
            DataPagamento = DateTime.Now;
        }

        internal void Confirmar()
        {
            if (Status != StatusPagamento.Pendente)
                throw new DomainException("Somente pagamentos pendentes podem ser confirmados");

            Status = StatusPagamento.Pago;
            DataPagamento = DateTime.Now;
        }

        internal void Cancelar(string motivo)
        {
            if (Status == StatusPagamento.Pago)
                throw new DomainException("Pagamento já confirmado não pode ser cancelado");

            Status = StatusPagamento.Cancelado;
        }

        internal void MarcarComoFalhou()
        {
            if (Status != StatusPagamento.Pendente)
                throw new DomainException("Somente pagamentos pendentes podem falhar");

            Status = StatusPagamento.Falhou;
        }
    }
    public enum StatusPagamento
    {
        Pendente,
        Pago,
        Cancelado,
        Falhou
    }

    public enum FormaPagamento
    {
        Dinheiro = 1,
        CartaoCredito = 2,
        CartaoDebito = 3,
        Pix = 4,
        Boleto = 5
    }


}
