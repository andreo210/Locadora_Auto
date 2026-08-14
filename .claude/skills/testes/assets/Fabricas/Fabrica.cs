using {{RootNamespace}}.Tests.Fakes;

namespace {{RootNamespace}}.Tests.Fabricas
{
    /// <summary>
    /// Entidades válidas para os testes, com valores padrão que passam nas validações de
    /// <c>Criar</c>. O teste só informa o que é relevante para o caso que está verificando —
    /// assim, quando uma validação nova entra na entidade, corrige-se um lugar só.
    ///
    /// Três regras ao escrever um método aqui:
    ///
    /// 1. O padrão tem que ser válido. Se <c>Fabrica.Cliente()</c> devolve algo que o serviço
    ///    recusa, todo teste de caminho feliz vira arqueologia.
    /// 2. Parâmetro opcional só para o que os testes precisam variar — normalmente o que
    ///    aparece nas regras de negócio (<c>ativo</c>, uma data, um valor de limite).
    /// 3. Filho de agregado nasce pela raiz. Se <c>Reserva.Criar</c> é internal, a fábrica chama
    ///    <c>cliente.ReservarVeiculo(...)</c> e devolve as duas pontas — testar pela porta que a
    ///    aplicação usa é o que mantém a invariante honesta.
    /// </summary>
    public static class Fabrica
    {
        /// <summary>Data segura para "futuro": as entidades validam contra <c>DateTime.UtcNow</c>.</summary>
        public static DateTime DaquiADias(int dias) => DateTime.UtcNow.AddDays(dias);

        /// <summary>Data segura para "passado". Nunca use literal: ele envelhece e o teste quebra sozinho.</summary>
        public static DateTime DiasAtras(int dias) => DateTime.UtcNow.AddDays(-dias);

        /// <summary>
        /// Escreve na chave primária mesmo com set privado. Em teste isso é necessário porque o id
        /// normalmente viria do banco, e sem ele todo filtro por id casaria com a entidade errada.
        /// Só é preciso chamar quando o teste precisa de um id específico — <c>ArmazemFake.Semear</c>
        /// já atribui id a quem entra sem nenhum.
        /// </summary>
        public static void DefinirId(object entidade, int id) => ChavePrimaria.Definir(entidade, id);

        // ---------------------------------------------------------------------------------
        // Daqui para baixo é por projeto. O formato é sempre este:
        //
        // public static Cliente Cliente(bool ativo = true)
        // {
        //     var cliente = Entidades.Cliente.Criar(
        //         nome: "Cliente de teste",
        //         documento: "12345678900",
        //         endereco: Endereco());
        //
        //     if (!ativo) cliente.Desativar();
        //
        //     return cliente;
        // }
        //
        // E, para agregado, devolvendo raiz e filho:
        //
        // public static (Cliente cliente, Pedido pedido) ClienteComPedido(int idPedido = 1)
        // {
        //     var cliente = Cliente();
        //     DefinirId(cliente, 1);
        //
        //     cliente.AbrirPedido(DaquiADias(3));
        //
        //     var pedido = cliente.Pedidos.Single();
        //     DefinirId(pedido, idPedido);
        //
        //     return (cliente, pedido);
        // }
        // ---------------------------------------------------------------------------------
    }
}
