using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace SignalRTestApp
{
    public class ChatHub : Hub
    {
        private List<string> _chatGroups = new() { "general" };

        private string CurrentUserName =>
            Context.User == null
            || Context.User.Identity == null
            || !Context.User.Identity.IsAuthenticated ? ConnectionUserName : Context.User.Identity.Name!;

        private string ConnectionUserName => $"User {Context.ConnectionId[..6]}";


        public async Task SendMessage(string message)
        {
            Console.WriteLine("Начало метода SendMessage");
            await Clients.All.SendAsync("ReceiveMessage", message, CurrentUserName);
            Console.WriteLine("Конец метода SendMessage");
        }

        public async Task JoinGroup(string groupName)
        {
            if (_chatGroups.Contains(groupName))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
                await Clients.Group(groupName).SendAsync("Notify", $"{CurrentUserName} joined the chat \"{groupName}\"");
            }
        }

        [Authorize(Roles = UserRoles.Admin)]
        public async Task Notify(string message)
        {
            await Clients.All.SendAsync("Notify", message);
        }

        public async override Task OnConnectedAsync()
        {
            await JoinGroup("general");
            await base.OnConnectedAsync();
        }

        public async override Task OnDisconnectedAsync(Exception? exception)
        {
            await Clients.All.SendAsync("Notify", $"{CurrentUserName} left the chat");
            await base.OnDisconnectedAsync(exception);
        }
    }
}
