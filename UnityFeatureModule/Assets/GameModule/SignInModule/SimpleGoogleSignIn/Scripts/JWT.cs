using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json.Linq;
using UnityEngine.Networking;

namespace Assets.SimpleGoogleSignIn.Scripts
{
    /// <summary>
    /// JWT debugger: https://jwt.io/
    /// </summary>
    public class JWT
    {
        public readonly string Encoded;

        public string Header => Base64UrlEncoder.Decode(Encoded.Split('.')[0]);
        public string Payload => Base64UrlEncoder.Decode(Encoded.Split('.')[1]);
        public string SignedData => Encoded.Split('.')[0] + "." + Encoded.Split('.')[1];
        public string Signature => Encoded.Split('.')[2];

        private const string JwksUri = "https://www.googleapis.com/oauth2/v3/certs";

        private static readonly Dictionary<string, Dictionary<string, string>> KnownPublicKeys = new Dictionary<string, Dictionary<string, string>>
        {
            { "c3afe7a9bda46bae6ef97e46c95cda48912e5979", new Dictionary<string, string> { { "e", "AQAB" }, { "n", "qxHzsqeQzXW-LT2Z-k30bJPhoMful1wUVPYUmukRR7qRnsC-7mQYaXkXaiuYcdlsZBS_AzfppQVIJ6GKncXQcZJ7-x-RwRm2exSdbmQ8xPJY1c1BLflc0Qa4fwGY_MjbR1kvlcx6etWhsnJqmivX9ALnCF5ZTR4ewC-BH7ZuilUYb6bCgG-zpSHNIQpgxO9gE8XoPBujGK9w6v_uzZb4rj2_8KWWT6RRBBQs1KDZmxzFkDcVOjgyTLmGPpHLQDF3R02DHzeaB84KB0QM-KyKIK1ejzCljdwCPAhNB9r14-01cUI1GUKuhv0tPgne3Je9qPIxl_g2FuZuqBnT1MPo9w" } } },
            { "c7e1141059a19b218209bc5af7a81a720e39b500", new Dictionary<string, string> { { "e", "AQAB" }, { "n", "rHXjB-RvfTDtw7LEaEai8rl8vyi8q2cGNy78jAyBMAwZYQVcqlvkx5Xuw-_oEaWoYcAPBLTqD1FCz4LvawiXMu0QFAl_rgzzbjvp_CHcKVnYCTlKJF6wwfegkmdneJV5m0k6-_o7sqouNtSVQNF-gR2W3DKb88WB2_b9SNR24ZLf4j7kH_JGUo8mj4K0gc4F2ZtBrTxunWmKdrAqWx6hdQUoe1tJaff2VJQs5YtVNtGj1Iuh6y3q-Sfp4BdOmP9KYljmwAQ0HKRVkgClNkChZzpj23nQhFrtGNcZIyCsbSs5qMJsUZ3LygK-TZZ9ykx5CxyWXNPdry6trDFVosdbEQ" } } }
        };

        public JWT(string encoded)
        {
            Encoded = encoded;
        }

        /// <summary>
        /// More info: https://developers.google.com/identity/openid-connect/openid-connect#validatinganidtoken
        /// Signature validation makes sense on a backend only in most cases.
        /// </summary>
        public void ValidateSignature(Action<bool, string> callback)
        {
            var header = JObject.Parse(Header);

            if ((string) header["typ"] != "JWT")
            {
                callback(false, "Unexpected header (typ).");
                return;
            }

            if ((string) header["alg"] != "RS256")
            {
                callback(false, "Unexpected header (alg).");
                return;
            }

            var payload = JObject.Parse(Payload);

            if ((string) payload["iss"] != "https://accounts.google.com")
            {
                callback(false, "Unexpected payload (iss).");
                return;
            }

            if ((string) payload["aud"] != Settings.Instance.ClientId)
            {
                callback(false, "Unexpected payload (aud).");
                return;
            }

            var exp = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds((int) payload["exp"]);

            if (exp < DateTime.UtcNow)
            {
                callback(false, "JWT expired.");
                return;
            }

            var kid = (string) header["kid"];

            if (KnownPublicKeys.ContainsKey(kid))
            {
                var verified = ValidateSignature(KnownPublicKeys[kid]["n"], KnownPublicKeys[kid]["e"]);

                if (verified)
                {
                    callback(true, null);
                }
                else
                {
                    callback(false, "Invalid JWT signature.");
                }

                return;
            }

            var request = UnityWebRequest.Get(JwksUri); // TODO: Cache keys.

            request.SendWebRequest().completed += obj =>
            {
                if (request.result == UnityWebRequest.Result.Success)
                {
                    var certs = JObject.Parse(request.downloadHandler.text);
                    var keys = certs["keys"].ToDictionary(i => i["kid"], i => i);
                    
                    if (!keys.ContainsKey(kid))
                    {
                        callback(false, $"Public key not found (kid={kid}).");
                        return;
                    }

                    var verified = ValidateSignature((string) keys[kid]["n"], (string) keys[kid]["e"]);

                    if (verified)
                    {
                        callback(true, null);
                    }
                    else
                    {
                        callback(false, "Invalid JWT signature.");
                    }
                }
                else
                {
                    callback(false, $"{request.error}: {request.downloadHandler.text}");
                }

                request.Dispose();
            };
        }

        private bool ValidateSignature(string modulus, string exponent)
        {
            var parameters = new RSAParameters
            {
                Modulus = Base64UrlEncoder.DecodeBytes(modulus),
                Exponent = Base64UrlEncoder.DecodeBytes(exponent)
            };
            var provider = new RSACryptoServiceProvider();

            provider.ImportParameters(parameters);

            var signature = Base64UrlEncoder.DecodeBytes(Signature);
            var sha = new SHA256Managed();
            var data = Encoding.UTF8.GetBytes(SignedData);
            var verified = provider.VerifyData(data, sha, signature);

            return verified;
        }
    }
}