using FluentValidation;
using Locadora_Auto.Front.Models.Request.Reserva;

namespace Locadora_Auto.Front.Models.Validadores
{
    public class ReservaValidator : AbstractValidator<CriarReservaRequest>
    {
        public ReservaValidator()
        {
            RuleFor(x => x.IdCliente)
                .GreaterThan(0).WithMessage("Selecione o cliente da reserva");

            RuleFor(x => x.IdFilial)
                .GreaterThan(0).WithMessage("Selecione a filial de retirada");

            RuleFor(x => x.IdCategoriaVeiculo)
                .GreaterThan(0).WithMessage("Selecione a categoria de veículo");

            RuleFor(x => x.DataInicio)
                .GreaterThan(DateTime.Now).WithMessage("A data de início deve ser futura");

            RuleFor(x => x.DataFim)
                .GreaterThan(x => x.DataInicio).WithMessage("A data de entrega deve ser posterior à data de início");
        }
    }
}
