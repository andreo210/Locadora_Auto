using Locadora_Auto.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Locadora_Auto.Infra.Data.Configuracao
{
    public class LocacaoSeguroConfig : IEntityTypeConfiguration<LocacaoSeguro>
    {
        public void Configure(EntityTypeBuilder<LocacaoSeguro> builder)
        {
            builder.ToTable("tbLocacao_seguro");

            builder.HasKey(ls => ls.IdLocacaoSeguro);

            builder.Property(ls => ls.IdLocacaoSeguro)
                   .HasColumnName("id_locacao_seguro");

            builder.Property(ls => ls.IdLocacao)
                   .HasColumnName("id_locacao")
                   .IsRequired();

            builder.Property(ls => ls.IdSeguro)
                   .HasColumnName("id_seguro")
                   .IsRequired();

            builder.Property(ls => ls.Ativo)
                  .HasColumnName("ativo")
                  .IsRequired();


            //chave estrangeira
            builder.HasOne<Locacao>()
                   .WithMany(l => l.Seguros)
                   .HasForeignKey(ls => ls.IdLocacao)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }

}
