namespace {{RootNamespace}}.Application.Models.Dto
{
    /// <summary>
    /// Parâmetros que toda listagem paginada recebe. No controller entra como
    /// <c>[FromQuery] ConsultaPaginadaRequest consulta</c> — os nomes na query string continuam
    /// os mesmos (<c>pagina</c>, <c>itensPorPagina</c>, <c>termo</c>, <c>ordenarPor</c>,
    /// <c>direcao</c>), então o contrato HTTP não muda.
    ///
    /// Os limites ficam aqui, no set, e não espalhados por cada serviço: página zero ou negativa
    /// quebraria o <c>Skip</c>, e <c>itensPorPagina=100000</c> viraria uma varredura de tabela
    /// disparada da barra de endereço.
    /// </summary>
    public class ConsultaPaginadaRequest
    {
        public const int MaximoItensPorPagina = 200;

        private int _pagina = 1;
        private int _itensPorPagina = 10;

        public int Pagina
        {
            get => _pagina;
            set => _pagina = value < 1 ? 1 : value;
        }

        public int ItensPorPagina
        {
            get => _itensPorPagina;
            set => _itensPorPagina = Math.Clamp(value, 1, MaximoItensPorPagina);
        }

        /// <summary>Busca textual livre. O que cada listagem procura com ele é decisão do serviço.</summary>
        public string? Termo { get; set; }

        /// <summary>Coluna clicada na tela. Nome desconhecido cai na ordenação padrão da listagem.</summary>
        public string? OrdenarPor { get; set; }

        /// <summary>"asc" ou "desc". Ausente, vale o padrão da coluna.</summary>
        public string? Direcao { get; set; }

        /// <summary>
        /// Termo pronto para comparação: sem espaço nas pontas, em minúsculas e nulo quando vazio.
        /// No Postgres o LIKE é sensível a maiúsculas, então a busca compara em minúsculas dos dois
        /// lados — normalizar aqui evita repetir (e esquecer) isso em cada serviço.
        /// </summary>
        public string? TermoNormalizado =>
            string.IsNullOrWhiteSpace(Termo) ? null : Termo.Trim().ToLower();
    }
}
