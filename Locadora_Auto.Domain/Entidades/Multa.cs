using static Locadora_Auto.Domain.Entidades.Caucao;

namespace Locadora_Auto.Domain.Entidades
{
    public class Multa
    {
        public int IdMulta { get; private set; }
        public TipoMulta Tipo { get; private set; }
        public decimal Valor { get; private set; }
        public StatusMulta Status { get; private set; }

        protected Multa() { } // EF

        internal static Multa Criar(decimal valor, TipoMulta tipo)
        {
            if (valor <= 0)
                throw new DomainException("Valor da caução deve ser maior que zero");
            return new Multa
            {
                Valor = valor,
                Status = StatusMulta.Pendente,
                Tipo = tipo
            };

        }

        internal void MarcarComoPaga()
        {
            if (Status != StatusMulta.Pendente)
                throw new DomainException("Somente multas pendentes podem ser pagas");

            Status = StatusMulta.Paga;
        }

        internal void CompensarComCaucao()
        {
            if (Status != StatusMulta.Pendente)
                throw new DomainException("Multa não pode ser compensada");

            Status = StatusMulta.CompensadaCaucao;
        }

        internal void Cancelar()
        {
            if (Status == StatusMulta.Paga)
                throw new DomainException("Multa paga não pode ser cancelada");

            Status = StatusMulta.Cancelada;
        }
    }
    public enum TipoMulta
    {
        Atraso,
        DanoVeiculo,
        MultaTransito,
        Limpeza,
        Outros
    }

    public enum StatusMulta
    {
        Pendente,
        Paga,
        CompensadaCaucao,
        Cancelada
    }

}
