namespace Locadora_Auto.Domain.Entidades
{
    public class Dano
    {
        public int IdDano { get; private set; }
        public int IdVistoria { get; private set; }

        public string Descricao { get; private set; } = null!;
        public TipoDano Tipo { get; private set; }
        public decimal ValorEstimado { get; private set; }
        public StatusDano Status { get; private set; }
        public DateTime DataRegistro { get; private set; }

        public Vistoria Vistoria { get; private set; } = null!;

        protected Dano() { } // EF

        //private readonly List<Foto> _fotos = new();
        //public IReadOnlyCollection<Foto> Fotos => _fotos;

        internal static Dano Criar(
            int idVistoria,
            string descricao,
            TipoDano tipo,
            decimal valor)
        {
            if (idVistoria <= 0)
                throw new DomainException("Vistoria inválida");

            if (string.IsNullOrWhiteSpace(descricao))
                throw new DomainException("Descrição obrigatória");

            if (valor <= 0)
                throw new DomainException("Valor deve ser maior que zero");

            return new Dano
            {
                IdVistoria = idVistoria,
                Descricao = descricao,
                Tipo = tipo,
                ValorEstimado = valor,
                Status = StatusDano.Registrado,
                DataRegistro = DateTime.UtcNow
            };
        }
        public void AtualizarValor(decimal novoValor)
        {
            if (novoValor <= 0)
                throw new DomainException("Valor inválido");

            if (Status == StatusDano.Pago || Status == StatusDano.Isento)
                throw new DomainException("Não é possível alterar dano finalizado");

            ValorEstimado = novoValor;
        }

        public void ColocarEmAnalise()
        {
            if (Status != StatusDano.Registrado)
                throw new DomainException("Somente danos registrados podem ir para análise");

            Status = StatusDano.EmAnalise;
        }
        public void Aprovar()
        {
            if (Status != StatusDano.Registrado)
                throw new DomainException("Somente danos registrados podem ser aprovados");

            Status = StatusDano.Aprovado;
        }
        public void MarcarComoPago()
        {
            if (Status != StatusDano.Cobrado)
                throw new DomainException("Dano precisa estar cobrado");

            Status = StatusDano.Pago;
        }
        public void MarcarComoCobrado()
        {
            if (Status != StatusDano.Aprovado)
                throw new DomainException("Dano precisa estar aprovado");

            Status = StatusDano.Cobrado;
        }

        public void Isentar()
        {
            if (Status == StatusDano.Pago)
                throw new DomainException("Dano já pago");

            Status = StatusDano.Isento;
        }
        public void Cancelar()
        {
            if (Status == StatusDano.Pago)
                throw new DomainException("Não é possível cancelar dano pago");

            Status = StatusDano.Cancelado;
        }

        //public void AdicionarFoto(Foto foto)
        //{
        //    if (Status == StatusDano.Pago)
        //        throw new DomainException("Não pode alterar dano pago");

        //    _fotos.Add(foto);
        //}
    }

    public enum TipoDano
    {
        Risco = 1,
        Amassado = 2,
        Quebra = 3,
        Vidro = 4,
        Outro = 5
    }

    public enum StatusDano
    {
        Registrado = 1,
        Aprovado = 2,
        Cobrado = 3,
        Pago = 4,
        Isento = 5,
        EmAnalise = 6,
        Cancelado = 6
    }

}

