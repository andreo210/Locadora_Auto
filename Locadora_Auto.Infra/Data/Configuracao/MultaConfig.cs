using Locadora_Auto.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Locadora_Auto.Infra.Data.Configuracao
{
    public class MultaConfig : IEntityTypeConfiguration<Multa>
    {
        public void Configure(EntityTypeBuilder<Multa> builder)
        {
            builder.ToTable("multa");

            builder.HasKey(e => e.IdMulta);

            builder.Property(e => e.IdMulta)
                .HasColumnName("id_multa");

            builder.Property(e => e.Tipo)
                .HasColumnName("tipo")
                .HasMaxLength(30)
                .IsRequired();

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
