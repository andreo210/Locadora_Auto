using Locadora_Auto.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Locadora_Auto.Infra.Data.Configuracao
{
    public class LocacaoSeguroConfig : IEntityTypeConfiguration<LocacaoSeguro>
    {
        public void Configure(EntityTypeBuilder<LocacaoSeguro> builder)
        {
            builder.ToTable("locacao_seguro");

            builder.HasKey(e => new { e.IdLocacao, e.IdSeguro });

            builder.Property(e => e.IdLocacao)
                .HasColumnName("quantidade")
                .IsRequired();

            //builder.HasOne(e => e.Locacao)
            //    .WithMany(l => l.Adicionais)
            //    .HasForeignKey(e => e.IdLocacao);

            //builder.HasOne(e => e.IdSeguro)
            //    .WithMany()
            //    .HasForeignKey(e => e.IdAdicional);
        }
    }

}
