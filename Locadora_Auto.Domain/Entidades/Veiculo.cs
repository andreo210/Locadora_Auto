namespace Locadora_Auto.Domain.Entidades
{
    public class Veiculo
    {
        public int IdVeiculo { get; private set; }
        public string Placa { get; private set; } = null!;
        public string Marca { get; private set; } = null!;
        public string Modelo { get; private set; } = null!;
        public int Ano { get; private set; }
        public string Chassi { get; private set; } = null!;
        public int IdCategoria { get; private set; }
        public int KmAtual { get; private set; }
        public bool Ativo { get; private set; }
        public bool Disponivel { get; private set; }
        public int FilialAtualId { get; private set; }
        public StatusVeiculo Status { get; private set; }

        //navegação
        public CategoriaVeiculo Categoria { get; set; } = null!;
        public Filial FilialAtual { get; set; } = null!;

        public ICollection<Locacao> Locacoes { get; set; } = new List<Locacao>();

        //manutenção
        private readonly List<Manutencao> _manutencoes = new();
        public IReadOnlyCollection<Manutencao> Manutencoes => _manutencoes;

        public static Veiculo Criar(string placa, string marca, string modelo, int ano, string chassi, int kmAtual,int idCategoria, int idFilialAtual)
        {
            if (string.IsNullOrWhiteSpace(placa))
                throw new InvalidOperationException("placa é obrigatório");
            if (string.IsNullOrWhiteSpace(marca))
                throw new InvalidOperationException("marca é obrigatório");
            if (string.IsNullOrWhiteSpace(modelo))
                throw new InvalidOperationException("modelo é obrigatório");
            if (!int.IsPositive(ano))
                throw new InvalidOperationException("ano tem que ser um numero positivo");
            if (string.IsNullOrWhiteSpace(chassi))
                throw new InvalidOperationException("chassi é obrigatório");
            // veículo zero km é válido, então só km negativo é recusado
            if (kmAtual < 0)
                throw new InvalidOperationException("kmAtual não pode ser negativo");
            if (!int.IsPositive(idCategoria))
                throw new InvalidOperationException("idCategoria tem que ser um numero positivo");
            if (!int.IsPositive(idFilialAtual))
                throw new InvalidOperationException("idFilialAtual tem que ser um numero positivo");

            return new Veiculo
            {
                Placa = placa.Trim().ToUpper(),
                Marca = marca.Trim().ToUpper(),
                Modelo = modelo.Trim().ToUpper(),
                Ano = ano,
                Chassi = chassi.Trim().ToUpper(),
                KmAtual = kmAtual,
                IdCategoria = idCategoria,
                FilialAtualId = idFilialAtual,
                Ativo = true,
                Disponivel = true,
                Status = StatusVeiculo.Disponivel
            };
        }

        public void Atualizar(int kmAtual,int idFilialAtual, string? marca = null, string? modelo = null, int? ano = null)
        {

            if (kmAtual < 0)
                throw new InvalidOperationException("kmAtual não pode ser negativo");
            if (!int.IsPositive(idFilialAtual))
                throw new InvalidOperationException("idFilialAtual tem que ser um numero positivo");
            KmAtual = kmAtual;
            FilialAtualId = idFilialAtual;

            // marca/modelo/ano são opcionais: só mudam quando vêm preenchidos
            if (!string.IsNullOrWhiteSpace(marca))
                Marca = marca.Trim().ToUpper();
            if (!string.IsNullOrWhiteSpace(modelo))
                Modelo = modelo.Trim().ToUpper();
            if (ano.HasValue)
            {
                if (!int.IsPositive(ano.Value))
                    throw new InvalidOperationException("ano tem que ser um numero positivo");
                Ano = ano.Value;
            }
        }

        public void Ativar()
        {
            Ativo = true;

            // só volta a ficar disponível se não estiver locado nem em manutenção
            if (Status == StatusVeiculo.Indisponivel)
            {
                Status = StatusVeiculo.Disponivel;
                Disponibilizar();
            }
        }
        public void Desativar()
        {
            Ativo = false;

            // veículo inativo nunca é ofertado para locação
            Indisponibilizar();

            if (Status == StatusVeiculo.Disponivel)
                Status = StatusVeiculo.Indisponivel;
        }

        public void Disponibilizar()
        {
            Disponivel = true;
        }
        public void Indisponibilizar()
        {
            Disponivel = false;
        }


        public void Valida(string placa, string marca, string modelo, int ano, string chassi, int kmAtual, int idCategoria, int idFilialAtual)
        {
            if (string.IsNullOrWhiteSpace(placa))
                throw new InvalidOperationException("placa é obrigatório");
            if (string.IsNullOrWhiteSpace(marca))
                throw new InvalidOperationException("marca é obrigatório");
            if (string.IsNullOrWhiteSpace(modelo))
                throw new InvalidOperationException("modelo é obrigatório");
            if (!int.IsPositive(ano))
                throw new InvalidOperationException("ano tem que ser um numero positivo");
            if (string.IsNullOrWhiteSpace(chassi))
                throw new InvalidOperationException("chassi é obrigatório");
            if (!int.IsPositive(kmAtual))
                throw new InvalidOperationException("kmAtual tem que ser um numero positivo");
            if (!int.IsPositive(idCategoria))
                throw new InvalidOperationException("idCategoria tem que ser um numero positivo");
            if (!int.IsPositive(idFilialAtual))
                throw new InvalidOperationException("idFilialAtual tem que ser um numero positivo");
        }

        #region Manutenção do veiculo
        public void IniciarManutencao(TipoManutencao tipo, string descricao)
        {
            if (Status == StatusVeiculo.Locado)
                throw new DomainException("Veículo locado não pode entrar em manutenção");

            _manutencoes.Add(Manutencao.Criar(tipo, descricao));

            Status = StatusVeiculo.EmManutencao;
            Indisponibilizar();
        }
        public void TerminaManutencao(decimal custo, int idManutencao)
        {
            if (Status == StatusVeiculo.Locado)
                throw new DomainException("Veículo locado não pode entrar em manutenção");

            var manutencao = ObterManutencao(idManutencao);
            manutencao.Encerrar(custo);
            SairDaManutencao();
        }

        public void CancelarManutencao(int idManutencao)
        {
            if (Status == StatusVeiculo.Locado)
                throw new DomainException("Veículo locado não pode entrar em manutenção");

            var manutencao = ObterManutencao(idManutencao);
            manutencao.Cancelar();
            SairDaManutencao();
        }

        public void AtualizarDescricaoManutencao(int idManutencao, string descricao)
        {
            if (Status == StatusVeiculo.Locado)
                throw new DomainException("Veículo locado não pode entrar em manutenção");

            var manutencao = ObterManutencao(idManutencao);
            // só a descrição muda: o veículo continua no status em que estava
            manutencao.AtualizarDescricao(descricao);
        }

        /// <summary>
        /// Ao sair da manutenção o veículo só volta a ser ofertado se estiver ativo.
        /// </summary>
        private void SairDaManutencao()
        {
            if (Ativo)
            {
                Status = StatusVeiculo.Disponivel;
                Disponibilizar();
            }
            else
            {
                Status = StatusVeiculo.Indisponivel;
                Indisponibilizar();
            }
        }

        private Manutencao ObterManutencao(int idManutencao)
        {
            var manutencao = _manutencoes.FirstOrDefault(x => x.IdManutencao == idManutencao);
            if (manutencao == null)
                throw new DomainException($"Manutenção {idManutencao} não encontrada para este veículo");

            return manutencao;
        }
        #endregion Manutenção do veiculo
    }

    public enum StatusVeiculo
    {
        Disponivel = 1,
        Indisponivel = 2,
        Locado = 3,
        EmManutencao = 4
    }

}
