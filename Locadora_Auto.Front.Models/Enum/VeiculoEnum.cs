namespace Locadora_Auto.Front.Models.Enum
{
    /// <summary>
    /// Espelha Locadora_Auto.Domain.Entidades.StatusVeiculo — os valores precisam bater com os da Api.
    /// </summary>
    public enum StatusVeiculoEnum
    {
        Disponivel = 1,
        Indisponivel = 2,
        Locado = 3,
        EmManutencao = 4
    }

    /// <summary>
    /// Espelha Locadora_Auto.Domain.Entidades.TipoManutencao.
    /// </summary>
    public enum TipoManutencaoEnum
    {
        Preventiva = 1,
        Corretiva = 2,
        Revisao = 3,
        TrocaPneu = 4,
        Funilaria = 5
    }

    /// <summary>
    /// Espelha Locadora_Auto.Domain.Entidades.StatusManutencao.
    /// </summary>
    public enum StatusManutencaoEnum
    {
        Aberta = 1,
        EmAndamento = 2,
        Finalizada = 3,
        Cancelada = 4
    }

    public static class VeiculoEnumExtensions
    {
        public static string GetDisplayName(this StatusVeiculoEnum status)
        {
            return status switch
            {
                StatusVeiculoEnum.Disponivel => "Disponível",
                StatusVeiculoEnum.Indisponivel => "Indisponível",
                StatusVeiculoEnum.Locado => "Locado",
                StatusVeiculoEnum.EmManutencao => "Em manutenção",
                _ => status.ToString()
            };
        }

        public static string GetCorBadge(this StatusVeiculoEnum status)
        {
            return status switch
            {
                StatusVeiculoEnum.Disponivel => "bg-success",
                StatusVeiculoEnum.Indisponivel => "bg-secondary",
                StatusVeiculoEnum.Locado => "bg-primary",
                StatusVeiculoEnum.EmManutencao => "bg-warning text-dark",
                _ => "bg-secondary"
            };
        }

        public static string GetDisplayName(this TipoManutencaoEnum tipo)
        {
            return tipo switch
            {
                TipoManutencaoEnum.Preventiva => "Preventiva",
                TipoManutencaoEnum.Corretiva => "Corretiva",
                TipoManutencaoEnum.Revisao => "Revisão",
                TipoManutencaoEnum.TrocaPneu => "Troca de pneu",
                TipoManutencaoEnum.Funilaria => "Funilaria",
                _ => tipo.ToString()
            };
        }

        public static string GetDisplayName(this StatusManutencaoEnum status)
        {
            return status switch
            {
                StatusManutencaoEnum.Aberta => "Aberta",
                StatusManutencaoEnum.EmAndamento => "Em andamento",
                StatusManutencaoEnum.Finalizada => "Finalizada",
                StatusManutencaoEnum.Cancelada => "Cancelada",
                _ => status.ToString()
            };
        }

        public static string GetCorBadge(this StatusManutencaoEnum status)
        {
            return status switch
            {
                StatusManutencaoEnum.Aberta => "bg-warning text-dark",
                StatusManutencaoEnum.EmAndamento => "bg-info text-dark",
                StatusManutencaoEnum.Finalizada => "bg-success",
                StatusManutencaoEnum.Cancelada => "bg-danger",
                _ => "bg-secondary"
            };
        }
    }
}
