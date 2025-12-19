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

            builder.HasKey(e => e.IdCaucao);

            builder.Property(e => e.IdCaucao)
                .HasColumnName("id_caucao");

            builder.Property(e => e.Valor)
                .HasColumnName("valor")
                .HasPrecision(10, 2)
                .IsRequired();

            builder.Property(e => e.Status)
                .HasColumnName("status")
                .HasMaxLength(20)
                .IsRequired();

            builder.HasOne(e => e.Locacao)
                .WithMany()
                .HasForeignKey(e => e.IdLocacao);
        }
    }

}
