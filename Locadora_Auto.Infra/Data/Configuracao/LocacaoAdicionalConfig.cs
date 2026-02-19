using Locadora_Auto.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Locadora_Auto.Infra.Data.Configuracao
{
    public class LocacaoAdicionalConfig : IEntityTypeConfiguration<LocacaoAdicional>
    {
        public void Configure(EntityTypeBuilder<LocacaoAdicional> builder)
        {
            builder.ToTable("tbLocacaoAdicional");

            builder.HasKey(e => e.IdLocacaoAdicional);

            builder.Property(e => e.IdLocacaoAdicional)
                .HasColumnName("id_locacao_adicional");

            builder.Property(e => e.IdLocacao)
                .HasColumnName("id_locacao");

            builder.Property(e => e.IdAdicional)
                .HasColumnName("id_adicional");

            builder.Property(e => e.Quantidade)
                .HasColumnName("quantidade");

            builder.Property(e => e.Dias)
                .HasColumnName("dias");

            builder.Property(e => e.ValorDiariaContratada)
                .HasColumnName("valor_diaria");

            builder.Property(e => e.ValorTotal)
                .HasColumnName("valor_total");

            builder.HasOne(e => e.Adicional)
                .WithMany(x => x.LocacaoAdicionals)
                .HasForeignKey(e => e.IdAdicional);

            builder.HasOne<Locacao>()
                .WithMany(l => l.Adicionais)
                .HasForeignKey(e => e.IdLocacao)
                .OnDelete(DeleteBehavior.Cascade);
        }

    }

}
