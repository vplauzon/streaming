using Microsoft.Azure.EventHubs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
    public class HttpEventHubClient : IEventHubClient
    {
        private readonly HttpClient _client = new HttpClient();
        private readonly EventHubsConnectionStringBuilder _builder;

        private HttpEventHubClient(string connectionString)
        {
            _builder = new EventHubsConnectionStringBuilder(connectionString);
        }

        public static IEventHubClient CreateFromConnectionString(string connectionString)
        {
            return new HttpEventHubClient(connectionString);
        }

        async Task IEventHubClient.SendAsync(object jsonPayload)
        {
            var nameSpace = _builder.Endpoint.Authority.Split('.')[0];
            var resourceUrl = $"https://{nameSpace}.servicebus.windows.net/{_builder.EntityPath}";
            var url = $"{resourceUrl}/messages?api-version=2014-01&timeout=60";
            var content = new StringContent(
                JsonConvert.SerializeObject(jsonPayload),
                Encoding.UTF8,
                "application/json");

            _client.DefaultRequestHeaders.Remove("Authorization");
            _client.DefaultRequestHeaders.TryAddWithoutValidation(
                "Authorization",
                CreateToken(resourceUrl, _builder.SasKeyName, _builder.SasKey));

            var response = await _client.PostAsync(url, content);

            if (response.IsSuccessStatusCode)
            {
                return;
            }
            else
            {
                var responseText = await response.Content.ReadAsStringAsync();

                throw new InvalidOperationException(responseText);
            }
        }

        async Task IEventHubClient.SendBatchAsync(IEnumerable<object> batch)
        {
            var nameSpace = _builder.Endpoint.Authority.Split('.')[0];
            var resourceUrl = $"https://{nameSpace}.servicebus.windows.net/{_builder.EntityPath}";
            var url = $"{resourceUrl}/messages?api-version=2014-01&timeout=60";
            var payload = from b in batch
                          select new { Body = b };
            var content = new StringContent(
                JsonConvert.SerializeObject(payload.ToArray()),
                Encoding.UTF8,
                "application/vnd.microsoft.servicebus.json");

            _client.DefaultRequestHeaders.TryAddWithoutValidation(
                "Authorization",
                CreateToken(resourceUrl, _builder.SasKeyName, _builder.SasKey));

            var response = await _client.PostAsync(url, content);

            if (response.IsSuccessStatusCode)
            {
                return;
            }
            else
            {
                var responseText = await response.Content.ReadAsStringAsync();

                throw new InvalidOperationException(responseText);
            }
        }

        Task IEventHubClient.CloseAsync()
        {
            _client.Dispose();

            return Task.CompletedTask;
        }

        private static string CreateToken(string resourceUri, string keyName, string key)
        {
            TimeSpan sinceEpoch = DateTime.UtcNow - new DateTime(1970, 1, 1);
            var week = 60 * 60 * 24 * 7;
            var expiry = Convert.ToString((int)sinceEpoch.TotalSeconds + week);
            string stringToSign = HttpUtility.UrlEncode(resourceUri) + "\n" + expiry;
            HMACSHA256 hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
            var signature = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign)));
            var sasToken = String.Format(
                CultureInfo.InvariantCulture,
                "SharedAccessSignature sr={0}&sig={1}&se={2}&skn={3}",
                HttpUtility.UrlEncode(resourceUri),
                HttpUtility.UrlEncode(signature),
                expiry,
                keyName);

            return sasToken;
        }
    }
}