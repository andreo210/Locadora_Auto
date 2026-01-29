using Locadora_Auto.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Locadora_Auto.Infra.Data.Configuracao
{
    public class FuncionarioConfig : IEntityTypeConfiguration<Funcionario>
    {
        public void Configure(EntityTypeBuilder<Funcionario> builder)
        {
            builder.ToTable("tbFuncionario");

            builder.HasKey(e => e.IdFuncionario);

            builder.Property(e => e.IdFuncionario)
                .HasColumnName("id_funcionario");

            builder.Property(e => e.Matricula)
                .HasColumnName("matricula")
                .HasMaxLength(20)
                .IsRequired();

            builder.HasIndex(e => e.Matricula)
                .IsUnique();

            builder.Property(e => e.Cargo)
                .HasColumnName("cargo")
                .HasMaxLength(50);

            builder.Property(e => e.Ativo)
                .HasColumnName("status")
                .IsRequired();
            //chave estrangeira
            builder.Property(e => e.IdUser)
                .HasColumnName("id_user");

            builder.HasOne(u => u.Usuario)//o funcionario é usuário
               .WithOne(f => f.Funcionario)//o usuario pode ser um fucionario
               .HasForeignKey<Funcionario>(c => c.IdUser)
               .OnDelete(DeleteBehavior.Cascade);//ao deletar o funcionario, deletar o usuário
        }
    }

}
