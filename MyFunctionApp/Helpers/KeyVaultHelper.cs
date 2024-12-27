using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyFunctionApp.Helpers
{
    using System;
    using System.Collections.Concurrent;
    using System.Threading.Tasks;
    using Azure.Identity;
    using Azure.Security.KeyVault.Secrets;

    public class KeyVaultHelper
    {
        private readonly SecretClient _secretClient;
        private readonly ConcurrentDictionary<string, string> _cachedSecrets;

        public KeyVaultHelper(string keyVaultUri)
        {
            if (string.IsNullOrEmpty(keyVaultUri))
            {
                throw new ArgumentException("Key Vault URI cannot be null or empty.", nameof(keyVaultUri));
            }

            _secretClient = new SecretClient(new Uri(keyVaultUri), new DefaultAzureCredential());
            _cachedSecrets = new ConcurrentDictionary<string, string>();
        }

        public async Task<string> GetSecretAsync(string secretName)
        {
            if (string.IsNullOrEmpty(secretName))
            {
                throw new ArgumentException("Secret name cannot be null or empty.", nameof(secretName));
            }

            // Check if the secret is already in the cache
            if (_cachedSecrets.TryGetValue(secretName, out var cachedSecret))
            {
                return cachedSecret;
            }

            // Retrieve the secret from Azure Key Vault
            KeyVaultSecret secret = await _secretClient.GetSecretAsync(secretName);
            _cachedSecrets[secretName] = secret.Value; // Cache the secret in memory
            return secret.Value;
        }
    }

}
