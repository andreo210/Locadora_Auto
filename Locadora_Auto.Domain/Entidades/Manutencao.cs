namespace Locadora_Auto.Domain.Entidades
{
    public class Manutencao
    {
        public int IdManutencao { get; private set; }
        public TipoManutencao Tipo { get; private set; }
        public string? Descricao { get; private set; }
        public decimal Custo { get; private set; }
        public DateTime DataInicio { get; private set; }
        public DateTime? DataFim { get; private set; }
        public StatusManutencao Status { get; private set; }

        protected Manutencao() { } // EF

        public static Manutencao Criar(TipoManutencao tipo, string? descricao)
        {
            return new Manutencao
            {
                Tipo = tipo,
                Descricao = descricao,
                DataInicio = DateTime.UtcNow,
                Status = StatusManutencao.Aberta,
                Custo = 0
            };
        }

        public void Encerrar(decimal custo)
        {
            if (Status != StatusManutencao.Aberta)
                throw new DomainException("Somente manutenção aberta pode ser encerrada");

            if (custo < 0)
                throw new DomainException("Custo inválido");

            Custo = custo;
            DataFim = DateTime.UtcNow;
            Status = StatusManutencao.Finalizada;
        }

        public void Cancelar()
        {
            if (Status == StatusManutencao.Finalizada)
                throw new DomainException("Manutenção finalizada não pode ser cancelada");

            Status = StatusManutencao.Cancelada;
        }

        public void AtualizarDescricao(string descricao)
        {
            Descricao = descricao;
        }
    }
}

public enum TipoManutencao
{
    Preventiva = 1,
    Corretiva = 2,
    Revisao = 3,
    TrocaPneu = 4,
    Funilaria = 5
}

public enum StatusManutencao
{
    Aberta = 1,
    EmAndamento = 2,
    Finalizada = 3,
    Cancelada = 4
}

