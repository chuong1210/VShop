using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace api_be.Application.Services.HubService
{
    public class FlashSaleHubService:Hub
    {
        public async Task NotifyFlashSaleUpdate(string message)
        {
            await Clients.All.SendAsync("ReceiveFlashSaleUpdate", message);
        }
    }
}
