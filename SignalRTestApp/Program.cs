using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using SignalRTestApp;
using SignalRTestApp.Filters;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Security.Claims;
using System.Threading.Tasks;

internal class Program
{
    private static Dictionary<string, UserData> _usersByLogin = new();

    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options => options.LoginPath = "/login");
        builder.Services.AddAuthorization();
        builder.Services.AddSignalR(config =>
        {
            config.AddFilter<LoggingHubFilter>();
            //config.SupportedProtocols = new[] { "", "" };
        });

        var app = builder.Build();

        app.UseDefaultFiles();
        app.UseStaticFiles();
        app.UseAuthentication();
        app.UseAuthorization();

        //app.MapGet(
        //    "/",
        //    async (HttpContext context) =>
        //    {
        //        if (context.User.IsInRole(UserRoles.Admin))
        //            Results.Redirect("/admin");
        //        else
        //            await SendHtmlAsync(context, "wwwroot/index.html");
        //    });
        app.MapGet(
            "/login",
            async context => await SendHtmlAsync(context, "html/login.html"));

        app.MapGet(
            "/logout",
            async (HttpContext context) =>
            {
                await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return Results.Redirect("/login");
            });

        app.MapGet(
            "/admin",
            [Authorize(Roles = UserRoles.Admin)] async (HttpContext context) =>
            
            {
                await SendHtmlAsync(context, "html/adminChat.html");
            });

        app.MapPost(
            "/login",
            async (string? returnUrl, HttpContext context) =>
            {
                var form = context.Request.Form;
                if (!form.ContainsKey("login") || form["login"].ToString() == null)
                {
                    return Results.BadRequest("No UserName");
                }
                var login = form["login"].ToString();
                if (_usersByLogin.ContainsKey(login))
                {
                    //user exists
                }
                else
                {
                //new user
                if (login == null || login == string.Empty)
                    {
                        return Results.BadRequest("Invalid UserName");
                    }
                    var role = login.ToLower() == "admin" ? UserRoles.Admin : UserRoles.User;
                    _usersByLogin[login] = new(login, role);
                }
                var claims = new[]
                {
                    new Claim(ClaimTypes.Name, login),
                    new Claim(ClaimTypes.Role, _usersByLogin[login].Role),
                };
                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
                await context.SignInAsync(claimsPrincipal);
                return Results.Redirect(returnUrl ?? "/");
            });

        app.MapHub<ChatHub>("/chat", config =>
        {
            config.Transports = HttpTransportType.WebSockets | HttpTransportType.ServerSentEvents;
        });
        //app.MapGet("/", () => "Hello World!");

        app.Run();

        async Task SendHtmlAsync(HttpContext context, string path)
        {
            context.Response.ContentType = "text/html; charset=utf-8";
            await context.Response.SendFileAsync(path);
        }
    }
}