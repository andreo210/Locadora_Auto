namespace Locadora_Auto.Domain.Entidades
{
    public class Vistoria
    {
        public int IdVistoria { get; private set; }
        public int IdLocacao { get; private set; }
        public TipoVistoria Tipo { get; private set; }
        public NivelCombustivel Combustivel { get; private set; }
        public string? Observacoes { get; private set; }
        public DateTime DataVistoria { get; private set; }
        public int IdFuncionario { get; private set; }
        public int KmVeiculo { get; private set; }
        public Locacao Locacao { get; private set; } = null!;        
        public Funcionario Funcionario { get; private set; } = null!;

        private readonly List<FotoVistoria> _fotos = new();
        public IReadOnlyCollection<FotoVistoria> Fotos => _fotos;

        private readonly List<Dano> _danos = new();
        public IReadOnlyCollection<Dano> Danos => _danos;

        protected Vistoria() { } // EF

        internal static Vistoria Criar(
            int idLocacao,
            int idFuncionario,
            TipoVistoria tipo,
            NivelCombustivel Combustivel,
            int kmVeiculo,
            string? observacoes = null)
        {

            if (!Enum.IsDefined(typeof(TipoVistoria), tipo))
                throw new DomainException("Tipo de vistoria inválido");

            if (!Enum.IsDefined(typeof(NivelCombustivel), Combustivel))
                throw new DomainException("Tipo de vistoria inválido");

            var vistoria = new Vistoria
            {
                IdLocacao = idLocacao,
                IdFuncionario = idFuncionario,
                Tipo = tipo,
                KmVeiculo = kmVeiculo,
                Combustivel = Combustivel,                
                Observacoes = observacoes,
                DataVistoria = DateTime.UtcNow               
            };           
            return vistoria;
        }

        public void AdicionarDano(Dano dano)
        {
            if (dano == null)
                throw new DomainException("Dano inválido");

            _danos.Add(dano);
        }

        public void AdicionarFoto(FotoVistoria foto)
        {
            if (foto == null)
                throw new DomainException("Foto inválida");

            _fotos.Add(FotoVistoria.Criar(/*foto.IdVistoria.Value,*/ foto.NomeArquivo, foto.Raiz, foto.Diretorio, foto.Extensao, foto.QuantidadeBytes.Value));
        }

        public void AtualizarObservacoes(string observacoes)
        {
            Observacoes = observacoes;
        }
    }
    public enum TipoVistoria
    {
        Retirada = 1,
        Devolucao = 2,
        Avaria = 3
    }

    public enum NivelCombustivel
    {
        Vazio = 1,
        UmQuarto = 2,
        Meio = 3,
        TresQuartos = 4,
        Cheio = 5
    }

}
