using Locadora_Auto.Domain.Auditoria;

namespace Locadora_Auto.Domain.Entidades
{
    /// <summary>
    /// Seção 12: uma linha por tentativa de abrir contrato sobre um veículo que já estava
    /// comprometido no período (RN-40).
    ///
    /// O indicador que ela alimenta é "tentativas de sobreposição recusadas <b>por filial</b>", e a
    /// leitura dele é sobre <b>processo</b>, não sobre sistema: a recusa em si funcionou, o balcão
    /// não vendeu carro que não existe. O que ela denuncia é o atendente escolhendo placa que já
    /// tem contrato — agenda de pátio desatualizada, treinamento, ou frota curta demais na filial.
    /// Se o número sobe numa filial, o problema é lá, e não no código.
    ///
    /// Por isso é tabela e não log: por filial, ao longo do tempo, comparável. Contagem que mora só
    /// no arquivo de log ninguém acompanha, e ela desaparece a cada restart.
    /// </summary>
    public class RecusaSobreposicao : IAuditoria
    {
        public int IdRecusaSobreposicao { get; private set; }

        public int IdVeiculo { get; private set; }

        /// <summary>
        /// Onde a tentativa foi feita. É o recorte do indicador — o balcão que erra é o que precisa
        /// aparecer, e ele não é necessariamente o da filial atual do veículo.
        /// </summary>
        public int IdFilialRetirada { get; private set; }

        public DateTime InicioSolicitado { get; private set; }
        public DateTime FimSolicitado { get; private set; }

        public DateTime DataRecusa { get; private set; }

        public OrigemRecusa Origem { get; private set; }

        /// <summary>
        /// Preenchido quando a recusa foi numa <b>extensão</b> (RN-42), e não numa abertura: é o
        /// contrato que se tentou esticar. Nulo na abertura. A distinção importa porque as duas
        /// dizem coisas diferentes ao gestor — abertura errada é escolha de placa, extensão
        /// recusada é frota curta com o cliente já na mão.
        /// </summary>
        public int? IdLocacaoEmExtensao { get; private set; }

        //auditoria: é daqui que sai quem tentou. Recusa não se altera, então o par de modificação
        //existe só porque o contrato IAuditoria pede, e permanece nulo.
        public DateTime DataCriacao { get; set; }
        public string? IdUsuarioCriacao { get; set; }
        public DateTime? DataModificacao { get; set; }
        public string? IdUsuarioModificacao { get; set; }

        private RecusaSobreposicao() { } // EF

        /// <summary>
        /// <c>Criar</c> é público, ao contrário do de <see cref="MovimentoVeiculo"/> e
        /// <see cref="BloqueioVeiculo"/>: esta não é entidade de agregado do veículo. Ela não muda
        /// estado de ativo nenhum — é registro de um fato que aconteceu no balcão, e quem o observa
        /// é o serviço que recusou.
        /// </summary>
        public static RecusaSobreposicao Criar(
            int idVeiculo,
            int idFilialRetirada,
            DateTime inicioSolicitado,
            DateTime fimSolicitado,
            OrigemRecusa origem,
            int? idLocacaoEmExtensao = null)
        {
            return new RecusaSobreposicao
            {
                IdVeiculo = idVeiculo,
                IdFilialRetirada = idFilialRetirada,
                InicioSolicitado = inicioSolicitado,
                FimSolicitado = fimSolicitado,
                Origem = origem,
                IdLocacaoEmExtensao = idLocacaoEmExtensao is > 0 ? idLocacaoEmExtensao : null,
                DataRecusa = DateTime.UtcNow
            };
        }
    }

    /// <summary>
    /// Por onde a recusa saiu. As duas contam para o indicador, mas dizem coisas diferentes sobre a
    /// operação — e é por isso que não viram um número só.
    /// </summary>
    public enum OrigemRecusa
    {
        /// <summary>
        /// A consulta do serviço encontrou o contrato sobreposto antes de gravar. É o caso normal:
        /// o atendente escolheu uma placa comprometida e o sistema avisou a tempo.
        /// </summary>
        Consulta = 1,

        /// <summary>
        /// A constraint <c>EXCLUDE</c> do banco barrou (SQLSTATE <c>23P01</c>). Isto só acontece em
        /// <b>concorrência real</b>: dois atendentes abrindo o mesmo período no mesmo instante,
        /// os dois passando pela consulta antes de qualquer um gravar.
        ///
        /// Contado à parte porque o significado é outro: aqui o processo do balcão não errou — dois
        /// pontos de venda disputaram o mesmo carro. Se este número cresce, o problema é falta de
        /// frota ou de coordenação entre canais, não treinamento.
        /// </summary>
        Banco = 2
    }
}
