using Locadora_Auto.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Locadora_Auto.Infra.Data.Configuracao
{
    public class DanoConfig : IEntityTypeConfiguration<Dano>
    {
        public void Configure(EntityTypeBuilder<Dano> builder)
        {
            builder.ToTable("tbDano");

            builder.HasKey(e => e.IdDano);

            builder.Property(e => e.IdDano)
                .HasColumnName("id_dano");

            builder.Property(c => c.Status)
                   .HasColumnName("tipo_status")
                   .HasConversion<int>()
                   .IsRequired();

            builder.Property(c => c.Tipo)
                   .HasColumnName("tipo_dano")
                   .HasConversion<int>()
                   .IsRequired();

            builder.Property(e => e.ValorEstimado)
               .HasColumnName("valor_estimado")
               .HasPrecision(10, 2)
               .IsRequired();

            builder.Property(e => e.Descricao)
                .HasColumnName("descricao")
                .HasMaxLength(200)
                .IsRequired();           

            builder.Property(e => e.DataRegistro)
                .HasColumnName("data_registro")
                .IsRequired();

            //chave estrangeira
            builder.Property(e => e.IdVistoria)
                .HasColumnName("id_vistoria");
            builder.HasOne(e => e.Vistoria)
                .WithMany(v => v.Danos)
                .HasForeignKey(e => e.IdVistoria)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

}
