using FluentValidation;
using Locadora_Auto.Front.Models.Request.Seguro;

namespace Locadora_Auto.Front.Models.Validadores
{
    public class SeguroValidator : AbstractValidator<CriarSeguroRequest>
    {
        public SeguroValidator()
        {
            RuleFor(x => x.Nome)
                .NotEmpty().WithMessage("O nome do seguro é obrigatório")
                .MinimumLength(3).WithMessage("O nome deve ter no mínimo 3 caracteres")
                .MaximumLength(45).WithMessage("O nome deve ter no máximo 45 caracteres");

            RuleFor(x => x.Descricao)
                .NotEmpty().WithMessage("A descrição é obrigatória")
                .MaximumLength(100).WithMessage("A descrição deve ter no máximo 100 caracteres");

            RuleFor(x => x.ValorDiaria)
                .GreaterThan(0).WithMessage("O valor da diária deve ser maior que zero");

            RuleFor(x => x.Franquia)
                .GreaterThanOrEqualTo(0).WithMessage("A franquia não pode ser negativa");

            RuleFor(x => x.Cobertura)
                .NotEmpty().WithMessage("A cobertura é obrigatória")
                .MaximumLength(200).WithMessage("A cobertura deve ter no máximo 200 caracteres");
        }
    }

    public class EditarSeguroValidator : AbstractValidator<EditarSeguroRequest>
    {
        public EditarSeguroValidator()
        {
            RuleFor(x => x.Nome)
                .NotEmpty().WithMessage("O nome do seguro é obrigatório")
                .MinimumLength(3).WithMessage("O nome deve ter no mínimo 3 caracteres")
                .MaximumLength(45).WithMessage("O nome deve ter no máximo 45 caracteres");

            RuleFor(x => x.Descricao)
                .NotEmpty().WithMessage("A descrição é obrigatória")
                .MaximumLength(100).WithMessage("A descrição deve ter no máximo 100 caracteres");

            RuleFor(x => x.ValorDiaria)
                .GreaterThan(0).WithMessage("O valor da diária deve ser maior que zero");

            RuleFor(x => x.Franquia)
                .GreaterThanOrEqualTo(0).WithMessage("A franquia não pode ser negativa");

            RuleFor(x => x.Cobertura)
                .NotEmpty().WithMessage("A cobertura é obrigatória")
                .MaximumLength(200).WithMessage("A cobertura deve ter no máximo 200 caracteres");
        }
    }
}
