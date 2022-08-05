using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace DnsUpdater
{

    public class Api : IDisposable
    {
        private readonly HttpClient client;
        private readonly HttpClientHandler httpClientHandler;
        private bool disposedValue = false;

        public string UiPassword { get; }

        public Api(string baseAddress, string uiPassword)
        {
            httpClientHandler = new HttpClientHandler();
            httpClientHandler.ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) =>
            {
                return true;
            };
            client = new HttpClient(httpClientHandler);
            client.BaseAddress = new Uri(baseAddress);
            UiPassword = uiPassword;
        }

        public async Task<string> FetchToken()
        {
            using var loginContent = new FormUrlEncodedContent(new Dictionary<string, string> { { "pw", UiPassword } });

            using var loginResponse = await client.PostAsync("admin/index.php?login=", loginContent);
            loginResponse.EnsureSuccessStatusCode();

            using var tokenResponse = await client.GetAsync("admin/index.php");
            tokenResponse.EnsureSuccessStatusCode();

            var body = await tokenResponse.Content.ReadAsStringAsync();

            Regex tokenRegex = new Regex("(?<=<div id=\"token\" hidden>)[^<]+");
            var match = tokenRegex.Match(body);

            if (string.IsNullOrEmpty(match.Value)) {
                throw new Exception("Login token not found. Check the password is correct.");
            }
            return match.Value;
        }

        public async Task UpdateDns(string domain, string ipAddress, string token)
        {
            using var dnsContent = new FormUrlEncodedContent(new Dictionary<string, string> {
                { "action", "add" },
                { "ip", ipAddress },
                { "domain", domain },
                { "token", token }
            });

            using var dnsResponse = await client.PostAsync("admin/scripts/pi-hole/php/customdns.php", dnsContent);
            dnsResponse.EnsureSuccessStatusCode();

            var body = await dnsResponse.Content.ReadAsStringAsync();
            var apiResponse = JsonConvert.DeserializeObject<ApiResponse>(body);
            if (apiResponse?.Success != true)
            {
                throw new Exception($"API response does not indicate success: {apiResponse?.Message}");
            }
        }

        public async Task DeleteDns(string domain, string ipAddress, string token)
        {
            using var dnsContent = new FormUrlEncodedContent(new Dictionary<string, string> {
                { "action", "delete" },
                { "ip", ipAddress },
                { "domain", domain },
                { "token", token }
            });

            using var dnsResponse = await client.PostAsync("admin/scripts/pi-hole/php/customdns.php", dnsContent);
            dnsResponse.EnsureSuccessStatusCode();

            var body = await dnsResponse.Content.ReadAsStringAsync();
            var apiResponse = JsonConvert.DeserializeObject<ApiResponse>(body);
            if (apiResponse?.Success != true)
            {
                throw new Exception($"API response does not indicate success: {apiResponse?.Message}");
            }
        }

        public async Task<Dictionary<string, string>> GetDns(string token)
        {
            using var dnsContent = new FormUrlEncodedContent(new Dictionary<string, string> {
                { "action", "get" },
                { "token", token }
            });

            using var dnsResponse = await client.PostAsync("admin/scripts/pi-hole/php/customdns.php", dnsContent);
            dnsResponse.EnsureSuccessStatusCode();

            var body = await dnsResponse.Content.ReadAsStringAsync();
            var apiResponse = JsonConvert.DeserializeObject<RawDnsGetResponse>(body);

            return apiResponse.ToDictionary();                
        }
        class RawDnsGetResponse
        {
            [JsonProperty("data")]
            public string[][]? Data { get; set; }

            public Dictionary<string, string> ToDictionary()
            {
                var dict = new Dictionary<string, string>();
                if (Data != null)
                {
                    foreach (var data in Data)
                    {
                        dict.Add(data[0], data[1]);
                    }
                }
                return dict;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    client?.Dispose();
                    httpClientHandler?.Dispose();
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
