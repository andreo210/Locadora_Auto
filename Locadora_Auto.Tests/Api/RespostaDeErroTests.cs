using Locadora_Auto.Application.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Net;
using Xunit;

namespace Locadora_Auto.Tests.Api
{
    /// <summary>
    /// Toda resposta de erro sai como ProblemDetails (RFC 7807). O que estes testes fixam é a
    /// tradução de exceção em status: o cliente precisa distinguir "tente de novo" (409) de
    /// "deu ruim no servidor" (500).
    /// </summary>
    public class RespostaDeErroTests
    {
        private static HttpContext Contexto()
        {
            var contexto = new DefaultHttpContext();
            contexto.Request.Path = "/api/v1/veiculos/7";
            return contexto;
        }

        [Fact]
        public void Conflito_de_concorrencia_vira_409_e_nao_500()
        {
            var problem = ExceptionProblemFactory.Create(
                Contexto(),
                new DbUpdateConcurrencyException("A linha foi alterada por outra transação"));

            Assert.Equal((int)HttpStatusCode.Conflict, problem.Status);
            Assert.Equal("Conflito de concorrência", problem.Title);
            Assert.Contains("outro usuário", problem.Detail);
        }

        [Fact]
        public void Conflito_de_concorrencia_nao_vaza_a_mensagem_interna_do_ef()
        {
            var problem = ExceptionProblemFactory.Create(
                Contexto(),
                new DbUpdateConcurrencyException("Database operation expected to affect 1 row(s) but actually affected 0"));

            Assert.DoesNotContain("row(s)", problem.Detail);
        }

        /// <summary>
        /// Violação de constraint EXCLUDE. Usa a exceção real do driver — e não um dublê — porque o
        /// que está em teste é justamente reconhecer o que o Npgsql entrega em produção.
        /// </summary>
        private static PostgresException Sobreposicao(string sqlState = "23P01")
            => new("conflicting key value violates exclusion constraint \"ex_locacao_sem_sobreposicao\"",
                   "ERROR", "ERROR", sqlState);

        [Fact]
        public void Sobreposicao_de_contrato_vira_409_e_nao_500()
        {
            // o EF embrulha a exceção do driver: quem chega ao middleware é o DbUpdateException
            var problem = ExceptionProblemFactory.Create(
                Contexto(),
                new DbUpdateException("erro ao salvar", Sobreposicao()));

            Assert.Equal((int)HttpStatusCode.Conflict, problem.Status);
            Assert.Equal("Conflito de agenda do veículo", problem.Title);
        }

        [Fact]
        public void Sobreposicao_de_contrato_nao_vaza_o_nome_da_constraint()
        {
            var problem = ExceptionProblemFactory.Create(
                Contexto(),
                new DbUpdateException("erro ao salvar", Sobreposicao()));

            Assert.DoesNotContain("exclusion constraint", problem.Detail);
            Assert.Contains("período", problem.Detail);
        }

        [Fact]
        public void Outra_violacao_do_postgres_nao_vira_conflito_de_agenda()
        {
            // 23505 é chave duplicada: erro diferente, mensagem diferente — não pode cair aqui
            var problem = ExceptionProblemFactory.Create(
                Contexto(),
                new DbUpdateException("erro ao salvar", Sobreposicao("23505")));

            Assert.NotEqual((int)HttpStatusCode.Conflict, problem.Status);
        }

        [Fact]
        public void Excecao_inesperada_continua_500()
        {
            var problem = ExceptionProblemFactory.Create(Contexto(), new InvalidOperationException("qualquer coisa"));

            Assert.Equal((int)HttpStatusCode.InternalServerError, problem.Status);
        }

        [Fact]
        public void ProblemException_preserva_o_proprio_status()
        {
            var problem = ExceptionProblemFactory.Create(
                Contexto(),
                new ProblemException(ProblemFactory.Create(HttpStatusCode.Forbidden, "sem permissão")));

            Assert.Equal((int)HttpStatusCode.Forbidden, problem.Status);
        }

        [Fact]
        public void Toda_resposta_de_erro_carrega_caminho_e_traceId()
        {
            var problem = ExceptionProblemFactory.Create(Contexto(), new DbUpdateConcurrencyException());

            Assert.Equal("/api/v1/veiculos/7", problem.Instance);
            Assert.True(problem.Extensions.ContainsKey("traceId"));
        }

        [Fact]
        public void ProblemFactory_recusa_status_de_sucesso()
        {
            // ProblemDetails só existe para erro: 200 aqui seria bug de quem chamou
            Assert.Throws<ArgumentOutOfRangeException>(() => ProblemFactory.Create(HttpStatusCode.OK, "ok"));
        }
    }
}
