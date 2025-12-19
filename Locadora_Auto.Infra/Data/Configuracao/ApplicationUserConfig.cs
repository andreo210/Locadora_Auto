using Locadora_Auto.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Locadora_Auto.Infra.Data.Configuracao
{
    public class ApplicationUserConfig : IEntityTypeConfiguration<ApplicationUser>
    {
        public void Configure(EntityTypeBuilder<ApplicationUser> builder)
        {
            builder.ToTable("AspNetUsers");

            builder.Property(e => e.NomeCompleto)
                .HasColumnName("NomeCompleto")
                .HasMaxLength(255);

            builder.Property(e => e.Cpf)
                .HasColumnName("Cpf")
                .HasMaxLength(11);

            builder.Property(e => e.Ativo)
                .HasColumnName("Ativo");

            builder.Property(e => e.DataCriacao)
                .HasColumnName("DataCriacao");
        }
    }

}
