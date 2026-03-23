using FluentValidation;
using Locadora_Auto.Front.Models.Request.Categoria;

namespace Locadora_Auto.Front.Models.Validadores
{
  
    public class CategoriaValidator : AbstractValidator<CriarCategoriaRequest>
    {
        public CategoriaValidator()
        {
            RuleFor(x => x.Nome)
                .NotEmpty().WithMessage("Nome é obrigatório")
                .Length(3, 100).WithMessage("Nome deve ter entre 3 e 100 caracteres")
                .Matches(@"^[a-zA-ZÀ-ÿ\s]+$").WithMessage("Nome deve conter apenas letras");

        }

    }
}
