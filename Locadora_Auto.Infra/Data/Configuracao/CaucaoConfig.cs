using Locadora_Auto.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Locadora_Auto.Infra.Data.Configuracao
{
    public class CaucaoConfig : IEntityTypeConfiguration<Caucao>
    {
        public void Configure(EntityTypeBuilder<Caucao> builder)
        {
            builder.ToTable("caucao");

            builder.HasKey(c => c.IdCaucao);

            builder.Property(c => c.IdCaucao)
                   .HasColumnName("id_caucao");

            builder.Property(c => c.Valor)
                   .HasColumnName("valor")
                   .HasPrecision(10, 2)
                   .IsRequired();

            builder.Property(c => c.Status)
                   .HasColumnName("status")
                   .HasConversion<string>()
                   .HasMaxLength(20)
                   .IsRequired();

            builder.HasOne<Locacao>()
                   .WithMany(l => l.Caucoes)
                   .HasForeignKey("id_locacao")  // FK SOMBRA
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }


}
