namespace Locadora_Auto.Front.Models.Enum
{
    public enum CargoEnum
    {
        Administrador,
        Gerente,
        Operador,
        Vendedor,
        Financeiro
    }

    public static class CargoExtensions
    {
        public static string GetDisplayName(this CargoEnum cargo)
        {
            return cargo switch
            {
                CargoEnum.Administrador => "Administrador",
                CargoEnum.Gerente => "Gerente",
                CargoEnum.Operador => "Operador",
                CargoEnum.Vendedor => "Vendedor",
                CargoEnum.Financeiro => "Financeiro",
                _ => cargo.ToString()
            };
        }
    }
}
