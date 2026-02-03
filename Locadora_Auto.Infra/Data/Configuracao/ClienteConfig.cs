using Locadora_Auto.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Locadora_Auto.Infra.Data.Configuracao
{
    public class ClienteConfig : IEntityTypeConfiguration<Clientes>
    {
        public void Configure(EntityTypeBuilder<Clientes> builder)
        {
            builder.ToTable("tbCliente");

            builder.HasKey(e => e.IdCliente);

            builder.Property(e => e.IdCliente)
                .HasColumnName("id_cliente")
                .ValueGeneratedOnAdd();


            builder.Property(e => e.Status)
                  .HasColumnName("status")
                  .HasConversion<string>() 
                  .HasMaxLength(20)
                  .IsRequired();

            builder.Property(e => e.NumeroHabilitacao)
               .HasColumnName("numero_habilitacao");

            builder.Property(e => e.ValidadeHabilitacao)
                .HasColumnName("validade_habilitacao");

            builder.Property(e => e.TotalLocacoes)
               .HasColumnName("total_locacoes");

            builder.Property(e => e.IdUsuarioCriacao)
                .HasColumnName("id_usuario_criacao");

            builder.Property(e => e.IdUsuarioModificacao)
                .HasColumnName("id_usuario_modificacao");

            builder.Property(e => e.DataCriacao)
                .HasColumnName("data_criacao");

            builder.Property(e => e.DataModificacao)
                .HasColumnName("data_modificacao");

            //chave estrangeira
            builder.Property(e => e.IdUser)
                .HasColumnName("idAspNetUsers");


            #region relacionamento
            builder.HasOne(p => p.Endereco)//uma cliente tem um endereço
                   .WithOne(c => c.Cliente)//um endereço tem um clientea
                   .HasForeignKey<Endereco>(c => c.IdCliente)
                   .OnDelete(DeleteBehavior.Cascade);//ao deletar o cliente, deletar o endereço

            builder.HasOne(u => u.Usuario)//o cliente é usuário
                .WithOne(f => f.Cliente)//o usuario pode ser um cliente
                .HasForeignKey<Clientes>(c =>c.IdUser)
                .OnDelete(DeleteBehavior.Cascade);


            #endregion relacionamento

        }
    }

}
