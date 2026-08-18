using Locadora_Auto.Domain.Entidades;
using Locadora_Auto.Infra.Data;
using Locadora_Auto.Infra.Data.CurrentUsers;
using Locadora_Auto.Infra.Data.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using System.Reflection;
using System.Text.RegularExpressions;
using Xunit;

namespace Locadora_Auto.Tests.Infra
{
    /// <summary>
    /// O filtro da RN-40 é a peça que impede dois contratos no mesmo carro, e os testes de serviço
    /// o executam sobre o fake — LINQ em memória, que aceita expressão nenhum provider traduz. Aqui
    /// ele passa pelo provider do Postgres de verdade: <c>ToQueryString</c> compila a consulta sem
    /// abrir conexão, então uma expressão intraduzível quebra o teste em vez de quebrar em produção.
    ///
    /// Os demais existem para a constraint <c>EXCLUDE</c>: o predicado dela precisa listar os
    /// status terminais na representação real da coluna, e nada no compilador liga uma coisa à
    /// outra.
    /// </summary>
    public class SobreposicaoDeContratoTests
    {
        private sealed class UsuarioFake : ICurrentUser
        {
            public string? UserId => "teste";
            public bool IsAuthenticated => true;
        }

        /// <summary>
        /// SQL da migration <b>mais recente</b> que (re)define a constraint — que é a que vale no
        /// banco. Achar por reflexão em vez de fixar o tipo é o que mantém este teste honesto
        /// quando o predicado mudar: mexer nele é sempre migration nova, porque editar a antiga não
        /// migra banco que já rodou. Fixado num tipo, o teste passaria a conferir uma definição
        /// que o banco não usa mais.
        /// </summary>
        private static string SqlDaConstraintVigente()
        {
            var migrations = typeof(SobreposicaoDeContrato).Assembly
                .GetTypes()
                .Where(t => typeof(Migration).IsAssignableFrom(t) && !t.IsAbstract)
                .Select(t => new { Id = t.GetCustomAttribute<MigrationAttribute>()?.Id, Tipo = t })
                .Where(m => m.Id != null)
                .OrderBy(m => m.Id, StringComparer.Ordinal);

            string? vigente = null;

            foreach (var migration in migrations)
            {
                var sql = string.Join("\n", ((Migration)Activator.CreateInstance(migration.Tipo)!)
                    .UpOperations.OfType<SqlOperation>()
                    .Select(operacao => operacao.Sql));

                if (sql.Contains("ADD CONSTRAINT ex_locacao_sem_sobreposicao"))
                    vigente = sql;
            }

            Assert.NotNull(vigente);
            return vigente!;
        }

        private static LocadoraDbContext MontarContexto()
        {
            var opcoes = new DbContextOptionsBuilder<LocadoraDbContext>()
                // string de conexão nunca usada: compilar a consulta não abre conexão
                .UseNpgsql("Host=localhost;Database=modelo_para_teste;Username=postgres;Password=postgres")
                .UseSnakeCaseNamingConvention()
                .Options;

            return new LocadoraDbContext(opcoes, new UsuarioFake());
        }

        [Fact]
        public void Filtro_de_sobreposicao_e_traduzido_para_SQL()
        {
            using var contexto = MontarContexto();
            var inicio = new DateTime(2026, 3, 10, 9, 0, 0, DateTimeKind.Utc);

            var sql = contexto.Set<Locacao>()
                .Where(Locacao.Sobrepostas(7, inicio, inicio.AddDays(4)))
                .ToQueryString();

            Assert.Contains("id_veiculo", sql);
            Assert.Contains("data_inicio", sql);

            // COALESCE(data_fim_real, data_fim_prevista): enquanto o contrato está aberto vale a
            // previsão; depois de fechado, o que de fato aconteceu
            Assert.Contains("COALESCE", sql, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("data_fim_real", sql);
            Assert.Contains("data_fim_prevista", sql);
        }

        [Fact]
        public void Status_terminal_chega_ao_banco_como_texto()
        {
            using var contexto = MontarContexto();

            var status = contexto.Model.FindEntityType(typeof(Locacao))!.FindProperty(nameof(Locacao.Status))!;

            // HasConversion<string>() não deixa conversor próprio na propriedade: quem carrega a
            // conversão é o type mapping, e é dele que sai o texto que vai para a coluna
            var conversor = status.GetTypeMapping().Converter;

            Assert.NotNull(conversor);
            Assert.Equal(typeof(string), status.GetProviderClrType());

            // é isto que o predicado da constraint EXCLUDE precisa listar — entre aspas. Escrever
            // ali os inteiros do enum compila, não dá erro nenhum e desliga a garantia em silêncio.
            var terminaisNoBanco = Locacao.StatusTerminais
                .Select(s => (string)conversor!.ConvertToProvider(s)!)
                .ToArray();

            // RN-61: Cancelada porque o contrato foi anulado e o período tem que voltar à oferta
            // retroativamente; Finalizada porque o ciclo acabou. Devolvida, Fechada e
            // ComSaldoResidual ficam de fora — o carro rodou naquele período
            Assert.Equal(new[] { "Finalizada", "Cancelada" }, terminaisNoBanco);
        }

        /// <summary>
        /// A migration é SQL bruto: nada no compilador liga o predicado dela a
        /// <c>Locacao.StatusTerminais</c>. Acrescentar um status terminal no domínio e esquecer a
        /// constraint não quebra build nem teste nenhum — a constraint só passa a deixar passar
        /// contrato encerrado, sem sinal de erro. É este teste que faz esse esquecimento doer.
        /// </summary>
        [Fact]
        public void Constraint_lista_exatamente_os_status_terminais_do_dominio()
        {
            var sql = SqlDaConstraintVigente();

            var listaDoPredicado = Regex.Match(sql, @"status NOT IN \(([^)]*)\)").Groups[1].Value;

            Assert.NotEmpty(listaDoPredicado);

            var noPredicado = listaDoPredicado
                .Split(',')
                .Select(literal => literal.Trim().Trim('\''))
                .ToArray();

            Assert.Equal(Locacao.StatusTerminais.Select(s => s.ToString()).ToArray(), noPredicado);
        }

        [Fact]
        public void Constraint_usa_o_mesmo_intervalo_que_o_dominio()
        {
            var sql = SqlDaConstraintVigente();

            // btree_gist é o que deixa a igualdade de id_veiculo conviver com a sobreposição do
            // intervalo no mesmo índice; sem a extensão o ALTER TABLE falha. Ela é criada na
            // SobreposicaoDeContrato e não se repete nas migrations que só reescrevem o predicado
            Assert.Contains("CREATE EXTENSION IF NOT EXISTS btree_gist",
                string.Join("\n", new SobreposicaoDeContrato()
                    .UpOperations.OfType<SqlOperation>()
                    .Select(operacao => operacao.Sql)));

            Assert.Contains("ex_locacao_sem_sobreposicao", sql);

            // mesmo COALESCE de Locacao.Sobrepostas, e tstzrange é meio-aberto por padrão — é o que
            // faz contrato encostado no anterior ser aceito nos dois lados
            Assert.Contains("tstzrange(data_inicio, COALESCE(data_fim_real, data_fim_prevista))", sql);
            Assert.Contains("id_veiculo WITH =", sql);
        }
    }
}
