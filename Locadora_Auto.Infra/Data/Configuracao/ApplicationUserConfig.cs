using Locadora_Auto.Domain.Entidades.Indentity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Locadora_Auto.Infra.Data.Configuracao
{
    public class ApplicationUserConfig : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("aspnet_users");

            builder.Property(e => e.NomeCompleto)
                .HasColumnName("NomeCompleto")
                .HasMaxLength(255);

            builder.Property(e => e.Ativo)
                .HasColumnName("Ativo");

            builder.Property(e => e.DataCriacao)
                .HasColumnName("DataCriacao");
        }
    }

}
