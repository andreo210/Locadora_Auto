using Locadora_Auto.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Locadora_Auto.Infra.Data.Configuracao
{
    public class VistoriaConfig : IEntityTypeConfiguration<Vistoria>
    {
        public void Configure(EntityTypeBuilder<Vistoria> builder)
        {
            builder.ToTable("tbVistoria");

            builder.HasKey(v => v.IdVistoria);

            builder.Property(v => v.IdVistoria)
                   .HasColumnName("id_vistoria");

            builder.Property(v => v.IdLocacao)
                   .HasColumnName("id_locacao");

            builder.Property(v => v.IdFuncionario)
                   .HasColumnName("id_funcionario");

            builder.Property(v => v.Tipo)
                   .HasConversion<int>()
                   .HasColumnName("tipo");

            builder.Property(v => v.Combustivel)
                  .HasConversion<int>()
                  .HasColumnName("nivel_combustivel");

            builder.Property(v => v.Observacoes)
                   .HasColumnName("observacoes");

            builder.Property(v => v.KmVeiculo)
                   .HasColumnName("km_veiculo");

            builder.Property(v => v.DataVistoria)
                   .HasColumnName("data_vistoria");

            builder.HasOne(v => v.Locacao)
                   .WithMany(l => l.Vistorias)
                   .HasForeignKey(v => v.IdLocacao)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(v => v.Funcionario)
                   .WithMany()
                   .HasForeignKey(v => v.IdFuncionario);
        }
    }


}
