using Locadora_Auto.Domain.Entidades;
using Locadora_Auto.Infra.Data;
using Locadora_Auto.Infra.Data.CurrentUsers;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Locadora_Auto.Tests.Infra
{
    /// <summary>
    /// A varredura da RN-45 é o primeiro lugar do sistema que filtra a trilha por uma lista de ids
    /// somada a um enum. É exatamente o que o <c>RepositorioFake</c> não prova: lá a
    /// <c>Expression</c> roda em memória sobre <c>List&lt;T&gt;</c> e qualquer coisa "funciona",
    /// inclusive o que o provider do Npgsql recusaria a traduzir.
    ///
    /// O EF gera o SQL sem abrir conexão, então dá para verificar a tradução aqui. Uma consulta
    /// intraduzível estoura em <c>ToQueryString</c> — a chamada é a asserção. Se um dia ela virar
    /// avaliação no cliente, a varredura passaria a arrastar a trilha inteira para a memória a cada
    /// cinco minutos, e nenhum teste de serviço notaria.
    /// </summary>
    public class VarreduraDaPreparacaoTests
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
        public void Filtro_de_quem_esta_no_patio_traduz_para_sql()
        {
            using var contexto = MontarContexto();

            var sql = contexto.Set<Veiculo>()
                .Where(v => v.Status == StatusVeiculo.EmPreparacao)
                .ToQueryString();

            Assert.Contains("tb_veiculo", sql);
            Assert.Contains("status", sql);
        }

        [Fact]
        public void Filtro_das_entradas_no_patio_traduz_para_sql()
        {
            using var contexto = MontarContexto();
            var ids = new List<int> { 1, 2, 3 };

            // é o filtro da varredura: a trilha dos veículos do lote, só as entradas em preparação
            var sql = contexto.Set<MovimentoVeiculo>()
                .Where(m => ids.Contains(m.IdVeiculo)
                            && m.StatusDestino == StatusVeiculo.EmPreparacao)
                .ToQueryString();

            Assert.Contains("id_veiculo", sql);
            Assert.Contains("status_destino", sql);
        }

        [Fact]
        public void Status_do_veiculo_chega_ao_banco_como_inteiro()
        {
            // tb_veiculo.status é int, ao contrário de tb_locacao.status, que LocacaoConfig converte
            // para texto. A varredura compara enum com enum e depende dessa conversão continuar int
            using var contexto = MontarContexto();

            var propriedade = contexto.Model
                .FindEntityType(typeof(MovimentoVeiculo))!
                .FindProperty(nameof(MovimentoVeiculo.StatusDestino))!;

            Assert.Equal(typeof(int), propriedade.GetProviderClrType());
            Assert.Equal(5, (int)StatusVeiculo.EmPreparacao);
        }
    }
}
