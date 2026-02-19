namespace Locadora_Auto.Domain.Entidades
{
    public class FotoVistoria : FotoBase
    {
        public static FotoVistoria Criar(string nome,string raiz, string diretorio, string extensao, long quantidadeBytes)
        {

            if (quantidadeBytes <= 0)
                throw new InvalidOperationException("quantidadeBytes não pode ser menor que zero");

            if (string.IsNullOrWhiteSpace(nome))
                throw new InvalidOperationException("nome não pode ser nulo");

            if (string.IsNullOrWhiteSpace(raiz))
                throw new InvalidOperationException("raiz não pode ser nulo");

            if (string.IsNullOrWhiteSpace(diretorio))
                throw new InvalidOperationException("diretorio não pode ser nulo");

            if (string.IsNullOrWhiteSpace(extensao))
                throw new InvalidOperationException("diretorio não pode ser nulo");


            return new FotoVistoria
            {
                NomeArquivo = nome,
                Raiz = raiz,
                Diretorio = diretorio,
                Extensao = extensao,
                DataUpload = DateTime.Now,
                QuantidadeBytes = quantidadeBytes
            };
        }       
    }
}
