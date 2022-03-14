using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace BlazorSignalRApp.Server.Hubs
{
    public class ChatHub : Hub
    {
        public async Task SendMessage(string user, string message)
        {

            //Todo Call Logic App with
            //Listen to Event Grid,
            //messageclient


            await Clients.All.SendAsync("ReceiveMessage", user, message);
        }
    }
}
