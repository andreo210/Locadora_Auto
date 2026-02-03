namespace Locadora_Auto.Application.Models.Dto
{
    public abstract class EnderecoBase
    {
        public string Logradouro { get; set; } = null!;
        public string Numero { get; set; } = null!;
        public string? Complemento { get; set; }
        public string Bairro { get; set; } = null!;
        public string Cidade { get; set; } = null!;
        public string Estado { get; set; } = null!;
        public string Cep { get; set; } = null!;
    }

    public class EnderecoDto : EnderecoBase
    {
    }

    public class CriarEnderecoDto : EnderecoBase
    {
    
    }

    public class AtualizarEnderecoDto : EnderecoBase
    {

    }

}
