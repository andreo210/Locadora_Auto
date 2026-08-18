using Locadora_Auto.Domain.Entidades;
using Locadora_Auto.Infra.Data;
using Locadora_Auto.Infra.Data.CurrentUsers;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Locadora_Auto.Tests.Infra
{
    /// <summary>
    /// RN-32: apurar duas vezes não pode produzir duas contas. A idempotência de
    /// <c>Locacao.AbrirFechamento</c> é a recusa amigável; a <b>garantia</b> é o índice único sobre
    /// <c>id_locacao</c> em <c>tb_fechamento_locacao</c>, e é ele que segura o caso que a
    /// idempotência não vê: dois pedidos concorrentes, cada um com sua instância da locação.
    ///
    /// Isso não dá para verificar no <c>RepositorioFake</c> — lá não há índice nenhum. O que se
    /// fixa aqui é o índice chegar ao modelo do EF: se alguém trocar o 1:1 por um 1:N, a garantia
    /// se desliga em silêncio e o contrato passa a aceitar duas contas divergentes.
    /// </summary>
    public class ContaUnicaPorContratoTests
    {
        private sealed class UsuarioFake : ICurrentUser
        {
            public string? UserId => "teste";
            public bool IsAuthenticated => true;
        }

        private static LocadoraDbContext MontarContexto()
        {
            var opcoes = new DbContextOptionsBuilder<LocadoraDbContext>()
                // string de conexão nunca usada: ler o modelo não abre conexão
                .UseNpgsql("Host=localhost;Database=modelo_para_teste;Username=postgres;Password=postgres")
                .UseSnakeCaseNamingConvention()
                .Options;

            return new LocadoraDbContext(opcoes, new UsuarioFake());
        }

        [Fact]
        public void Fechamento_tem_indice_unico_sobre_a_locacao()
        {
            using var contexto = MontarContexto();

            var indice = contexto.Model
                .FindEntityType(typeof(FechamentoLocacao))!
                .GetIndexes()
                .Single(i => i.Properties.Count == 1
                          && i.Properties[0].Name == nameof(FechamentoLocacao.IdLocacao));

            Assert.True(indice.IsUnique);
        }

        [Fact]
        public void Linha_do_fechamento_cascateia_e_a_conta_nao_apaga_o_contrato()
        {
            using var contexto = MontarContexto();

            // a linha não existe sem a conta: apagar o fechamento leva as linhas junto
            var doFechamento = contexto.Model
                .FindEntityType(typeof(LinhaFechamento))!
                .GetForeignKeys()
                .Single(fk => fk.PrincipalEntityType.ClrType == typeof(FechamentoLocacao));

            Assert.Equal(DeleteBehavior.Cascade, doFechamento.DeleteBehavior);

            // já o funcionário que isentou continua respondendo pela isenção depois de sair da
            // empresa (RN-34) — apagá-lo não pode levar a linha embora
            var doFuncionario = contexto.Model
                .FindEntityType(typeof(LinhaFechamento))!
                .GetForeignKeys()
                .Single(fk => fk.PrincipalEntityType.ClrType == typeof(Funcionario));

            Assert.Equal(DeleteBehavior.Restrict, doFuncionario.DeleteBehavior);
        }

        [Fact]
        public void Natureza_da_linha_nao_vira_coluna()
        {
            // é derivada do tipo de propósito: guardá-la criaria um segundo lugar onde o sinal da
            // linha pode divergir do que ela cobra, e um extrato com sinal divergente não se defende
            using var contexto = MontarContexto();

            var propriedade = contexto.Model
                .FindEntityType(typeof(LinhaFechamento))!
                .FindProperty(nameof(LinhaFechamento.Natureza));

            Assert.Null(propriedade);
        }
    }
}
