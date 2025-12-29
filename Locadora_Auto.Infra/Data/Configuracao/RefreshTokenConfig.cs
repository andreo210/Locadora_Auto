using Locadora_Auto.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Locadora_Auto.Infra.Data.Configuracao
{
    public class RefreshTokenConfig : IEntityTypeConfiguration<RefreshToken>
    {
        public void Configure(EntityTypeBuilder<RefreshToken> builder)
        {
            builder.ToTable("refresh_tokens");

            builder.HasKey(e => e.Id);

            builder.Property(e => e.Id)
                .HasColumnName("id")
                .IsRequired();

            builder.Property(e => e.Token)
                .HasColumnName("token")
                .IsRequired();

            builder.HasIndex(e => e.Token)
                .IsUnique();

            builder.Property(e => e.ExpiraEm)
                .HasColumnName("expira_em")
                .IsRequired();

            builder.Property(e => e.Revogado)
                .HasColumnName("revogado")
                .IsRequired();

            builder.Property(e => e.CriadoEm)
                .HasColumnName("criado_em")
                .IsRequired();

            builder.Property(e => e.UserId)
                .HasColumnName("user_id");

            builder.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId);
        }
    }

}
