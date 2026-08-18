using Locadora_Auto.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Locadora_Auto.Infra.Data.Configuracao
{
    public class FilialConfig : IEntityTypeConfiguration<Filial>
    {
        public void Configure(EntityTypeBuilder<Filial> builder)
        {
            builder.ToTable("tb_filial");

            builder.HasKey(e => e.IdFilial);

            builder.Property(e => e.IdFilial)
                .HasColumnName("id_filial");

            builder.Property(e => e.Nome)
                .HasColumnName("nome")
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(e => e.Cidade)
                .HasColumnName("cidade")
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(e => e.Ativo)
                .HasColumnName("ativo");

            // RN-45/RN-46: minutos entre a devolução e a volta do carro à oferta. O default vale
            // para as filiais que já existiam quando a coluna entrou.
            builder.Property(e => e.TempoPreparacaoMinutos)
                .HasColumnName("tempo_preparacao_minutos")
                .HasDefaultValue(Filial.PreparacaoPadraoMinutos)
                .IsRequired();

            // RN-49: a filial participa do remanejamento programado de frota. O default true vale
            // para as filiais que já existiam quando a coluna entrou — obrigá-las a se habilitar
            // deixaria a rede inteira sem transferência até alguém perceber.
            builder.Property(e => e.PermiteTransferencia)
                .HasColumnName("permite_transferencia")
                .HasDefaultValue(true)
                .IsRequired();

            //chave estrangeira
            builder.Property(e => e.IdEndereco)
                .HasColumnName("id_endereco")
                .IsRequired();

            builder.HasOne(u => u.Endereco)//uma filial tem um endereço
                .WithOne(f => f.Filial)//um endereço tem uma filial
                .HasForeignKey<Filial>(f => f.IdEndereco)
                .OnDelete(DeleteBehavior.Cascade);//ao deletar a filial, deletar o endereço

        }
    }

}
