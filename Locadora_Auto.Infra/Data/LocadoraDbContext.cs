using Locadora_Auto.Domain.Auditoria;
using Locadora_Auto.Domain.Entidades;
using Locadora_Auto.Domain.Entidades.Indentity;
using Locadora_Auto.Infra.Data.CurrentUsers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Locadora_Auto.Infra.Data
{
    public class LocadoraDbContext : IdentityDbContext<User, IdentityRole, string>
    {
        private readonly ICurrentUser _currentUser;
        public LocadoraDbContext(DbContextOptions<LocadoraDbContext> options, ICurrentUser currentUser) : base(options) 
        {
            _currentUser = currentUser;
        }

        //DbSets do domínio
        public DbSet<Clientes> Clientes => Set<Clientes>();
        public DbSet<Funcionario> Funcionarios => Set<Funcionario>();
        public DbSet<Veiculo> Veiculos => Set<Veiculo>();
        public DbSet<Locacao> Locacoes => Set<Locacao>();
        public DbSet<Pagamento> Pagamentos => Set<Pagamento>();
        public DbSet<Manutencao> Manutencoes => Set<Manutencao>();
        public DbSet<MovimentoVeiculo> MovimentosVeiculo => Set<MovimentoVeiculo>();
        public DbSet<BloqueioVeiculo> BloqueiosVeiculo => Set<BloqueioVeiculo>();
        public DbSet<TransferenciaVeiculo> TransferenciasVeiculo => Set<TransferenciaVeiculo>();
        public DbSet<RecusaSobreposicao> RecusasSobreposicao => Set<RecusaSobreposicao>();
        public DbSet<Reserva> Reservas => Set<Reserva>();
        public DbSet<Vistoria> Vistorias => Set<Vistoria>();
        public DbSet<Multa> Multas => Set<Multa>();
        public DbSet<Caucao> Caucoes => Set<Caucao>();
        public DbSet<Endereco> Enderecos => Set<Endereco>();
        public DbSet<Seguro> Seguros => Set<Seguro>();
        public DbSet<LocacaoSeguro> LocacaoSeguros => Set<LocacaoSeguro>();
        public DbSet<Dano> Danos => Set<Dano>();


        /// <summary>
        /// O Postgres grava data/hora em 'timestamp with time zone' e o Npgsql só aceita DateTime com
        /// Kind=Utc — datas vindas de DTO chegam como Unspecified e seriam rejeitadas. Este conversor
        /// normaliza na escrita e remarca como Utc na leitura.
        /// </summary>
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

        protected override void OnModelCreating(ModelBuilder builder)
        {
            //Aplica todas as configurações de entidade do assembly
            builder.ApplyConfigurationsFromAssembly(typeof(LocadoraDbContext).Assembly);

            base.OnModelCreating(builder);

            //O Identity nomeia as próprias tabelas em PascalCase; renomeia para o padrão snake_case
            builder.Entity<User>().ToTable("asp_net_users");
            builder.Entity<IdentityRole>().ToTable("asp_net_roles");
            builder.Entity<IdentityUserRole<string>>().ToTable("asp_net_user_roles");
            builder.Entity<IdentityUserClaim<string>>().ToTable("asp_net_user_claims");
            builder.Entity<IdentityUserLogin<string>>().ToTable("asp_net_user_logins");
            builder.Entity<IdentityUserToken<string>>().ToTable("asp_net_user_tokens");
            builder.Entity<IdentityRoleClaim<string>>().ToTable("asp_net_role_claims");

            //Aplica o conversor de UTC depois do base para alcançar também as entidades do Identity
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

            AplicarTokenDeConcorrencia(builder);
        }

        /// <summary>
        /// Marca o <c>xmin</c> — coluna de sistema que o Postgres já mantém em toda tabela — como
        /// token de concorrência. Com isso o UPDATE passa a levar <c>WHERE xmin = @original</c>: se
        /// outra pessoa gravou a mesma linha nesse meio tempo, nenhuma linha é afetada e o EF lança
        /// <see cref="Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException"/> em vez de a
        /// segunda gravação apagar a primeira em silêncio.
        ///
        /// Não cria coluna nova nem exige campo na entidade: o xmin entra como propriedade sombra.
        ///
        /// Só protege quando a entidade chega rastreada da leitura original — ou seja, quando o
        /// serviço carregou com <c>rastreado: true</c>. Em entidade lida sem rastreio, o
        /// <c>AtualizarSalvarAsync</c> relê a linha antes de gravar e o token compara com o valor
        /// recém-lido, o que não acusa conflito (mas também não gera falso positivo).
        /// </summary>
        private static void AplicarTokenDeConcorrencia(ModelBuilder builder)
        {
            foreach (var tipoEntidade in builder.Model.GetEntityTypes())
            {
                var tipo = tipoEntidade.ClrType;

                //tipos owned são gravados junto da raiz, que já carrega o próprio token
                if (tipoEntidade.IsOwned()) continue;

                //o Identity já traz ConcurrencyStamp para o mesmo fim
                if (typeof(IdentityUser).IsAssignableFrom(tipo)
                    || tipo.Namespace?.StartsWith("Microsoft.AspNetCore.Identity") == true) continue;

                //histórico temporal só recebe insert: não há concorrência de escrita a proteger
                if (typeof(ITemporalHistory).IsAssignableFrom(tipo)) continue;

                //mapeamento escrito à mão porque UseXminAsConcurrencyToken() está obsoleto no
                //Npgsql 8; o resultado é o mesmo, via API padrão do EF
                builder.Entity(tipo)
                    .Property<uint>("xmin")
                    .HasColumnName("xmin")
                    .HasColumnType("xid")
                    .IsRowVersion();
            }
        }

        //sobreescreve o saveChange para criar histórico temporal
        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            AplicarAuditoria();
            CriarHistoricoTemporal();
            return await base.SaveChangesAsync(cancellationToken);
        }

        private void CriarHistoricoTemporal()
        {
            var entries = ChangeTracker.Entries()
                .Where(e =>
                    e.State == EntityState.Modified ||
                    e.State == EntityState.Deleted);

            foreach (var entry in entries)
            {
                // Ignora histórico
                if (entry.Entity is ITemporalHistory)
                    continue;

                var entityType = entry.Metadata.ClrType; 

                // Verifica se a entidade optou por histórico
                var temporalInterface = entityType.GetInterfaces()
                    .FirstOrDefault(i =>
                        i.IsGenericType &&
                        i.GetGenericTypeDefinition() == typeof(ITemporalEntity<>));

                if (temporalInterface == null)
                    continue;

                var historyType = temporalInterface.GetGenericArguments()[0];

                var history = (ITemporalHistory)Activator.CreateInstance(historyType)!;

                MapearValores(entry, history);

                history.DataEvento = DateTime.UtcNow;
                history.Acao = entry.State == EntityState.Modified ? "UPDATE" : "DELETE";
                history.UsuarioEvento = _currentUser.UserId ?? "SYSTEM";

                Add(history);
            }
        }
        private static void MapearValores(EntityEntry entry, object history)
        {
            foreach (var prop in entry.OriginalValues.Properties)
            {
                var historyProp = history.GetType().GetProperty(prop.Name);

                if (historyProp == null || !historyProp.CanWrite)
                    continue;

                historyProp.SetValue(history, entry.OriginalValues[prop.Name]);
            }
        }

        private void AplicarAuditoria()
        {
            var entries = ChangeTracker.Entries<IAuditoria>();

            foreach (var entry in entries)
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.DataCriacao = DateTime.UtcNow;
                    entry.Entity.IdUsuarioCriacao = _currentUser.UserId ?? "SYSTEM";
                }

                if (entry.State == EntityState.Modified)
                {
                    entry.Entity.DataModificacao = DateTime.UtcNow;
                    entry.Entity.IdUsuarioModificacao = _currentUser.UserId ?? "SYSTEM";
                }
            }
        }
    }
}
