using Locadora_Auto.Domain.Auditoria;
using Locadora_Auto.Domain.UtilExtensions;
using Microsoft.AspNetCore.Identity;

namespace Locadora_Auto.Domain.Entidades.Indentity
{
   
    public class User : IdentityUser, ITemporalEntity<UserHistorico>
    {
        public string? NomeCompleto { get; private set; }
        public string? Cpf { get; private set; }
        public bool Ativo { get; private set; }
        public DateTime DataCriacao { get; private set; }

        //navegação
        public Clientes? Cliente { get; set; }
        public Funcionario? Funcionario { get; set; }


        public static User Criar(string nome, string cpf, string phoneNumber, string email)
        {
            if(DocumentoExtensionMethods.EhCpfValido(cpf) == false)
                throw new InvalidOperationException("CPF inválido");

            if (string.IsNullOrWhiteSpace(nome))
                throw new InvalidOperationException("nome é obrigatório");;

            if (string.IsNullOrWhiteSpace(cpf)) 
                throw new InvalidOperationException("cpf é obrigatório");

            if (string.IsNullOrWhiteSpace(phoneNumber))
                throw new InvalidOperationException("telefone é obrigatório");

            if (string.IsNullOrWhiteSpace(email))
                throw new InvalidOperationException("email é obrigatório");

            cpf = LimparCpf(cpf);
            return new User
            {
                UserName = cpf,
                NomeCompleto = nome,
                Cpf = cpf,
                PhoneNumber = LimparTelefone(phoneNumber),
                Email = email,
                Ativo = true,
                DataCriacao = DateTime.Now,
                NormalizedUserName = cpf,
            };
            
        }

        public void Atualizar(string nome, string phoneNumber, string email)
        {

            if (string.IsNullOrWhiteSpace(nome))
                throw new InvalidOperationException("nome é obrigatório");

            if (string.IsNullOrWhiteSpace(phoneNumber))
                throw new InvalidOperationException("telefone é obrigatório");

            if (string.IsNullOrWhiteSpace(email))
                throw new InvalidOperationException("email é obrigatório");

            NomeCompleto = nome.Trim().ToUpper();
            PhoneNumber = LimparTelefone(phoneNumber);
            Email = email.Trim().ToLower();
            Ativo = true;   
        }

        public void Ativar()
        {
            Ativo = true;
        }
        public void Desativar()
        {
            Ativo = false;
        }

        private static string LimparCpf(string cpf)
        {
            return new string(cpf.Where(char.IsDigit).ToArray());
        }

        private static string LimparTelefone(string telefone)
        {
            return new string(telefone.Where(char.IsDigit).ToArray());
        }


    }
    

}
