using Locadora_Auto.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Locadora_Auto.Infra.Data.Configuracao
{
    public class ManutencaoConfig : IEntityTypeConfiguration<Manutencao>
    {
        public void Configure(EntityTypeBuilder<Manutencao> builder)
        {
            builder.ToTable("tbManutencao");

            //chave primaria
            builder.HasKey(e => e.IdManutencao);
            builder.Property(e => e.IdManutencao)
                .HasColumnName("id_manutencao");

            //chave estrangeira
            builder.HasOne<Veiculo>()
                   .WithMany(l => l.Manutencoes)
                   .HasForeignKey("id_veiculo")  // FK SOMBRA
                   .OnDelete(DeleteBehavior.Cascade);

            builder.Property(c => c.Tipo)
               .HasColumnName("tipo_manutencao")
               .HasConversion<int>()
               .IsRequired();

            builder.Property(c => c.Descricao)
                .HasColumnName("descricao");

            builder.Property(e => e.Custo)
                .HasColumnName("custo")
                .HasPrecision(10, 2)
                .IsRequired();

            builder.Property(c => c.DataInicio)
               .HasColumnName("data_inicio");

            builder.Property(c => c.DataFim)
               .HasColumnName("data_fim");

            builder.Property(c => c.Status)
                .HasColumnName("status_manutencao")
                .HasConversion<int>()
                .IsRequired();

            

            

        }
    }

}
