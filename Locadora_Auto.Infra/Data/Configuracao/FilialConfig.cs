using Locadora_Auto.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Locadora_Auto.Infra.Data.Configuracao
{
    public class FilialConfig : IEntityTypeConfiguration<Filial>
    {
        public void Configure(EntityTypeBuilder<Filial> builder)
        {
            builder.ToTable("tbFilial");

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

            //chave estrangeira
            builder.Property(e => e.IdEndereco)
                .HasColumnName("idEndereco")
                .IsRequired();

            builder.HasOne(u => u.Endereco)//uma filial tem um endereço
                .WithOne(f => f.Filial)//um endereço tem uma filial
                .HasForeignKey<Filial>(f => f.IdEndereco)
                .OnDelete(DeleteBehavior.Cascade);//ao deletar a filial, deletar o endereço

        }
    }

}
