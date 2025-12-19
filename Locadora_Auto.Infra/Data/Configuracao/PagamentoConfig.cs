using Locadora_Auto.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Locadora_Auto.Infra.Data.Configuracao
{
    public class PagamentoConfig : IEntityTypeConfiguration<Pagamento>
    {
        public void Configure(EntityTypeBuilder<Pagamento> builder)
        {
            builder.ToTable("pagamento");

            builder.HasKey(e => e.IdPagamento);

            builder.Property(e => e.IdPagamento)
                .HasColumnName("id_pagamento");

            builder.Property(e => e.Valor)
                .HasColumnName("valor")
                .HasPrecision(10, 2)
                .IsRequired();

            builder.Property(e => e.DataPagamento)
                .HasColumnName("data_pagamento")
                .IsRequired();

            builder.Property(e => e.Status)
                .HasColumnName("status")
                .HasMaxLength(20)
                .IsRequired();

            builder.HasOne(e => e.Locacao)
                .WithMany()
                .HasForeignKey(e => e.IdLocacao);
        }
    }

}
