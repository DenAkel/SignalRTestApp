using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;

namespace SignalRTestApp.Filters
{
    public class LoggingHubFilter : IHubFilter
    {
        public async ValueTask<object?> InvokeMethodAsync(
            HubInvocationContext invocationContext,
            Func<HubInvocationContext, ValueTask<object?>> next)
        {
            Console.WriteLine($"Вызов метода {invocationContext.HubMethodName}");
            try
            {
                return await next(invocationContext);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Не удалось вызвать метод {invocationContext.HubMethodName}: {ex.Message}");
                throw;
            }
        }
    }
}
