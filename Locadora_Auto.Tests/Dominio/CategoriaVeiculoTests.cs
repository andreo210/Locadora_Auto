using Locadora_Auto.Domain.Entidades;
using Locadora_Auto.Tests.Fabricas;
using Xunit;

namespace Locadora_Auto.Tests.Dominio
{
    /// <summary>
    /// RN-08: quilometragem livre é <c>LimiteKm</c> nulo. As colunas sempre foram anuláveis, mas a
    /// guarda exigia número — então o plano mais vendido no varejo era o único que o cadastro não
    /// conseguia expressar, e quem omitisse o campo pegava um 500 no serviço.
    /// </summary>
    public class CategoriaVeiculoTests
    {
        [Fact]
        public void Categoria_de_quilometragem_livre_e_valida()
        {
            var categoria = Fabrica.Categoria(limiteKm: null, valorKmExcedente: null);

            Assert.Null(categoria.LimiteKm);
            Assert.Null(categoria.ValorKmExcedente);
        }

        [Fact]
        public void Sem_limite_o_valor_do_km_excedente_e_descartado()
        {
            // preço de km excedente sem franquia é preço que nada aciona, e guardá-lo faria a
            // categoria parecer controlada num filtro por "tem valor de km"
            var categoria = Fabrica.Categoria(limiteKm: null, valorKmExcedente: 1.20m);

            Assert.Null(categoria.ValorKmExcedente);
        }

        [Fact]
        public void Categoria_com_limite_exige_o_valor_do_km_excedente()
        {
            // doc 07 §4: é o "cadastro inconsistente" que bloqueia o fechamento — e descobrir isso
            // na devolução, com o cliente esperando, é tarde demais
            Assert.Throws<InvalidOperationException>(
                () => Fabrica.Categoria(limiteKm: 200, valorKmExcedente: null));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Limite_de_km_nao_positivo_e_recusado(int limiteKm)
        {
            Assert.Throws<InvalidOperationException>(
                () => Fabrica.Categoria(limiteKm: limiteKm, valorKmExcedente: 1.20m));
        }

        [Fact]
        public void Atualizar_pode_mudar_a_categoria_para_quilometragem_livre()
        {
            var categoria = Fabrica.Categoria(limiteKm: 200, valorKmExcedente: 1.20m);

            categoria.Atualizar("Hatch", 150m, limiteKm: null, valorKmExcedente: null);

            Assert.Null(categoria.LimiteKm);
            Assert.Null(categoria.ValorKmExcedente);
        }
    }
}
