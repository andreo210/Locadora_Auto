using Locadora_Auto.Application.Models.Consultas;
using Locadora_Auto.Application.Models.Dto;
using Locadora_Auto.Application.Models.Mappers;
using Locadora_Auto.Domain;
using Locadora_Auto.Domain.Entidades;
using Locadora_Auto.Tests.Fabricas;
using Xunit;

namespace Locadora_Auto.Tests.Consultas
{
    public class ConsultaPaginadaRequestTests
    {
        [Theory]
        [InlineData(0, 1)]
        [InlineData(-5, 1)]
        [InlineData(1, 1)]
        [InlineData(7, 7)]
        public void Pagina_nunca_fica_abaixo_de_um(int informado, int esperado)
        {
            // página zero ou negativa quebraria o Skip do repositório
            var consulta = new ConsultaPaginadaRequest { Pagina = informado };

            Assert.Equal(esperado, consulta.Pagina);
        }

        [Theory]
        [InlineData(0, 1)]
        [InlineData(-1, 1)]
        [InlineData(25, 25)]
        [InlineData(100_000, ConsultaPaginadaRequest.MaximoItensPorPagina)]
        public void ItensPorPagina_fica_dentro_do_limite(int informado, int esperado)
        {
            // sem o teto, "?itensPorPagina=100000" viraria varredura de tabela pela barra de endereço
            var consulta = new ConsultaPaginadaRequest { ItensPorPagina = informado };

            Assert.Equal(esperado, consulta.ItensPorPagina);
        }

        [Fact]
        public void Valores_padrao_sao_primeira_pagina_com_dez_itens()
        {
            var consulta = new ConsultaPaginadaRequest();

            Assert.Equal(1, consulta.Pagina);
            Assert.Equal(10, consulta.ItensPorPagina);
        }

        [Theory]
        [InlineData("  Fiat ARGO  ", "fiat argo")]
        [InlineData("ABC", "abc")]
        public void Termo_normalizado_vem_aparado_e_em_minusculas(string informado, string esperado)
        {
            // o LIKE do Postgres é sensível a maiúsculas: a busca compara em minúsculas dos dois lados
            var consulta = new ConsultaPaginadaRequest { Termo = informado };

            Assert.Equal(esperado, consulta.TermoNormalizado);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Termo_vazio_vira_nulo_para_o_filtro_ser_ignorado(string? informado)
        {
            var consulta = new ConsultaPaginadaRequest { Termo = informado };

            Assert.Null(consulta.TermoNormalizado);
        }
    }

    public class OrdenacaoDeConsultaTests
    {
        private static readonly OrdenacaoDeConsulta<Adicional> Ordenacoes =
            OrdenacaoDeConsulta<Adicional>.Padrao(a => a.Nome)
                .Com("nome", a => a.Nome)
                .Com("valordiaria", a => a.ValorDiaria);

        private static IQueryable<Adicional> Dados() => new[]
        {
            Fabrica.Adicional("GPS", 30m),
            Fabrica.Adicional("Cadeirinha", 10m),
            Fabrica.Adicional("Wi-Fi", 20m)
        }.AsQueryable();

        [Fact]
        public void Coluna_conhecida_ordena_ascendente_por_padrao()
        {
            var ordenar = Ordenacoes.Montar("valordiaria", direcao: null);

            var resultado = ordenar(Dados()).ToList();

            Assert.Equal(new[] { 10m, 20m, 30m }, resultado.Select(a => a.ValorDiaria));
        }

        [Fact]
        public void Direcao_desc_inverte_a_ordem()
        {
            var ordenar = Ordenacoes.Montar("valordiaria", "desc");

            var resultado = ordenar(Dados()).ToList();

            Assert.Equal(new[] { 30m, 20m, 10m }, resultado.Select(a => a.ValorDiaria));
        }

        [Fact]
        public void Nome_de_coluna_nao_diferencia_maiusculas()
        {
            var ordenar = Ordenacoes.Montar("ValorDiaria", "desc");

            Assert.Equal(30m, ordenar(Dados()).First().ValorDiaria);
        }

        [Fact]
        public void Coluna_desconhecida_cai_no_padrao()
        {
            // quem editou a URL na mão não deve receber erro, e sim a ordenação padrão da tela
            var ordenar = Ordenacoes.Montar("coluna_que_nao_existe", direcao: null);

            var resultado = ordenar(Dados()).ToList();

            Assert.Equal(new[] { "Cadeirinha", "GPS", "Wi-Fi" }, resultado.Select(a => a.Nome));
        }

        [Fact]
        public void Sem_coluna_informada_usa_o_padrao()
        {
            var ordenar = Ordenacoes.Montar(new ConsultaPaginadaRequest());

            Assert.Equal("Cadeirinha", ordenar(Dados()).First().Nome);
        }

        [Fact]
        public void Padrao_pode_nascer_descendente()
        {
            var decrescente = OrdenacaoDeConsulta<Adicional>.Padrao(a => a.ValorDiaria, descendente: true);

            var resultado = decrescente.Montar(new ConsultaPaginadaRequest())(Dados()).ToList();

            Assert.Equal(30m, resultado.First().ValorDiaria);
        }

        [Fact]
        public void Direcao_informada_vence_o_padrao_descendente()
        {
            var decrescente = OrdenacaoDeConsulta<Adicional>.Padrao(a => a.ValorDiaria, descendente: true);

            var resultado = decrescente.Montar(coluna: null, direcao: "asc")(Dados()).ToList();

            Assert.Equal(10m, resultado.First().ValorDiaria);
        }
    }

    public class PaginatedResultMapperTests
    {
        [Fact]
        public void ParaDto_troca_os_itens_e_preserva_os_metadados()
        {
            var pagina = new PaginatedResult<Adicional>
            {
                Items = new[] { Fabrica.Adicional("GPS", 30m) },
                Total = 42,
                Pagina = 3,
                TotalPaginas = 5,
                ItensPorPagina = 10
            };

            var resultado = pagina.ParaDto(AdicionalMapper.ToDtoList);

            Assert.IsType<AdicionalDto>(Assert.Single(resultado.Items));
            Assert.Equal("GPS", resultado.Items[0].Nome);
            Assert.Equal(42, resultado.Total);
            Assert.Equal(3, resultado.Pagina);
            Assert.Equal(5, resultado.TotalPaginas);
            Assert.Equal(10, resultado.ItensPorPagina);
        }
    }
}
