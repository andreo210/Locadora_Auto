using FluentValidation;
using Locadora_Auto.Front.Models.Request;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Locadora_Auto.Front.Models.Validadores
{
  
    public class FuncionarioValidator : AbstractValidator<FuncionarioRequest>
    {
        public FuncionarioValidator()
        {
            RuleFor(x => x.Matricula)
                .NotEmpty().WithMessage("Matrícula é obrigatória")
                .Length(3, 20).WithMessage("Matrícula deve ter entre 3 e 20 caracteres");

            RuleFor(x => x.Nome)
                .NotEmpty().WithMessage("Nome é obrigatório")
                .Length(3, 100).WithMessage("Nome deve ter entre 3 e 100 caracteres")
                .Matches(@"^[a-zA-ZÀ-ÿ\s]+$").WithMessage("Nome deve conter apenas letras");

            RuleFor(x => x.Cargo)
                .NotEmpty().WithMessage("Cargo é obrigatório");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("E-mail é obrigatório")
                .EmailAddress().WithMessage("E-mail inválido");

            RuleFor(x => x.Telefone)
                .NotEmpty().WithMessage("Telefone é obrigatório")
                .Length(10, 11).WithMessage("Telefone deve ter 10 ou 11 dígitos");

            RuleFor(x => x.Cpf)
                .NotEmpty().WithMessage("CPF é obrigatório")
                .Length(11, 11).WithMessage("CPF deve ter 11 dígitos")
                .Must(ValidarCPF).WithMessage("CPF inválido");

            RuleFor(x => x.Permissoes)
                .NotEmpty().WithMessage("Selecione pelo menos uma permissão");

            RuleFor(x => x.Senha)
                .NotEmpty().WithMessage("Senha é obrigatória")
                .MinimumLength(6).WithMessage("Senha deve ter no mínimo 6 caracteres")
                .Matches(@"[A-Z]+").WithMessage("Senha deve conter pelo menos uma letra maiúscula")
                .Matches(@"[a-z]+").WithMessage("Senha deve conter pelo menos uma letra minúscula")
                .Matches(@"[0-9]+").WithMessage("Senha deve conter pelo menos um número");

            RuleFor(x => x.ConfirmeSenha)
                .NotEmpty().WithMessage("Confirmação de senha é obrigatória")
                .Equal(x => x.Senha).WithMessage("As senhas não conferem");
        }

        private bool ValidarCPF(string cpf)
        {
            if (string.IsNullOrWhiteSpace(cpf))
                return false;

            // Remove caracteres não numéricos
            cpf = new string(cpf.Where(char.IsDigit).ToArray());

            if (cpf.Length != 11)
                return false;

            // Elimina CPFs inválidos conhecidos
            if (cpf.All(c => c == cpf[0]))
                return false;

            // Validação do primeiro dígito
            int soma = 0;
            for (int i = 0; i < 9; i++)
                soma += int.Parse(cpf[i].ToString()) * (10 - i);

            int resto = soma % 11;
            int digito1 = resto < 2 ? 0 : 11 - resto;

            if (digito1 != int.Parse(cpf[9].ToString()))
                return false;

            // Validação do segundo dígito
            soma = 0;
            for (int i = 0; i < 10; i++)
                soma += int.Parse(cpf[i].ToString()) * (11 - i);

            resto = soma % 11;
            int digito2 = resto < 2 ? 0 : 11 - resto;

            return digito2 == int.Parse(cpf[10].ToString());
        }
    }
}
