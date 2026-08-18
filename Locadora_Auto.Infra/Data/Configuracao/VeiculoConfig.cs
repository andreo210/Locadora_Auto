using Locadora_Auto.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Locadora_Auto.Infra.Data.Configuracao
{
    public class VeiculoConfig : IEntityTypeConfiguration<Veiculo>
    {
        /// <summary>
        /// Predicado do índice parcial da RN-55. SQL bruto porque <c>HasFilter</c> não passa pelo
        /// modelo: o nome que vai aqui é o da <b>coluna</b> (<c>ativo</c>), não o da propriedade.
        /// </summary>
        private const string FiltroDeAtivos = "ativo";

        public void Configure(EntityTypeBuilder<Veiculo> builder)
        {
            builder.ToTable("tb_veiculo");

            builder.HasKey(e => e.IdVeiculo);

            builder.Property(e => e.IdVeiculo)
                .HasColumnName("id_veiculo");

            builder.Property(e => e.Placa)
                .HasColumnName("placa")
                .HasMaxLength(10)
                .IsRequired();

            // RN-55: a unicidade vale entre os **ativos**, não na tabela inteira. Índice global
            // impediria recadastrar a placa de um carro que saiu da frota — e placa é reemitida,
            // chassi de sinistrado sai do cadastro. O que a regra protege é a conciliação de multa
            // e de sinistro, que só olha carro ativo.
            builder.HasIndex(e => e.Placa)
                .IsUnique()
                .HasFilter(FiltroDeAtivos);

            builder.Property(e => e.Chassi)
                .HasColumnName("chassi")
                .HasMaxLength(30)
                .IsRequired();

            builder.HasIndex(e => e.Chassi)
                .IsUnique()
                .HasFilter(FiltroDeAtivos);

            builder.Property(e => e.KmAtual)
                .HasColumnName("km_atual")
                .IsRequired();

            builder.Property(e => e.Ativo)
                .HasColumnName("ativo")
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(e => e.Marca)
                .HasColumnName("marca");

            builder.Property(e => e.Modelo)
                .HasColumnName("modelo");

            builder.Property(e => e.Ano)
                .HasColumnName("ano");

            builder.Property(c => c.Status)
                  .HasColumnName("status")
                  .HasConversion<int>()
                  .IsRequired();

            // RN-56: por que, quando e por quem o ativo deixou a frota. Nulos enquanto ele for
            // frota — o que preenche os três é a desmobilização, que acontece uma vez só.
            builder.Property(e => e.MotivoDesmobilizacao)
                .HasColumnName("motivo_desmobilizacao")
                .HasMaxLength(500);

            builder.Property(e => e.DataDesmobilizacao)
                .HasColumnName("data_desmobilizacao");

            //chave estrangeira
            builder.Property(e => e.IdFuncionarioDesmobilizacao)
                .HasColumnName("id_funcionario_desmobilizacao");

            //sem navegação: a pergunta "quem baixou este carro" é de auditoria e se responde pelo
            //id, e uma navegação a mais no Veiculo pesaria em toda listagem de frota
            builder.HasOne<Funcionario>()
                .WithMany()
                .HasForeignKey(e => e.IdFuncionarioDesmobilizacao)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Property(e => e.IdCategoria)
                .HasColumnName("id_categoria")
                .IsRequired();

            builder.Property(e => e.FilialAtualId)
                .HasColumnName("id_filial_atual")
                .IsRequired();


            builder.HasOne(e => e.Categoria)//uma categoria tem muitos veículos
                .WithMany(v=>v.Veiculos)
                .HasForeignKey(e => e.IdCategoria)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(e => e.FilialAtual)
                .WithMany(v => v.Veiculos)
                .HasForeignKey(e => e.FilialAtualId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

}
