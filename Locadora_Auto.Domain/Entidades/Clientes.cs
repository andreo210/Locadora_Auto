using Locadora_Auto.Domain.Auditoria;
using Locadora_Auto.Domain.Entidades.Indentity;

namespace Locadora_Auto.Domain.Entidades
{
    public class Clientes : IAuditoria, ITemporalEntity<ClienteHistorico>
    {
        public int IdCliente { get; private set; }        
        public string? NumeroHabilitacao { get; private set; }
        public DateTime? ValidadeHabilitacao { get; private set; }
        public bool Ativo { get; private set; }
        public int TotalLocacoes { get; private set; }
        public StatusCliente Status { get; private set; }


        //auditoria
        public DateTime DataCriacao { get; set; }
        public string? IdUsuarioCriacao { get; set; }
        public DateTime? DataModificacao { get; set; }
        public string? IdUsuarioModificacao { get; set; }

        //chave estrangeira
        public string IdUser { get; set; } = null!;

        //navegação
        public User? Usuario { get; set; } = null!;
        public Endereco? Endereco { get; private set; } = null!;
        public ICollection<Locacao> Locacoes { get; set; } = new List<Locacao>();
        public ICollection<Reserva> Reservas { get; set; } = new List<Reserva>();

        private Clientes(){  }

        public static Clientes Criar(string numeroHabilitacao, DateTime validadeCnh, Endereco endereco)
        {
            if (string.IsNullOrWhiteSpace(numeroHabilitacao))
                throw new InvalidOperationException("Numero de habilitação é obrigatório");
            
            if (endereco == null)
                throw new InvalidOperationException("endereço não pode ser nulo");

            return new Clientes
            {
                NumeroHabilitacao = numeroHabilitacao,
                ValidadeHabilitacao = validadeCnh,
                Status = StatusCliente.Habilitado,
                Ativo = true,
                TotalLocacoes = 0,
                Endereco = Endereco.Criar(endereco.Logradouro, endereco.Numero, endereco.Bairro, endereco.Cidade, endereco.Estado, endereco.Cep, endereco.Complemento)
            };
        }


       

        public void Atualizar(string numeroHabilitacao, DateTime validadeCnh, Endereco endereco)
        {
            if (string.IsNullOrWhiteSpace(numeroHabilitacao))
                throw new InvalidOperationException("Numero de habilitação é obrigatório");

            if (endereco == null)
                throw new InvalidOperationException("endereço não pode ser nulo");

            NumeroHabilitacao = numeroHabilitacao;
            ValidadeHabilitacao = validadeCnh;
            Status = StatusCliente.Habilitado;
            Ativo = true;
            Endereco.Atualizar(endereco.Logradouro, endereco.Numero, endereco.Bairro, endereco.Cidade, endereco.Estado, endereco.Cep, endereco.Complemento);
        }

        //private void ClienteMaiorDeIdade(DateTime dataNascimento)
        //{
        //    var idade = DateTime.UtcNow.Year - dataNascimento.Year;

        //    // Ajustar se ainda não fez aniversário este ano
        //    if (DateTime.UtcNow < dataNascimento.AddYears(idade))
        //    {
        //        idade--;
        //    }

        //    return idade >= 18;
        //}

        public void Bloquear()
        {
            Status = StatusCliente.Bloqueado;
        }

        public void MarcarInadimplente()
        {
            Status = StatusCliente.Inadimplente;
        }

        public void Regularizar()
        {
            Status = StatusCliente.Habilitado;
        }

        public void Ativar()
        {
            Ativo = true;
        }
        public void Desativar()
        {
            Ativo = false;
        }

        public bool PodeLocar()
        {
            return Status == StatusCliente.Habilitado &&
                   ValidadeHabilitacao >= DateTime.Today;
        }


    }
    public enum StatusCliente
    {
        Habilitado,
        Inadimplente,
        Bloqueado
    }

    
}
