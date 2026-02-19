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
        //public bool Finalizada { get; set; }
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

        public void RegistrarDano(string descricao,TipoDano tipo, decimal valor)
        {
            //if (Finalizada)
            //    throw new DomainException("Vistoria já finalizada");

            if (Tipo != TipoVistoria.Devolucao)
                throw new DomainException("Danos só podem ser registrados na devolução");

            var dano = Dano.Criar(IdVistoria, descricao, tipo, valor);

            _danos.Add(dano);
        }

        public void RemoverDano(int idDano)
        {
            var dano = ObterDano(idDano);
            if (dano == null)
                throw new DomainException("Dano não encontrado");

            _danos.Remove(dano);
        }
        public void AprovarDano(int idDano)
        {
            var dano = ObterDano(idDano);
            if (dano == null)
                throw new DomainException("Dano não encontrado");

            dano.Aprovar();
        }
        public bool PossuiDanos()
        {
            return _danos.Any();
        }
        public void ColocarDanoEmAnalise(int idDano)
        {
            var dano = ObterDano(idDano);
            dano.ColocarEmAnalise();
        }
        public void IsentarDano(int idDano)
        {
            var dano = ObterDano(idDano);
            dano.Isentar();
        }
        public void MarcarDanoComoPago(int idDano)
        {
            var dano = ObterDano(idDano);
            dano.MarcarComoPago();
        }
        private Dano ObterDano(int idDano)
        {
            var dano = _danos.FirstOrDefault(d => d.IdDano == idDano);

            if (dano == null)
                throw new DomainException("Dano não encontrado");

            return dano;
        }

        public void AdicionarFoto(FotoVistoria foto)
        {
            if (foto == null)
                throw new DomainException("Foto inválida");

            _fotos.Add(FotoVistoria.Criar(/*foto.IdVistoria.Value,*/ foto.NomeArquivo, foto.Raiz, foto.Diretorio, foto.Extensao, foto.QuantidadeBytes.Value));
        }

        public void RemoverFoto(int idFoto)
        {
            var foto = _fotos.FirstOrDefault(f => f.IdFoto == idFoto);
            if (foto == null)
                throw new DomainException("Foto não encontrada");

            _fotos.Remove(foto);
        }

        public void AtualizarKm(int km)
        {
            if (km <= 0)
                throw new DomainException("KM inválido");

            KmVeiculo = km;
        }

        public void AtualizarCombustivel(NivelCombustivel nivel)
        {
            Combustivel = nivel;
        }

        public void AtualizarObservacoes(string observacoes)
        {
            Observacoes = observacoes;
        }

        //public void Finalizar()
        //{
        //    if (Finalizada)
        //        throw new DomainException("Vistoria já finalizada");

        //    Finalizada = true;
        //}

        private void ValidarChecklist()
        {
            if (KmVeiculo <= 0)
                throw new DomainException("KM não informado");

            if (!_fotos.Any())
                throw new DomainException("É necessário ao menos uma foto");
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
