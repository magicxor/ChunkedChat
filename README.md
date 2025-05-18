# ChunkedChat

This project is a proof-of-concept (PoC) demonstrating a web-based chat application that operates without any client-side JavaScript. It relies on chunked HTML responses to deliver new messages to users in real-time.

https://github.com/user-attachments/assets/3ef367e2-45bb-432b-a711-bc395e61eb58

## How it Works

The application is built using ASP.NET Core Minimal APIs and C#.

1.  **Main Chat Page (`/rooms/{room}`):**
    *   When a user navigates to a chat room (e.g., `/rooms/general`), they are served a simple HTML page.
    *   This page embeds two `<iframe>` elements:
        *   One `<iframe>` displays the live message feed (`/rooms/{room}/live-messages`).
        *   The other `<iframe>` displays the form for submitting new messages (`/rooms/{room}/new-message-form`).

2.  **New Message Form (`/rooms/{room}/new-message-form`):**
    *   This serves a basic HTML `<form>`.
    *   When a user types a message and submits the form, the data is `POST`ed to `/rooms/{room}/messages`.

3.  **Submitting a Message (`POST /rooms/{room}/messages`):**
    *   The server receives the new message.
    *   The message is stored in a server-side log for the specific chat room. Each message (`ChatMessage`) contains an ID (time-ordered GUID v7), a timestamp, an optional username, and the message text.
    *   The `ChatRoomLog` class manages these messages in a thread-safe manner, storing them chronologically.
    *   After processing, the user is redirected back to the new message form page.

4.  **Live Message Feed (`/rooms/{room}/live-messages`):**
    *   This is the core of the "JavaScript-less real-time" feature.
    *   When a client (the `<iframe>` from the main page) requests this endpoint, the server keeps the connection open.
    *   Initially, it sends the basic HTML document structure.
    *   Periodically (e.g., every second), the server checks the `ChatRoomLog` for new messages that have arrived since the client's last update.
    *   If new messages are found, they are formatted as HTML nodes and written to the response stream, followed by a flush operation.
    *   This "chunked" delivery means the browser receives and renders these new HTML snippets as they arrive, updating the message display without a full page reload or any JavaScript intervention.

## Key Components

*   `Program.cs`: Sets up the ASP.NET Core application, defines the API endpoints, and manages chat room data.
*   `ChatMessage.cs`: Defines the structure for a single chat message (ID, Timestamp, UserName, Text).
*   `ChatRoomLog.cs`: Manages the collection of messages for a specific chat room in a thread-safe and chronologically ordered manner.

## To Run

1.  Ensure you have the .NET SDK installed.
2.  Navigate to the `ChunkedChat` project directory.
3.  Run the application using `dotnet run`.
4.  Open your browser and navigate to an address like `http://localhost:<port>/rooms/your-room-name` (the actual port will be shown in the console when the application starts).
