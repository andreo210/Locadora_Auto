using FluentValidation;
using Locadora_Auto.Front.Models.Request.Categoria;

namespace Locadora_Auto.Front.Models.Validadores
{
    public class CategoriaValidator : AbstractValidator<CriarCategoriaRequest>
    {
        public CategoriaValidator()
        {
            RuleFor(x => x.Nome)
                .NotEmpty().WithMessage("O nome da categoria é obrigatório")
                .MinimumLength(3).WithMessage("O nome deve ter no mínimo 3 caracteres")
                .MaximumLength(100).WithMessage("O nome deve ter no máximo 100 caracteres");

            RuleFor(x => x.ValorDiaria)
                .GreaterThan(0).WithMessage("O valor da diária deve ser maior que zero");

            RuleFor(x => x.LimiteKm)
                .GreaterThanOrEqualTo(0).WithMessage("O limite de KM não pode ser negativo");

            RuleFor(x => x.ValorKmExcedente)
                .GreaterThanOrEqualTo(0).WithMessage("O valor do KM excedente não pode ser negativo");
        }
    }
}