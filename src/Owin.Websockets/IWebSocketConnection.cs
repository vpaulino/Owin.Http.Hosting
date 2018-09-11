
#if NET462
using System;
using System.Net.WebSockets;
using System.Threading.Tasks;
using Microsoft.Owin;

namespace Owin.WebSocket
{
    public interface IWebSocketConnection
    {
        void Abort();
        bool AuthenticateRequest(IOwinRequest request);
        Task<bool> AuthenticateRequestAsync(IOwinRequest request);
        Task Close(WebSocketCloseStatus status, string reason);
        void OnClose(WebSocketCloseStatus? closeStatus, string closeStatusDescription);
        Task OnCloseAsync(WebSocketCloseStatus? closeStatus, string closeStatusDescription);
        Task OnMessageReceived(ArraySegment<byte> message, WebSocketMessageType type);
        void OnOpen();
        Task OnOpenAsync();
        void OnReceiveError(Exception error);
        Task Send(ArraySegment<byte> buffer, bool endOfMessage, WebSocketMessageType type);
        Task SendBinary(ArraySegment<byte> buffer, bool endOfMessage);
        Task SendBinary(byte[] buffer, bool endOfMessage);
        Task SendText(ArraySegment<byte> buffer, bool endOfMessage);
        Task SendText(byte[] buffer, bool endOfMessage);
    }
}
#endif