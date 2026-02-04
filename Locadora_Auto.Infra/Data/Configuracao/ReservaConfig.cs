using Locadora_Auto.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Locadora_Auto.Infra.Data.Configuracao
{
    public class ReservaConfig : IEntityTypeConfiguration<Reserva>
    {
        public void Configure(EntityTypeBuilder<Reserva> builder)
        {
            builder.ToTable("tbReserva");

            builder.HasKey(e => e.IdReserva);

            builder.Property(e => e.IdReserva)
                .HasColumnName("id_reserva");

            builder.Property(e => e.DataInicio)
                .HasColumnName("data_inicio")
                .IsRequired();

            builder.Property(e => e.IdCliente)
               .HasColumnName("id_cliente")
               .IsRequired();

            builder.Property(e => e.IdFilial)
               .HasColumnName("id_filial")
               .IsRequired();

            builder.Property(e => e.IdCategoria)
               .HasColumnName("id_categoria_veiculo")
               .IsRequired();

            builder.Property(e => e.DataFim)
                .HasColumnName("data_fim")
                .IsRequired();

            builder.Property(c => c.Status)
                   .HasColumnName("status")
                   .HasConversion<int>()
                   .HasMaxLength(20)
                   .IsRequired();

            builder.HasOne(e => e.CategoriaVeiculo)
                .WithMany(v => v.Reservas)
                .HasForeignKey(e => e.IdCategoria)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(e => e.Cliente)
               .WithMany(v => v.Reservas)
               .HasForeignKey(e => e.IdCliente)
               .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(e => e.Filial)
              .WithMany(v => v.Reserva)
              .HasForeignKey(e => e.IdFilial);

        }
    }

}
