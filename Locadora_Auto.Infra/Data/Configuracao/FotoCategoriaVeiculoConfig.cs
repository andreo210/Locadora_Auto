using Locadora_Auto.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Locadora_Auto.Infra.Data.Configuracao
{
    public class FotoCategoriaVeiculoConfig : IEntityTypeConfiguration<FotoCategoriaVeiculo>
    {
        public void Configure(EntityTypeBuilder<FotoCategoriaVeiculo> builder)
        {
            builder.ToTable("tbFotoCategoriaVeiculo");

            builder.HasKey(e => e.IdFoto);

            builder.Property(e => e.IdFoto)
                .HasColumnName("id_foto");

            builder.Property(e => e.NomeArquivo)
                .HasColumnName("nome_arquivo");

            builder.Property(e => e.Raiz)
               .HasColumnName("raiz");

            builder.Property(e => e.QuantidadeBytes)
              .HasColumnName("quantidadeBytes");

            builder.Property(e => e.DataUpload)
               .HasColumnName("data_upload");

            builder.Property(e => e.Diretorio)
               .HasColumnName("diretorio");

            builder.Property(e => e.Extensao)
               .HasColumnName("extensao");

            //chave estrangeira
            builder.HasOne<CategoriaVeiculo>()
                   .WithMany(l => l.Fotos)
                   .HasForeignKey("id_categoria_veiculo")// fk fantasma, não existe na classe, mas é necessário para o relacionamento
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
