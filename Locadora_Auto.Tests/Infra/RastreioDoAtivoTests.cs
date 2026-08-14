using Locadora_Auto.Domain.Entidades;
using Locadora_Auto.Infra.Data;
using Locadora_Auto.Infra.Data.CurrentUsers;
using Locadora_Auto.Tests.Fabricas;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Locadora_Auto.Tests.Infra
{
    /// <summary>
    /// O change tracker do EF funciona sem abrir conexão, então dá para verificar sem banco o que
    /// acontece com o veículo quando uma locação nova é inserida. Isto é o que o
    /// <c>RepositorioFake</c> não consegue provar: em memória as entidades são a mesma instância
    /// que o teste criou, então lá a mudança de status "persiste" mesmo sem rastreio nenhum.
    ///
    /// A regra sob teste: <c>Locacao.Criar</c> chama <c>veiculo.Locar()</c>, e essa mudança só vira
    /// UPDATE se o veículo tiver sido carregado com <c>rastreado: true</c>.
    /// </summary>
    public class RastreioDoAtivoTests
    {
        private sealed class UsuarioFake : ICurrentUser
        {
            public string? UserId => "teste";
            public bool IsAuthenticated => true;
        }

        private static LocadoraDbContext MontarContexto()
        {
            var opcoes = new DbContextOptionsBuilder<LocadoraDbContext>()
                // string de conexão nunca usada: rastrear entidade não abre conexão
                .UseNpgsql("Host=localhost;Database=modelo_para_teste;Username=postgres;Password=postgres")
                .UseSnakeCaseNamingConvention()
                .Options;

            return new LocadoraDbContext(opcoes, new UsuarioFake());
        }

        /// <summary>
        /// Reproduz o que o repositório devolve: entidades com id, como se viessem do banco.
        /// </summary>
        private static (Clientes cliente, Veiculo veiculo, Funcionario funcionario) Existentes()
        {
            var cliente = Fabrica.Cliente();
            Fabrica.DefinirId(cliente, 1);

            var veiculo = Fabrica.Veiculo();
            Fabrica.DefinirId(veiculo, 1);

            var funcionario = Fabrica.Funcionario();
            Fabrica.DefinirId(funcionario, 1);

            return (cliente, veiculo, funcionario);
        }

        [Fact]
        public void Veiculo_rastreado_vira_update_quando_a_locacao_e_inserida()
        {
            using var contexto = MontarContexto();
            var (cliente, veiculo, funcionario) = Existentes();

            // o que AtualizarSalvarAsync/ObterPorIdAsync(rastreado: true) produzem
            contexto.Attach(veiculo);
            contexto.Attach(cliente);
            contexto.Attach(funcionario);

            var locacao = Fabrica.Locacao(cliente, veiculo, funcionario);
            contexto.Add(locacao);

            var entrada = contexto.Entry(veiculo);

            Assert.Equal(EntityState.Modified, entrada.State);
            Assert.True(entrada.Property(v => v.Status).IsModified);
            Assert.True(entrada.Property(v => v.Disponivel).IsModified);
            Assert.Equal(StatusVeiculo.Locado, veiculo.Status);
        }

        [Fact]
        public void Veiculo_sem_rastreio_seria_inserido_de_novo_em_vez_de_atualizado()
        {
            using var contexto = MontarContexto();
            var (cliente, veiculo, funcionario) = Existentes();

            // sem Attach: é o veículo que ObterPorIdAsync devolve com AsNoTracking
            var locacao = Fabrica.Locacao(cliente, veiculo, funcionario);
            contexto.Add(locacao);

            // Add pinta o grafo inteiro de Added: o veículo já existente viraria um INSERT novo,
            // e a saída da oferta nunca chegaria à linha que está no banco
            Assert.Equal(EntityState.Added, contexto.Entry(veiculo).State);
        }
    }
}
