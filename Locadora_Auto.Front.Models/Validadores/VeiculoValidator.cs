using FluentValidation;
using Locadora_Auto.Front.Models.Request.Veiculo;

namespace Locadora_Auto.Front.Models.Validadores
{
    public class VeiculoValidator : AbstractValidator<CriarVeiculoRequest>
    {
        public VeiculoValidator()
        {
            RuleFor(x => x.Placa)
                .NotEmpty().WithMessage("A placa é obrigatória")
                .MaximumLength(10).WithMessage("A placa deve ter no máximo 10 caracteres");

            RuleFor(x => x.Marca)
                .NotEmpty().WithMessage("A marca é obrigatória")
                .MaximumLength(60).WithMessage("A marca deve ter no máximo 60 caracteres");

            RuleFor(x => x.Modelo)
                .NotEmpty().WithMessage("O modelo é obrigatório")
                .MaximumLength(60).WithMessage("O modelo deve ter no máximo 60 caracteres");

            RuleFor(x => x.Ano)
                .GreaterThan(1900).WithMessage("Informe um ano válido")
                .LessThanOrEqualTo(DateTime.UtcNow.Year + 1).WithMessage("O ano não pode ser maior que o próximo ano");

            RuleFor(x => x.Chassi)
                .NotEmpty().WithMessage("O chassi é obrigatório")
                .MaximumLength(30).WithMessage("O chassi deve ter no máximo 30 caracteres");

            RuleFor(x => x.KmInicial)
                .GreaterThanOrEqualTo(0).WithMessage("A quilometragem não pode ser negativa");

            RuleFor(x => x.IdCategoria)
                .GreaterThan(0).WithMessage("Selecione a categoria do veículo");

            RuleFor(x => x.IdFilialAtual)
                .GreaterThan(0).WithMessage("Selecione a filial do veículo");
        }
    }

    public class EditarVeiculoValidator : AbstractValidator<EditarVeiculoRequest>
    {
        public EditarVeiculoValidator()
        {
            RuleFor(x => x.Marca)
                .NotEmpty().WithMessage("A marca é obrigatória")
                .MaximumLength(60).WithMessage("A marca deve ter no máximo 60 caracteres");

            RuleFor(x => x.Modelo)
                .NotEmpty().WithMessage("O modelo é obrigatório")
                .MaximumLength(60).WithMessage("O modelo deve ter no máximo 60 caracteres");

            RuleFor(x => x.Ano)
                .NotNull().WithMessage("O ano é obrigatório")
                .GreaterThan(1900).WithMessage("Informe um ano válido")
                .LessThanOrEqualTo(DateTime.UtcNow.Year + 1).WithMessage("O ano não pode ser maior que o próximo ano");

            RuleFor(x => x.KmAtual)
                .NotNull().WithMessage("A quilometragem é obrigatória")
                .GreaterThanOrEqualTo(0).WithMessage("A quilometragem não pode ser negativa");

            RuleFor(x => x.IdFilialAtual)
                .NotNull().WithMessage("Selecione a filial do veículo")
                .GreaterThan(0).WithMessage("Selecione a filial do veículo");
        }
    }

    public class IniciarManutencaoValidator : AbstractValidator<IniciarManutencaoRequest>
    {
        public IniciarManutencaoValidator()
        {
            RuleFor(x => x.IdTipoManutencao)
                .GreaterThan(0).WithMessage("Selecione o tipo de manutenção");

            RuleFor(x => x.Descricao)
                .NotEmpty().WithMessage("A descrição é obrigatória")
                .MaximumLength(500).WithMessage("A descrição deve ter no máximo 500 caracteres");
        }
    }

    public class TerminarManutencaoValidator : AbstractValidator<TerminarManutencaoRequest>
    {
        public TerminarManutencaoValidator()
        {
            RuleFor(x => x.Custo)
                .GreaterThanOrEqualTo(0).WithMessage("O custo não pode ser negativo");
        }
    }
}
