using FluentValidation;
using Locadora_Auto.Front.Models.Request.Categoria;
using Locadora_Auto.Front.Models.Request.Filial;

namespace Locadora_Auto.Front.Models.Validadores
{
    public class FilialValidator : AbstractValidator<CriarFilialRequest>
    {
        public FilialValidator()
        {
            RuleFor(x => x.Nome)
                .NotEmpty().WithMessage("O nome da categoria é obrigatório")
                .MinimumLength(3).WithMessage("O nome deve ter no mínimo 3 caracteres")
                .MaximumLength(100).WithMessage("O nome deve ter no máximo 100 caracteres");
        }
    }
}