using Locadora_Auto.Domain.Auditoria;
using Locadora_Auto.Domain.Entidades;
using Locadora_Auto.Domain.Entidades.Indentity;
using Locadora_Auto.Infra.Data.CurrentUsers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

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
        public DbSet<Reserva> Reservas => Set<Reserva>();
        public DbSet<Vistoria> Vistorias => Set<Vistoria>();
        public DbSet<Multa> Multas => Set<Multa>();
        public DbSet<Caucao> Caucoes => Set<Caucao>();
        public DbSet<Endereco> Enderecos => Set<Endereco>();
        public DbSet<Seguro> Seguros => Set<Seguro>();
        public DbSet<LocacaoSeguro> LocacaoSeguros => Set<LocacaoSeguro>();
        public DbSet<Dano> Danos => Set<Dano>();


        protected override void OnModelCreating(ModelBuilder builder)
        {
            //Aplica todas as configurações de entidade do assembly
            builder.ApplyConfigurationsFromAssembly(typeof(LocadoraDbContext).Assembly);

            base.OnModelCreating(builder);


            //Ajuste charset MariaDB
            builder.UseCollation("utf8mb4_unicode_ci");
            builder.HasCharSet("utf8mb4");
        }

        //sobreescreve o saveChange para criar histórico temporal
        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
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

                history.DataEvento = DateTime.Now;
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
    }
}
