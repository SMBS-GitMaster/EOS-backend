using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RadialReview.Hubs
{
  public class LogoutHub : Hub
  {
    public async Task NotifyLogout(string userId)
    {
      await Clients.User(userId).SendAsync("LogoutNotification");
    }

  }
}
