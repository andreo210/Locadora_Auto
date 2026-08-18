using Locadora_Auto.Domain.Entidades;
using Locadora_Auto.Infra.Data;
using Locadora_Auto.Infra.Data.CurrentUsers;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Locadora_Auto.Tests.Infra
{
    /// <summary>
    /// Os filtros novos dos indicadores de controle (seção 12) traduzidos para SQL.
    ///
    /// Mesma razão do <c>VarreduraDaPreparacaoTests</c>: no <c>RepositorioFake</c> a
    /// <c>Expression</c> roda em memória sobre <c>List&lt;T&gt;</c> e tudo "funciona", inclusive o
    /// que o provider do Npgsql recusaria. O EF gera o SQL sem abrir conexão, então a chamada a
    /// <c>ToQueryString</c> é a asserção — se a consulta virar avaliação no cliente, o indicador
    /// passaria a arrastar a tabela inteira para a memória e nenhum teste de serviço notaria.
    /// </summary>
    public class IndicadoresDeControleSqlTests
    {
        private sealed class UsuarioFake : ICurrentUser
        {
            public string? UserId => "teste";
            public bool IsAuthenticated => true;
        }

        private static LocadoraDbContext MontarContexto()
        {
            var opcoes = new DbContextOptionsBuilder<LocadoraDbContext>()
                // string de conexão nunca usada: gerar SQL não abre conexão
                .UseNpgsql("Host=localhost;Database=modelo_para_teste;Username=postgres;Password=postgres")
                .UseSnakeCaseNamingConvention()
                .Options;

            return new LocadoraDbContext(opcoes, new UsuarioFake());
        }

        [Fact]
        public void Filtro_de_bloqueios_vencidos_traduz_para_sql()
        {
            using var contexto = MontarContexto();
            var ids = new List<int> { 1, 2, 3 };
            var fim = DateTime.UtcNow;

            var sql = contexto.Set<BloqueioVeiculo>()
                .Where(b => ids.Contains(b.IdVeiculo)
                            && b.DataLiberacao == null
                            && b.DataPrevistaLiberacao < fim)
                .ToQueryString();

            Assert.Contains("tb_bloqueio_veiculo", sql);
            Assert.Contains("data_liberacao", sql);
            Assert.Contains("data_prevista_liberacao", sql);
        }

        [Fact]
        public void Filtro_das_recusas_do_periodo_traduz_para_sql()
        {
            using var contexto = MontarContexto();
            var inicio = DateTime.UtcNow.AddDays(-30);
            var fim = DateTime.UtcNow;
            int? idFilial = 3;

            var sql = contexto.Set<RecusaSobreposicao>()
                .Where(r => r.DataRecusa > inicio
                            && r.DataRecusa <= fim
                            && (idFilial == null || r.IdFilialRetirada == idFilial))
                .ToQueryString();

            Assert.Contains("tb_recusa_sobreposicao", sql);
            Assert.Contains("data_recusa", sql);
            Assert.Contains("id_filial_retirada", sql);
        }

        [Fact]
        public void Filtro_da_viagem_em_curso_traduz_para_sql()
        {
            using var contexto = MontarContexto();

            var sql = contexto.Set<TransferenciaVeiculo>()
                .Where(t => t.Status == StatusTransferencia.EmTransito)
                .ToQueryString();

            Assert.Contains("tb_transferencia_veiculo", sql);
            Assert.Contains("status_transferencia", sql);
        }

        /// <summary>
        /// Os enums novos do ativo chegam ao banco como inteiro, como o resto de
        /// <c>tb_veiculo.status</c> — e os valores são o contrato com o front, que os espelha em
        /// <c>StatusVeiculoEnum</c>.
        /// </summary>
        [Fact]
        public void Situacoes_novas_do_ativo_mantem_os_valores_do_contrato()
        {
            Assert.Equal(2, (int)StatusVeiculo.Bloqueado);
            Assert.Equal(6, (int)StatusVeiculo.EmTransferencia);
            Assert.Equal(7, (int)StatusVeiculo.Desmobilizado);

            Assert.Equal(6, (int)TipoDocumentoOrigem.Bloqueio);
            Assert.Equal(7, (int)TipoDocumentoOrigem.Transferencia);
            Assert.Equal(8, (int)TipoDocumentoOrigem.Desmobilizacao);
        }
    }
}
