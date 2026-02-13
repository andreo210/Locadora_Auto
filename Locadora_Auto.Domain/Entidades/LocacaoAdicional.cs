using Locadora_Auto.Domain.Entidades;

public class LocacaoAdicional
{
    public int IdLocacaoAdicional { get; private set; }
    public int IdAdicional { get; private set; }
    public int IdLocacao { get; private set; }
    public decimal ValorDiariaContratada { get; private set; }
    public decimal ValorTotal { get; private set; }
    public int Quantidade { get; private set; }
    public int Dias { get; private set; }

    //navegação
    public Adicional Adicional { get; private set; }

    protected LocacaoAdicional() { }

    public static LocacaoAdicional Criar(
        int idAdicional,
        decimal valorDiaria,
        int quantidade,
        int dias)
    {
        if (idAdicional <= 0)
            throw new DomainException("Adicional inválido");

        if (valorDiaria <= 0)
            throw new DomainException("Valor diaria inválido");


        if (quantidade <= 0)
            throw new DomainException("Quantidade inválida");

        if (dias <= 0)
            throw new DomainException("Dias inválidos");

        return new LocacaoAdicional
        {
            IdAdicional = idAdicional,
            ValorDiariaContratada = valorDiaria,
            Quantidade = quantidade,
            ValorTotal = valorDiaria * quantidade * dias,
            Dias = dias
        };
    }

    public decimal CalcularTotal()
        => ValorDiariaContratada * Quantidade * Dias;
}
