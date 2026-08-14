
using Locadora_Auto.Application.Configuration.Ultils.NotificadorServices;
using Locadora_Auto.Application.Models.Dto;
using Locadora_Auto.Application.Services.AdicionaisServices;
using Locadora_Auto.Domain.Entidades;
using Locadora_Auto.Tests.Fabricas;
using Locadora_Auto.Tests.Fakes;
using Xunit;

namespace Locadora_Auto.Tests.Servicos
{
    /// <summary>
    /// O contrato do serviço é: falha de regra de negócio não lança exceção, ela notifica.
    /// Como o notificador é a mesma instância que o controller consulta, verificar
    /// <c>TemNotificacao()</c> aqui é exatamente o que decide entre 2xx e ProblemDetails na Api.
    /// </summary>
    public class AdicionalServiceTests
    {
        private static (AdicionalService service, AdicionalRepositoryFake repositorio, NotificadorService notificador)
            Montar(params Adicional[] jaCadastrados)
        {
            var armazem = new ArmazemFake().Semear(jaCadastrados);
            var repositorio = new AdicionalRepositoryFake(armazem);
            var notificador = new NotificadorService();

            return (new AdicionalService(repositorio, notificador), repositorio, notificador);
        }

        [Fact]
        public async Task Criar_com_nome_repetido_notifica_e_nao_grava()
        {
            var (service, repositorio, notificador) = Montar(Fabrica.Adicional("Cadeirinha"));

            var resultado = await service.CriarAsync(new CriarAtualizarAdicionalDto
            {
                Nome = "Cadeirinha",
                ValorDiaria = 30m
            });

            Assert.Null(resultado);
            Assert.True(notificador.TemNotificacao());
            Assert.Equal(0, repositorio.Salvamentos);
        }

        [Fact]
        public async Task Criar_com_valor_negativo_notifica_em_vez_de_lancar()
        {
            var (service, repositorio, notificador) = Montar();

            var resultado = await service.CriarAsync(new CriarAtualizarAdicionalDto
            {
                Nome = "GPS",
                ValorDiaria = -10m
            });

            Assert.Null(resultado);
            Assert.Contains(notificador.ObterNotificacoes(), n => n.Mensagem.Contains("Valor", StringComparison.OrdinalIgnoreCase));
            Assert.Equal(0, repositorio.Salvamentos);
        }

        [Fact]
        public async Task Criar_valido_grava_e_devolve_o_dto()
        {
            var (service, repositorio, notificador) = Montar();

            var resultado = await service.CriarAsync(new CriarAtualizarAdicionalDto
            {
                Nome = "GPS",
                ValorDiaria = 15m
            });

            Assert.False(notificador.TemNotificacao());
            Assert.NotNull(resultado);
            Assert.Equal("GPS", resultado!.Nome);
            Assert.True(resultado.Ativo);
            Assert.Equal(1, repositorio.Salvamentos);
        }

        [Fact]
        public async Task Atualizar_id_inexistente_notifica()
        {
            var (service, repositorio, notificador) = Montar();

            var sucesso = await service.AtualizarAsync(99, new CriarAtualizarAdicionalDto
            {
                Nome = "GPS",
                ValorDiaria = 15m
            });

            Assert.False(sucesso);
            Assert.True(notificador.TemNotificacao());
            Assert.Equal(0, repositorio.Salvamentos);
        }

        [Fact]
        public async Task Atualizar_troca_os_dados_e_grava()
        {
            var adicional = Fabrica.Adicional("Cadeirinha", 25m);
            var (service, repositorio, notificador) = Montar(adicional);

            var sucesso = await service.AtualizarAsync(adicional.IdAdicional, new CriarAtualizarAdicionalDto
            {
                Nome = "Cadeirinha infantil",
                ValorDiaria = 32m
            });

            Assert.True(sucesso);
            Assert.False(notificador.TemNotificacao());
            Assert.Equal("Cadeirinha infantil", adicional.Nome);
            Assert.Equal(32m, adicional.ValorDiaria);
            Assert.Equal(1, repositorio.Salvamentos);
        }

        [Fact]
        public async Task Ativar_adicional_que_ja_esta_ativo_notifica()
        {
            var adicional = Fabrica.Adicional();
            var (service, repositorio, notificador) = Montar(adicional);

            var sucesso = await service.AtivarAsync(adicional.IdAdicional);

            Assert.False(sucesso);
            Assert.True(notificador.TemNotificacao());
            Assert.Equal(0, repositorio.Salvamentos);
        }

        [Fact]
        public async Task Desativar_marca_como_inativo_e_grava()
        {
            var adicional = Fabrica.Adicional();
            var (service, repositorio, notificador) = Montar(adicional);

            var sucesso = await service.DesativarAsync(adicional.IdAdicional);

            Assert.True(sucesso);
            Assert.False(notificador.TemNotificacao());
            Assert.False(adicional.Ativo);
            Assert.Equal(1, repositorio.Salvamentos);
        }

        [Fact]
        public async Task Desativar_duas_vezes_notifica_na_segunda()
        {
            var adicional = Fabrica.Adicional();
            var (service, _, notificador) = Montar(adicional);

            await service.DesativarAsync(adicional.IdAdicional);
            var segunda = await service.DesativarAsync(adicional.IdAdicional);

            Assert.False(segunda);
            Assert.True(notificador.TemNotificacao());
        }

        [Fact]
        public async Task Obter_ativos_traz_apenas_os_ofertados()
        {
            var ativo = Fabrica.Adicional("GPS");
            var inativo = Fabrica.Adicional("Cadeirinha");
            inativo.Desativar();

            var (service, _, _) = Montar(ativo, inativo);

            var resultado = await service.ObterSeguroAtivoAsync();

            Assert.Single(resultado);
            Assert.Equal("GPS", resultado[0].Nome);
        }
    }
}
