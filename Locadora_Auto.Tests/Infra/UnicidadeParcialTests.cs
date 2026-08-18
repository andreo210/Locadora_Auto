using Locadora_Auto.Domain.Entidades;
using Locadora_Auto.Infra.Data;
using Locadora_Auto.Infra.Data.CurrentUsers;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Locadora_Auto.Tests.Infra
{
    /// <summary>
    /// RN-55: a unicidade de placa e chassi é <b>parcial</b> — vale só entre veículos ativos.
    ///
    /// Isto não dá para verificar no <c>RepositorioFake</c>: lá não há índice nenhum, e um serviço
    /// que esquecesse a checagem passaria no teste de serviço e quebraria no banco. O que se fixa
    /// aqui é o predicado chegar ao modelo do EF — se alguém regerar o <c>VeiculoConfig</c> sem o
    /// <c>HasFilter</c>, a regra volta a ser global em silêncio e o carro desmobilizado segura para
    /// sempre uma placa que o Detran já reemitiu.
    /// </summary>
    public class UnicidadeParcialTests
    {
        private sealed class UsuarioFake : ICurrentUser
        {
            public string? UserId => "teste";
            public bool IsAuthenticated => true;
        }

        private static LocadoraDbContext MontarContexto()
        {
            var opcoes = new DbContextOptionsBuilder<LocadoraDbContext>()
                // string de conexão nunca usada: ler o modelo não abre conexão
                .UseNpgsql("Host=localhost;Database=modelo_para_teste;Username=postgres;Password=postgres")
                .UseSnakeCaseNamingConvention()
                .Options;

            return new LocadoraDbContext(opcoes, new UsuarioFake());
        }

        [Theory]
        [InlineData(nameof(Veiculo.Placa))]
        [InlineData(nameof(Veiculo.Chassi))]
        public void Indice_unico_do_veiculo_e_restrito_aos_ativos(string propriedade)
        {
            using var contexto = MontarContexto();

            var indice = contexto.Model
                .FindEntityType(typeof(Veiculo))!
                .GetIndexes()
                .Single(i => i.Properties.Count == 1 && i.Properties[0].Name == propriedade);

            Assert.True(indice.IsUnique);

            // o predicado é SQL bruto e cita a **coluna**, não a propriedade
            Assert.Equal("ativo", indice.GetFilter());
        }
    }
}
