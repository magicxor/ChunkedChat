using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Net.ServerSentEvents;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc;

namespace ChunkedChat;

public class Program
{
    private static readonly ConcurrentDictionary<string, ChatRoomLog> ChatRoomMessages = new();

    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        var app = builder.Build();

        app.MapGet("/rooms/{room}", (
            [FromRoute] [Required] string room,
            [FromServices] IServer server) =>
        {
            var serverUrl = server.Features.Get<IServerAddressesFeature>()?.Addresses.FirstOrDefault() ?? throw new InvalidOperationException("No server address configured");
            var htmlContent = $$"""
                                <!doctype html>
                                <html lang=en>
                                <head>
                                <meta charset=utf-8>
                                <meta name="color-scheme" content="dark light">
                                <title>Chat</title>
                                <style>
                                  *,
                                  *::before,
                                  *::after {
                                      box-sizing: border-box;
                                  }
                                </style>
                                </head>
                                <body style="margin: 0;">
                                  <div style="display: flex; flex-direction: column; width: 100%; height: 100dvh; border: none;">
                                    <iframe src="{{serverUrl}}/rooms/{{room}}/live-messages" style="flex-grow: 1; border: none;"></iframe>
                                    <iframe src="{{serverUrl}}/rooms/{{room}}/new-message-form" style="height: 200px; border: none;"></iframe>
                                  </div>
                                </body>
                                </html>
                                """;
            return TypedResults.Content(
                content: htmlContent,
                contentType: "text/html");
        }).WithName("Chat room page HTML");

        app.MapGet("/rooms/{room}/new-message-form", (
            [FromRoute] [Required] string room,
            [FromServices] IServer server) =>
        {
            var serverUrl = server.Features.Get<IServerAddressesFeature>()?.Addresses.FirstOrDefault() ?? throw new InvalidOperationException("No server address configured");
            var htmlContent = $$"""
                                <!doctype html>
                                <html lang=en>
                                <head>
                                <meta charset=utf-8>
                                <meta name="color-scheme" content="dark light">
                                <title>Chat form</title>
                                <style>
                                  *,
                                  *::before,
                                  *::after {
                                      box-sizing: border-box;
                                  }
                                </style>
                                </head>
                                <body style="margin: 0;">
                                    <form action="{{serverUrl}}/rooms/{{room}}/messages" method="POST" style="display: flex; flex-direction: column; width: 100%; height: 100dvh;">
                                        <label for="text" style="margin: 5px;">Message text:</label>
                                        <textarea type="text" id="text" name="text" required autofocus style="flex-grow: 1; margin: 5px; resize: none;"></textarea>
                                        <button type="submit" style="margin: 5px;">Send</button>
                                    </form>
                                </body>
                                </html>
                                """;
            return TypedResults.Content(
                content: htmlContent,
                contentType: "text/html");
        }).WithName("Chat room new message form HTML");

        app.MapGet("/rooms/{room}/live-messages", async (
            [FromRoute] [Required] string room,
            [FromQuery] DateTime? since,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            context.Response.StatusCode = 200;
            context.Response.ContentType = "text/html";

            await context.Response.WriteAsync($$"""
                                                <!doctype html>
                                                <html lang=en>
                                                <head>
                                                  <meta charset=utf-8>
                                                  <meta name="color-scheme" content="dark light">
                                                  <title>Chat</title>
                                                </head>
                                                <body>
                                                  <p>you joined the room '{{room}}'</p>
                                                """, cancellationToken);
            await context.Response.Body.FlushAsync(cancellationToken);

            var timestamp = since ?? DateTime.UtcNow;

            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(1000, cancellationToken);
                var chatRoomLog = ChatRoomMessages.GetOrAdd(room, _ => new ChatRoomLog());
                var currentBatch = chatRoomLog.GetMessagesSince(timestamp);
                timestamp = DateTime.UtcNow;

                foreach (var message in currentBatch)
                {
                    await context.Response.WriteAsync($"<p>[{message.Timestamp}] {message.UserName ?? "Anonymous"}: {message.Text}</p>", cancellationToken);
                    await context.Response.Body.FlushAsync(cancellationToken);
                }
            }
        }).WithName("Chat room messages");

        app.MapPost("/rooms/{room}/messages", (
            [FromRoute] [Required] string room,
            [FromForm] [Required] string text) =>
        {
            var chatRoomLog = ChatRoomMessages.GetOrAdd(room, _ => new ChatRoomLog());
            var message = new ChatMessage(null, text);
            chatRoomLog.AddMessage(message);
            return Results.LocalRedirect($"/rooms/{room}/new-message-form");
        }).WithName("Chat room new message POST").DisableAntiforgery();

        app.Run();
    }
}
