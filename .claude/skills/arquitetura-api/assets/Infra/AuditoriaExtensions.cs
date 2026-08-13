using {{RootNamespace}}.Domain.Auditoria;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace {{RootNamespace}}.Infra.Data
{
    /// <summary>
    /// Auditoria, histórico temporal e normalização de datas em UTC — escritos como extensões
    /// em vez de classe base porque o contexto costuma já herdar de IdentityDbContext, e C# não
    /// tem herança múltipla. Ligue no seu contexto assim:
    ///
    /// <code>
    /// protected override void OnModelCreating(ModelBuilder builder)
    /// {
    ///     builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    ///     base.OnModelCreating(builder);          // antes do conversor, para alcançar o Identity
    ///     builder.AplicarConversorDataUtc();
    /// }
    ///
    /// public override async Task&lt;int&gt; SaveChangesAsync(CancellationToken ct = default)
    /// {
    ///     var usuario = _currentUser.UserId ?? "SYSTEM";
    ///     ChangeTracker.AplicarAuditoria(usuario);
    ///     this.CriarHistoricoTemporal(usuario);
    ///     return await base.SaveChangesAsync(ct);
    /// }
    /// </code>
    /// </summary>
    public static class AuditoriaExtensions
    {
        /// <summary>
        /// Preenche as colunas de IAuditoria. Data sempre em UtcNow: DateTime.Now gravaria
        /// o horário local numa coluna 'timestamp with time zone' e as comparações errariam
        /// pelo fuso.
        /// </summary>
        public static void AplicarAuditoria(this ChangeTracker changeTracker, string usuario)
        {
            foreach (var entry in changeTracker.Entries<IAuditoria>())
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.DataCriacao = DateTime.UtcNow;
                    entry.Entity.IdUsuarioCriacao = usuario;
                }

                if (entry.State == EntityState.Modified)
                {
                    entry.Entity.DataModificacao = DateTime.UtcNow;
                    entry.Entity.IdUsuarioModificacao = usuario;
                }
            }
        }

        /// <summary>
        /// Para cada entidade que declara ITemporalEntity&lt;THistory&gt;, grava uma linha de
        /// histórico com os valores ORIGINAIS antes do UPDATE/DELETE, na mesma transação.
        /// A cópia é por nome de propriedade: nome diferente = campo não copiado, sem erro.
        /// </summary>
        public static void CriarHistoricoTemporal(this DbContext context, string usuario)
        {
            var alteradas = context.ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Modified || e.State == EntityState.Deleted)
                .ToList();

            foreach (var entry in alteradas)
            {
                if (entry.Entity is ITemporalHistory)
                    continue;                      // histórico não gera histórico

                var interfaceTemporal = entry.Metadata.ClrType.GetInterfaces()
                    .FirstOrDefault(i => i.IsGenericType &&
                                         i.GetGenericTypeDefinition() == typeof(ITemporalEntity<>));

                if (interfaceTemporal == null)
                    continue;                      // entidade não optou por histórico

                var tipoHistorico = interfaceTemporal.GetGenericArguments()[0];
                var historico = (ITemporalHistory)Activator.CreateInstance(tipoHistorico)!;

                CopiarValoresOriginais(entry, historico);

                historico.DataEvento = DateTime.UtcNow;
                historico.Acao = entry.State == EntityState.Modified ? "UPDATE" : "DELETE";
                historico.UsuarioEvento = usuario;

                // cast para object de propósito: o EF resolve a entidade pelo tipo em tempo de
                // execução, não pela interface
                context.Add((object)historico);
            }
        }

        private static void CopiarValoresOriginais(EntityEntry entry, object historico)
        {
            foreach (var propriedade in entry.OriginalValues.Properties)
            {
                var destino = historico.GetType().GetProperty(propriedade.Name);

                if (destino == null || !destino.CanWrite)
                    continue;

                destino.SetValue(historico, entry.OriginalValues[propriedade.Name]);
            }
        }

        /// <summary>
        /// O Postgres grava em 'timestamp with time zone' e o Npgsql só aceita DateTime com
        /// Kind=Utc — data vinda de DTO chega como Unspecified e seria rejeitada. Este conversor
        /// normaliza na escrita e remarca como Utc na leitura.
        ///
        /// Ele salva a gravação, mas não conserta comparação em memória: continua valendo
        /// DateTime.UtcNow no código de domínio.
        /// </summary>
        public static void AplicarConversorDataUtc(this ModelBuilder builder)
        {
            foreach (var tipoEntidade in builder.Model.GetEntityTypes())
            {
                foreach (var propriedade in tipoEntidade.GetProperties())
                {
                    if (propriedade.ClrType == typeof(DateTime))
                        propriedade.SetValueConverter(ConversorDataUtc);
                    else if (propriedade.ClrType == typeof(DateTime?))
                        propriedade.SetValueConverter(ConversorDataUtcNulavel);
                }
            }
        }

        /// <summary>
        /// Liga o <c>xmin</c> — coluna de sistema que o Postgres já mantém em toda tabela — como
        /// token de concorrência otimista. O UPDATE passa a levar <c>WHERE xmin = @original</c>:
        /// se outra pessoa gravou a mesma linha nesse meio tempo, nenhuma linha é afetada e o EF
        /// lança <see cref="DbUpdateConcurrencyException"/> em vez de a segunda gravação apagar a
        /// primeira em silêncio.
        ///
        /// Não cria coluna nem exige campo na entidade — entra como propriedade sombra. Em banco
        /// que não seja Postgres, o equivalente é uma coluna de versão com <c>IsRowVersion()</c>.
        ///
        /// Atenção na migration: o gerador do EF não sabe que xmin é coluna de sistema e produz um
        /// AddColumn por tabela, que falha no Postgres. Esvazie o Up/Down da migration gerada — o
        /// banco não precisa mudar, só o modelo.
        ///
        /// Só protege quando a entidade chega rastreada da leitura original (<c>rastreado: true</c>).
        /// </summary>
        public static void AplicarTokenDeConcorrencia(this ModelBuilder builder)
        {
            foreach (var tipoEntidade in builder.Model.GetEntityTypes())
            {
                //tipos owned são gravados junto da raiz, que já carrega o próprio token
                if (tipoEntidade.IsOwned()) continue;

                //histórico temporal só recebe insert: não há concorrência de escrita a proteger
                if (typeof(ITemporalHistory).IsAssignableFrom(tipoEntidade.ClrType)) continue;

                //o Identity já traz ConcurrencyStamp para o mesmo fim
                if (tipoEntidade.ClrType.Namespace?.StartsWith("Microsoft.AspNetCore.Identity") == true) continue;

                //via API padrão do EF: o atalho UseXminAsConcurrencyToken() está obsoleto no Npgsql 8
                builder.Entity(tipoEntidade.ClrType)
                    .Property<uint>("xmin")
                    .HasColumnName("xmin")
                    .HasColumnType("xid")
                    .IsRowVersion();
            }
        }

        private static readonly ValueConverter<DateTime, DateTime> ConversorDataUtc = new(
            valor => valor.Kind == DateTimeKind.Local
                ? valor.ToUniversalTime()
                : DateTime.SpecifyKind(valor, DateTimeKind.Utc),
            valor => DateTime.SpecifyKind(valor, DateTimeKind.Utc));

        private static readonly ValueConverter<DateTime?, DateTime?> ConversorDataUtcNulavel = new(
            valor => valor.HasValue
                ? (valor.Value.Kind == DateTimeKind.Local
                    ? valor.Value.ToUniversalTime()
                    : DateTime.SpecifyKind(valor.Value, DateTimeKind.Utc))
                : valor,
            valor => valor.HasValue
                ? DateTime.SpecifyKind(valor.Value, DateTimeKind.Utc)
                : valor);
    }
}
