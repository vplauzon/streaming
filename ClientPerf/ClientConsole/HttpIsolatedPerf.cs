using Microsoft.Azure.EventHubs;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace ClientConsole
{
    /// <summary>
    /// Based on https://docs.microsoft.com/en-us/rest/api/eventhub/Send-event
    /// </summary>
    public class HttpIsolatedPerf : Scenario
    {
        public HttpIsolatedPerf(string connectionString) : base(connectionString)
        {
        }

        public override async Task RunAsync()
        {
            for (int i = 0; i != 10; ++i)
            {
                var elasped = await TimeFunctionAsync(SendOneEventAsync);

                Console.WriteLine($"One Event:  {elasped}");
            }
        }

        private async Task SendOneEventAsync()
        {
            var builder = new EventHubsConnectionStringBuilder(ConnectionString);
            var nameSpace = builder.Endpoint.Authority.Split('.')[0];
            var client = new HttpClient();
            var resourceUrl = $"https://{nameSpace}.servicebus.windows.net/{builder.EntityPath}";
            var url = $"{resourceUrl}/messages";
            var content = new StringContent(
                GetDummyEventString(),
                Encoding.UTF8,
                "application/json");

            client.DefaultRequestHeaders.TryAddWithoutValidation(
                "Authorization",
                CreateToken(resourceUrl, builder.SasKeyName, builder.SasKey));

            var response = await client.PostAsync(url, content);
            var responseText = await response.Content.ReadAsStringAsync();
        }

        private static string CreateToken(string resourceUri, string keyName, string key)
        {
            TimeSpan sinceEpoch = DateTime.UtcNow - new DateTime(1970, 1, 1);
            var week = 60 * 60 * 24 * 7;
            var expiry = Convert.ToString((int)sinceEpoch.TotalSeconds + week);
            string stringToSign = HttpUtility.UrlEncode(resourceUri) + "\n" + expiry;
            HMACSHA256 hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
            var signature = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign)));
            var sasToken = String.Format(CultureInfo.InvariantCulture, "SharedAccessSignature sr={0}&sig={1}&se={2}&skn={3}", HttpUtility.UrlEncode(resourceUri), HttpUtility.UrlEncode(signature), expiry, keyName);

            return sasToken;
        }
    }
}