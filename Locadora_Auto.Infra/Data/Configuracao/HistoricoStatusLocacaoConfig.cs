using Locadora_Auto.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Locadora_Auto.Infra.Data.Configuracao
{
    public class HistoricoStatusLocacaoConfig : IEntityTypeConfiguration<HistoricoStatusLocacao>
    {
        public void Configure(EntityTypeBuilder<HistoricoStatusLocacao> builder)
        {
            builder.ToTable("historico_status_locacao");

            builder.HasKey(e => e.Id);

            builder.Property(e => e.Id)
                .HasColumnName("id");

            builder.Property(e => e.Status)
                .HasColumnName("status")
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(e => e.DataStatus)
                .HasColumnName("data_status")
                .IsRequired();

            //builder.HasOne(e => e.Locacao)
            //    .WithMany(l => l.HistoricoStatus)
            //    .HasForeignKey(e => e.IdLocacao);

            builder.HasOne(e => e.Funcionario)
                .WithMany()
                .HasForeignKey(e => e.IdFuncionario);
        }
    }

}
