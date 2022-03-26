using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Azure.Data.Tables;
using BlazorSignalRApp.Server.Model;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace BlazorSignalRApp.Server.Hubs
{
    public class ChatHub : Hub
    {
        public ChatHub(IConfiguration configuration, TableClient tableClient, ILogger<ChatHub> logger)
        {
            _config = configuration;
            _client = tableClient;
            _logger = logger;
        }

        private IConfiguration _config;

        private TableClient _client;

        private ILogger<ChatHub> _logger;

        public async Task SendMessage(string TransId)
        {

            var retVal = new List<BlobProcess>();
            var retString = new List<string>();

            try 
            {

                var recs = _client.QueryAsync<TableEntity>(filter: $"PartitionKey eq '{TransId}'");

                await foreach (var page in recs.AsPages()) {
                    foreach (var entity in page.Values) {
                        retVal.Add(new BlobProcess(TransId,
                                                   entity.GetString("RowKey"),
                                                   entity.GetString("EventName"),
                                                   entity.GetString("Data"),
                                                   entity.GetDateTime("Timestamp").ToString()));
                    }
                }

                retVal.OrderBy(x => x.Timestamp).ToList()
                      .ForEach(newObj => retString.Add($"{newObj.EventName}-{newObj.RowKey}-{newObj.Data}-{newObj.Timestamp}"));

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"SendMessage {TransId}");
            }

            await Clients.All.SendAsync("ReceiveMessage", retString.ToArray());
        }

        public async Task StartProcess(string TransId, string message)
        {
            
            var Responsemessage = "";
            var RetryCounter = 0;

            do
            {

                try
                {
                    Responsemessage = await InitiateAsyncProcess($"{{ \"ID\":\"{TransId}\",{message} }}");
                    RetryCounter++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Connection issue on StartProcess attempt {RetryCounter}");
                    if (!ex.Message.Contains("No such host is known"))
                        Responsemessage = ex.Message.ToString();
                }

            } while (string.IsNullOrEmpty(Responsemessage) && RetryCounter < 2);

            await Clients.All.SendAsync("ReceiveMessage", new List<string>() { Responsemessage }.ToArray());
        }

        private async Task<string> InitiateAsyncProcess(string clientMessage)
        {

            using var reqMessage = GetRequestMessage(HttpMethod.Post, _config["LogicAppURL"], clientMessage);
            using var client = new HttpClient();

            var resp = await GetMessage(reqMessage);

            return await resp.Content.ReadAsStringAsync();
        }

        private async Task<HttpResponseMessage> GetMessage(HttpRequestMessage requestMessage)
        {
            using var client = new HttpClient();
            return await client.SendAsync(requestMessage);
        }

        private static HttpRequestMessage GetRequestMessage(HttpMethod method, string requestURI, string body)
        {
            return new HttpRequestMessage()
            {
                Method = method,
                RequestUri = new Uri(requestURI),
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
        }

    }
}