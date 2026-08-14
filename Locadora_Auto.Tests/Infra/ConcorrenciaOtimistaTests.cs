using Locadora_Auto.Domain.Entidades;
using Locadora_Auto.Domain.Entidades.Indentity;
using Locadora_Auto.Infra.Data;
using Locadora_Auto.Infra.Data.CurrentUsers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace Locadora_Auto.Tests.Infra
{
    /// <summary>
    /// O modelo do EF é construído sem abrir conexão, então dá para verificar a configuração de
    /// concorrência sem banco nenhum. O que estes testes garantem é que o token está ligado onde
    /// deve e desligado onde não faz sentido — o comportamento em si (UPDATE com WHERE xmin)
    /// é do Postgres.
    /// </summary>
    public class ConcorrenciaOtimistaTests
    {
        private sealed class UsuarioFake : ICurrentUser
        {
            public string? UserId => "teste";
            public bool IsAuthenticated => true;
        }

        private static LocadoraDbContext MontarContexto()
        {
            var opcoes = new DbContextOptionsBuilder<LocadoraDbContext>()
                // string de conexão nunca usada: construir o modelo não abre conexão
                .UseNpgsql("Host=localhost;Database=modelo_para_teste;Username=postgres;Password=postgres")
                .UseSnakeCaseNamingConvention()
                .Options;

            return new LocadoraDbContext(opcoes, new UsuarioFake());
        }

        [Theory]
        [InlineData(typeof(Veiculo))]
        [InlineData(typeof(Reserva))]
        [InlineData(typeof(Locacao))]
        [InlineData(typeof(Clientes))]
        [InlineData(typeof(Manutencao))]
        [InlineData(typeof(Multa))]
        [InlineData(typeof(Pagamento))]
        [InlineData(typeof(Adicional))]
        [InlineData(typeof(Seguro))]
        [InlineData(typeof(Filial))]
        [InlineData(typeof(CategoriaVeiculo))]
        public void Entidades_de_dominio_usam_xmin_como_token_de_concorrencia(Type tipoEntidade)
        {
            using var contexto = MontarContexto();

            var xmin = contexto.Model.FindEntityType(tipoEntidade)?.FindProperty("xmin");

            Assert.NotNull(xmin);
            Assert.True(xmin!.IsConcurrencyToken, $"{tipoEntidade.Name} deveria ter xmin como token de concorrência");
            Assert.Equal(ValueGenerated.OnAddOrUpdate, xmin.ValueGenerated);
        }

        [Fact]
        public void Historico_temporal_fica_de_fora()
        {
            using var contexto = MontarContexto();

            // histórico só recebe insert: não há segunda gravação concorrente a proteger
            var xmin = contexto.Model.FindEntityType(typeof(ClienteHistorico))?.FindProperty("xmin");

            Assert.Null(xmin);
        }

        [Fact]
        public void Identity_fica_de_fora_porque_ja_tem_ConcurrencyStamp()
        {
            using var contexto = MontarContexto();

            var usuario = contexto.Model.FindEntityType(typeof(User));

            Assert.Null(usuario?.FindProperty("xmin"));
            Assert.NotNull(usuario?.FindProperty("ConcurrencyStamp"));
        }

        [Fact]
        public void Token_nao_vira_coluna_nova_no_banco()
        {
            using var contexto = MontarContexto();

            var xmin = contexto.Model.FindEntityType(typeof(Veiculo))!.FindProperty("xmin")!;

            // xmin é coluna de sistema já existente: o mapeamento aponta para ela, não cria outra
            Assert.Equal("xmin", xmin.GetColumnName());
            Assert.Equal("xid", xmin.GetColumnType());
        }
    }
}
