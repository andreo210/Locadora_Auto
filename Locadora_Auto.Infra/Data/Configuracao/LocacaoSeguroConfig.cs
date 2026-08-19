using Locadora_Auto.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Locadora_Auto.Infra.Data.Configuracao
{
    public class LocacaoSeguroConfig : IEntityTypeConfiguration<LocacaoSeguro>
    {
        public void Configure(EntityTypeBuilder<LocacaoSeguro> builder)
        {
            builder.ToTable("tb_locacao_seguro");

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

            // RN-18 e RN-25: diária e franquia como estavam no dia da contratação. Mesmo desenho
            // do valor_diaria_contratada da locação — default zero só para a coluna nascer, e as
            // linhas antigas preenchidas na migration a partir do cadastro do seguro.
            builder.Property(ls => ls.ValorDiariaContratada)
                  .HasColumnName("valor_diaria_contratada")
                  .HasPrecision(10, 2)
                  .HasDefaultValue(0m)
                  .IsRequired();

            builder.Property(ls => ls.FranquiaContratada)
                  .HasColumnName("franquia_contratada")
                  .HasPrecision(10, 2)
                  .HasDefaultValue(0m)
                  .IsRequired();

            // RN-19: a janela em que a proteção cobriu. Sem as duas datas a pró-rata é inexequível
            // — `ativo = false` diz que foi cancelada, mas não quando
            builder.Property(ls => ls.DataContratacao)
                  .HasColumnName("data_contratacao")
                  .IsRequired();

            builder.Property(ls => ls.DataCancelamento)
                  .HasColumnName("data_cancelamento");


            //chave estrangeira
            builder.HasOne<Locacao>()
                   .WithMany(l => l.Seguros)
                   .HasForeignKey(ls => ls.IdLocacao)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }

}
