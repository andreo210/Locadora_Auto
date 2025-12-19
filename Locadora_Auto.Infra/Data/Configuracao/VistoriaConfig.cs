using Locadora_Auto.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Locadora_Auto.Infra.Data.Configuracao
{
    public class VistoriaConfig : IEntityTypeConfiguration<Vistoria>
    {
        public void Configure(EntityTypeBuilder<Vistoria> builder)
        {
            builder.ToTable("vistoria");

            builder.HasKey(e => e.IdVistoria);

            builder.Property(e => e.IdVistoria)
                .HasColumnName("id_vistoria");

            builder.Property(e => e.Tipo)
                .HasColumnName("tipo")
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(e => e.Observacoes)
                .HasColumnName("observacoes");

            builder.Property(e => e.DataVistoria)
                .HasColumnName("data_vistoria")
                .IsRequired();

            builder.HasOne(e => e.Locacao)
                .WithMany()
                .HasForeignKey(e => e.IdLocacao);

            builder.HasOne(e => e.Funcionario)
                .WithMany()
                .HasForeignKey(e => e.IdFuncionario);
        }
    }

}
