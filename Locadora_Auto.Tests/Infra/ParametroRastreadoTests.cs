using Locadora_Auto.Domain.Entidades;
using Locadora_Auto.Infra.Data;
using Locadora_Auto.Infra.Data.CurrentUsers;
using Locadora_Auto.Infra.Data.Repositorio;
using Locadora_Auto.Tests.Fabricas;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Locadora_Auto.Tests.Infra
{
    /// <summary>
    /// <c>RepositorioGlobal.ObterPorIdAsync</c> já teve o <c>rastreado</c> invertido: ele fazia
    /// <c>FindAsync</c> (que rastreia) e <b>destacava quando <c>rastreado: true</c></b>. Como o
    /// nome dizia o contrário, os serviços pediam rastreio e recebiam entidade solta — e o
    /// <c>Add</c> da locação passava a pintar veículo, cliente e funcionário de <c>Added</c>,
    /// tentando inseri-los de novo (ver <see cref="RastreioDoAtivoTests"/>).
    ///
    /// Este teste fixa o significado do parâmetro. Só o caminho rastreado é verificável sem banco,
    /// porque ele resolve pelo change tracker; o caminho sem rastreio virou consulta
    /// <c>AsNoTracking</c> e precisa de conexão — foi exercitado contra o <c>locadora_autos</c>
    /// real, onde traduz para SQL e devolve zero entidades rastreadas.
    /// </summary>
    public class ParametroRastreadoTests
    {
        private sealed class UsuarioFake : ICurrentUser
        {
            public string? UserId => "teste";
            public bool IsAuthenticated => true;
        }

        private static LocadoraDbContext MontarContexto()
        {
            var opcoes = new DbContextOptionsBuilder<LocadoraDbContext>()
                // string de conexão nunca usada: o FindAsync resolve pelo tracker
                .UseNpgsql("Host=localhost;Database=modelo_para_teste;Username=postgres;Password=postgres")
                .UseSnakeCaseNamingConvention()
                .Options;

            return new LocadoraDbContext(opcoes, new UsuarioFake());
        }

        [Fact]
        public async Task Rastreado_true_devolve_a_entidade_seguida_pelo_contexto()
        {
            using var contexto = MontarContexto();

            var veiculo = Fabrica.Veiculo();
            Fabrica.DefinirId(veiculo, 1);
            contexto.Attach(veiculo);

            var repositorio = new VeiculosRepository(contexto);

            var obtido = await repositorio.ObterPorIdAsync(1, true);

            // é a mesma instância, e continua rastreada: sem isso a alteração feita pelo serviço
            // não vira UPDATE, e o filho novo (MovimentoVeiculo) não vira INSERT
            Assert.Same(veiculo, obtido);
            Assert.Equal(EntityState.Unchanged, contexto.Entry(obtido).State);
        }

        [Fact]
        public async Task Rastreado_true_mantem_a_transicao_do_ativo_visivel_para_o_SaveChanges()
        {
            using var contexto = MontarContexto();

            var veiculo = Fabrica.Veiculo();
            Fabrica.DefinirId(veiculo, 1);
            contexto.Attach(veiculo);

            var repositorio = new VeiculosRepository(contexto);
            var obtido = await repositorio.ObterPorIdAsync(1, true);

            obtido.Desativar();

            var entrada = contexto.Entry(obtido);
            Assert.Equal(EntityState.Modified, entrada.State);
            Assert.True(entrada.Property(v => v.Status).IsModified);

            // o movimento da RN-37 entra como filho novo do agregado — só o change tracker enxerga
            Assert.Equal(
                EntityState.Added,
                contexto.Entry(veiculo.Movimentos.Last()).State);
        }
    }
}
