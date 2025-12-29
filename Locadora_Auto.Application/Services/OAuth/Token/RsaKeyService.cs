using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;

namespace Locadora_Auto.Application.Services.OAuth.Token
{
    public class RsaKeyService
    {
        private readonly RSA _rsa;
        public string KeyId { get; } 

        public RsaKeyService(IConfiguration config, IWebHostEnvironment env)
        {
            var keyPath = config["Jwt:PrivateKeyPath"];
            KeyId = config["Jwt:KeyId"] ?? "auth-key-01";

            if (string.IsNullOrWhiteSpace(keyPath))
                throw new InvalidOperationException("Jwt:PrivateKeyPath não configurado");

            if (!File.Exists(keyPath))
            {
                CriarNovaChave(keyPath);
            }

            _rsa = RSA.Create();
            _rsa.ImportFromPem(File.ReadAllText(keyPath));
        }

        private static void CriarNovaChave(string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);

            using var rsa = RSA.Create(2048);
            var privateKeyPem = rsa.ExportRSAPrivateKeyPem();

            File.WriteAllText(path, privateKeyPem);
        }

        public SigningCredentials GetSigningCredentials()
        {
            var key = new RsaSecurityKey(_rsa)
            {
                KeyId = KeyId
            };

            return new SigningCredentials(key, SecurityAlgorithms.RsaSha256);
        }

        public JsonWebKey GetPublicJwk()
        {
            var parameters = _rsa.ExportParameters(false);

            return new JsonWebKey
            {
                Kty = "RSA",
                Use = "sig",
                Kid = KeyId,
                Alg = "RS256",
                N = Base64UrlEncoder.Encode(parameters.Modulus),
                E = Base64UrlEncoder.Encode(parameters.Exponent)
            };
        }
    }
}
