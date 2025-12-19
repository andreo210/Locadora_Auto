using Locadora_Auto.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Locadora_Auto.Infra.Data.Configuracao
{
    public class LocacaoAdicionalConfig : IEntityTypeConfiguration<LocacaoAdicional>
    {
        public void Configure(EntityTypeBuilder<LocacaoAdicional> builder)
        {
            builder.ToTable("locacao_adicional");

            builder.HasKey(e => new { e.IdLocacao, e.IdAdicional });

            builder.Property(e => e.Quantidade)
                .HasColumnName("quantidade")
                .IsRequired();

            //builder.HasOne(e => e.Locacao)
            //    .WithMany(l => l.Adicionais)
            //    .HasForeignKey(e => e.IdLocacao);

            builder.HasOne(e => e.Adicional)
                .WithMany()
                .HasForeignKey(e => e.IdAdicional);
        }
    }

}
