using Locadora_Auto.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Locadora_Auto.Infra.Data.Configuracao
{
    public class RefreshTokenConfig : IEntityTypeConfiguration<RefreshToken>
    {
        public void Configure(EntityTypeBuilder<RefreshToken> builder)
        {
            builder.ToTable("RefreshTokens");

            builder.HasKey(e => e.Id);

            builder.Property(e => e.Token)
                .HasMaxLength(512)
                .IsRequired();

            builder.HasIndex(e => e.Token)
                .IsUnique();

            builder.Property(e => e.ExpiraEm)
                .IsRequired();

            builder.Property(e => e.Revogado)
                .IsRequired();

            builder.Property(e => e.CriadoEm)
                .IsRequired();

            builder.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

}
