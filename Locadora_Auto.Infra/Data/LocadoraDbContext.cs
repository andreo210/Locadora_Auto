using Locadora_Auto.Domain.Entidades;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Locadora_Auto.Infra.Data
{
    public class LocadoraDbContext : IdentityDbContext<ApplicationUser>
    {
        public LocadoraDbContext(DbContextOptions<LocadoraDbContext> options) : base(options) { }

        //DbSets do domínio
        public DbSet<Cliente> Clientes => Set<Cliente>();
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
    }
}
