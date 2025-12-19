using Locadora_Auto.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Locadora_Auto.Infra.Data.Configuracao
{
    public class ReservaConfig : IEntityTypeConfiguration<Reserva>
    {
        public void Configure(EntityTypeBuilder<Reserva> builder)
        {
            builder.ToTable("reserva");

            builder.HasKey(e => e.IdReserva);

            builder.Property(e => e.IdReserva)
                .HasColumnName("id_reserva");

            builder.Property(e => e.DataInicio)
                .HasColumnName("data_inicio")
                .IsRequired();

            builder.Property(e => e.DataFim)
                .HasColumnName("data_fim")
                .IsRequired();

            builder.Property(e => e.Status)
                .HasColumnName("status")
                .HasMaxLength(20)
                .IsRequired();

            builder.HasOne(e => e.Cliente)
                .WithMany()
                .HasForeignKey(e => e.IdCliente);

            builder.HasOne(e => e.Categoria)
                .WithMany()
                .HasForeignKey(e => e.IdCategoria);
        }
    }

}
