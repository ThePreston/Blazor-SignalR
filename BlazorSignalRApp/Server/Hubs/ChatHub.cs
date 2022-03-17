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
using Newtonsoft.Json;

namespace BlazorSignalRApp.Server.Hubs
{
    public class ChatHub : Hub
    {
        public ChatHub(IConfiguration configuration, TableClient tableClient)
        {
            _config = configuration;
            _client = tableClient;
        }

        private IConfiguration _config;

        private TableClient _client;

        public async Task SendMessage(string TransId)
        {

            var retVal = new List<BlobProcess>();

            var recs = _client.QueryAsync<TableEntity>(filter: $"PartitionKey eq '{TransId}'");

            await foreach(var page in recs.AsPages())
            {
                foreach(var entity in page.Values)                
                    retVal.Add(new BlobProcess(TransId, 
                                               entity.GetString("RowKey"), 
                                               entity.GetString("EventName"), 
                                               entity.GetString("Data"), 
                                               entity.GetDateTime("Timestamp").ToString()));
            }

            await Clients.All.SendAsync("ReceiveMessage", JsonConvert.SerializeObject(retVal));
        }

        public async Task StartProcess(string TransId, string message)
        {

            var Responsemessage = "";
            var iCounter = 0;

            do
            {

                try
                {
                    Responsemessage = await InitiateAsyncProcess($"{{ \"ID\":\"{TransId}\",{message} }}");
                    iCounter++;
                }
                catch (Exception ex)
                {
                    if (!ex.Message.Contains("No such host is known"))
                        Responsemessage = ex.Message.ToString();
                }

            } while (string.IsNullOrEmpty(Responsemessage) && iCounter < 2);

            await Clients.All.SendAsync("ReceiveMessage", $"Started - {Responsemessage}");

        }

        private async Task<string> InitiateAsyncProcess(string clientMessage)
        {

            using var reqMessage = GetRequestMessage(HttpMethod.Post, _config["LogicAppURL"], clientMessage);
            {

                using var client = new HttpClient();

                var resp = await GetMessage(reqMessage);

                return await resp.Content.ReadAsStringAsync();

            }
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