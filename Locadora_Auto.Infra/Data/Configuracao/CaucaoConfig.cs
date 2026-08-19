using Locadora_Auto.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Locadora_Auto.Infra.Data.Configuracao
{
    public class CaucaoConfig : IEntityTypeConfiguration<Caucao>
    {
        public void Configure(EntityTypeBuilder<Caucao> builder)
        {
            builder.ToTable("tb_caucao");

            builder.HasKey(c => c.IdCaucao);
            builder.Property(c => c.IdCaucao)
                   .HasColumnName("id_caucao");

            builder.Property(c => c.Valor)
                   .HasColumnName("valor")
                   .HasPrecision(10, 2)
                   .IsRequired();

            // RN-30: quanto o fechamento consumiu. `Valor` passou a ser o depositado e não muda mais —
        // descontar dele apagava a resposta para "eu deixei quanto?"
        builder.Property(c => c.ValorConsumido)
            .HasColumnName("valor_consumido")
            .HasPrecision(10, 2)
            .HasDefaultValue(0m)
            .IsRequired();

        builder.Property(c => c.Status)
                   .HasColumnName("status")
                   .HasConversion<string>()
                   .HasMaxLength(20)
                   .IsRequired();

            //chave estrangeira
            builder.HasOne<Locacao>()
                   .WithMany(l => l.Caucoes)
                   .HasForeignKey("id_locacao")  // FK SOMBRA
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }


}
