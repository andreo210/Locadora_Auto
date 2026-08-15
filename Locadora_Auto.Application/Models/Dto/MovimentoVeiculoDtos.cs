namespace Locadora_Auto.Application.Models.Dto
{
    /// <summary>
    /// Uma transição da trilha do ativo (RN-37): de onde o carro saiu, para onde foi, com que
    /// documento, quando e por quem.
    ///
    /// Os pares id/nome seguem o que <c>ManutencaoDto</c> e <c>VeiculoDto</c> já fazem: o id é o
    /// que a tela filtra, o nome é o que ela mostra — assim renomear o enum não quebra o filtro
    /// gravado no front.
    ///
    /// Não traz quanto tempo o ativo ficou em cada situação: essa medida é a diferença para o
    /// movimento seguinte, e numa página ela seria mentira na primeira e na última linha. Quem
    /// mede permanência é o indicador, sobre a sequência inteira.
    /// </summary>
    public class MovimentoVeiculoDto
    {
        public int IdMovimentoVeiculo { get; set; }
        public int IdVeiculo { get; set; }

        /// <summary>Nulo no cadastro: antes de existir o veículo não estava em situação nenhuma.</summary>
        public int? IdStatusOrigem { get; set; }
        public string? StatusOrigem { get; set; }

        public int IdStatusDestino { get; set; }
        public string StatusDestino { get; set; } = null!;

        public int IdTipoOrigem { get; set; }
        public string TipoOrigem { get; set; } = null!;

        /// <summary>Preenchido quando <c>TipoOrigem</c> é contrato; nulo nos demais.</summary>
        public int? IdLocacaoOrigem { get; set; }

        /// <summary>Preenchido quando <c>TipoOrigem</c> é ordem de serviço; nulo nos demais.</summary>
        public int? IdManutencaoOrigem { get; set; }

        public DateTime DataMovimento { get; set; }

        /// <summary>
        /// Quem provocou a transição, do <c>IdUsuarioCriacao</c> da auditoria. É o que responde
        /// "quem liberou este carro" nos movimentos de pátio, que não têm documento.
        /// Enquanto a autenticação estiver comentada no <c>Program.cs</c>, vem "SYSTEM".
        /// </summary>
        public string? Autor { get; set; }
    }
}
