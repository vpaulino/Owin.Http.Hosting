
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Dependencies;
using FluentAssertions;
using Microsoft.Owin;
using Microsoft.Owin.Hosting;
using Microsoft.Owin.Testing;
using SimpleInjector;
using Owin.WebSocket;
using Owin.WebSocket.Extensions;
using Xunit;

namespace Owin.Websockets.Tests
{
    public class WebSocketTests : IDisposable
    {
        private static IDisposable sWeb;

        


        private IDisposable CreateHttpServer(string port, Container dependencyResolver)
        {
           return WebApp.Start(new StartOptions($"http://localhost:{port}"), startup =>
            {
                startup.MapWebSocketRoute<TestConnection>();
                startup.MapWebSocketRoute<TestConnection>("/ws", dependencyResolver);
                startup.MapWebSocketPattern<TestConnection>("/captures/(?<capture1>.+)/(?<capture2>.+)", dependencyResolver);
            });
        }

        public WebSocketTests()
        {
            
             

           
        }


        public static void Cleanup()
        {
          
        }

       

        ClientWebSocket StartStaticRouteClient(string port, string route = "/ws")
        {
            var client = new ClientWebSocket();
            client.ConnectAsync(new Uri($"ws://localhost:{port}" + route), CancellationToken.None).Wait();
            return client;
        }

        ClientWebSocket StartRegextRouteClient(string port, string param1, string param2)
        {
            var client = new ClientWebSocket();
            client.ConnectAsync(
                new Uri($"ws://localhost:{port}/captures/" + param1 + "/" + param2),
                CancellationToken.None)
                .Wait();

            return client;
        }

        [Fact]
        public void ConnectionTest()
        {
            var dependencyResolver = new Container();
            var socket = new TestConnection();

            dependencyResolver.Register<TestConnection>(() => socket, Lifestyle.Transient);

            var server = CreateHttpServer("8181", dependencyResolver);
            var client = StartStaticRouteClient("8181");
            client.State.Should().Be(WebSocketState.Open);

            client.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None)
                .Wait();

            client.State.Should().Be(WebSocketState.Closed);
        }

        [Fact]
        public void ConnectionTest_Attribute()
        {
            var server = CreateHttpServer("8081", new Container());

            var client = StartStaticRouteClient("8081", "/wsa");
            client.State.Should().Be(WebSocketState.Open);

            client.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None)
                .Wait();

            client.State.Should().Be(WebSocketState.Closed);
        }

        [Fact]
        public void CloseWithEmptyStatusTest()
        {
            var dependencyResolver = new Container();
            var socket = new TestConnection();
            dependencyResolver.Register<TestConnection>(()=> socket, Lifestyle.Transient);

            var server = CreateHttpServer("8082", dependencyResolver);
             
            var client = StartStaticRouteClient("8082");
            client.State.Should().Be(WebSocketState.Open);

            client.CloseAsync(WebSocketCloseStatus.Empty, string.Empty, CancellationToken.None)
                .Wait();

            //Let the networking happen
            Thread.Sleep(500);

            client.State.Should().Be(WebSocketState.Closed);
            socket.OnCloseCalled.Should().BeTrue();
            socket.OnCloseAsyncCalled.Should().BeTrue();
        }

        [Fact]
        public void CloseWithStatusTest()
        {
            var dependencyResolver = new Container();
            var socket = new TestConnection();
            dependencyResolver.Register<TestConnection>(() => socket, Lifestyle.Transient);

            var server = CreateHttpServer("8083", dependencyResolver);

            var client = StartStaticRouteClient("8083");
            client.State.Should().Be(WebSocketState.Open);

            const string CLOSE_DESCRIPTION = "My Description";

            client.CloseAsync(WebSocketCloseStatus.NormalClosure, CLOSE_DESCRIPTION, CancellationToken.None)
                .Wait();

            //Let the networking happen
            Thread.Sleep(500);

            socket.OnCloseCalled.Should().BeTrue();
            socket.CloseStatus.Should().Be(WebSocketCloseStatus.NormalClosure);
            socket.CloseDescription.Should().Be(CLOSE_DESCRIPTION);

            socket.OnCloseAsyncCalled.Should().BeTrue();
            socket.AsyncCloseStatus.Should().Be(WebSocketCloseStatus.NormalClosure);
            socket.AsyncCloseDescription.Should().Be(CLOSE_DESCRIPTION);

        }

        [Fact]
        public void CloseByDisconnectingTest()
        {
            var dependencyResolver = new Container();
            var socket = new TestConnection();
            dependencyResolver.Register<TestConnection>(() => socket, Lifestyle.Transient);

            

            var server = CreateHttpServer("8084", dependencyResolver);

            var client = StartStaticRouteClient("8084");
            client.State.Should().Be(WebSocketState.Open);

            client.Dispose();
            var task = Task.Run(
                () =>
                {
                    while (!socket.OnCloseCalled || !socket.OnCloseAsyncCalled)
                        Thread.Sleep(10);
                });

            task.Wait(TimeSpan.FromMinutes(2)).Should().BeTrue();

            socket.OnCloseCalled.Should().BeTrue();
            socket.OnCloseAsyncCalled.Should().BeTrue();
            //socket.CloseStatus.Should().Be(WebSocketCloseStatus.Empty);
        }

        [Fact]
        public void SendTest()
        {
            var dependencyResolver = new Container();
            var socket = new TestConnection();
            dependencyResolver.Register<TestConnection>(() => socket, Lifestyle.Transient);

            

            var server = CreateHttpServer("8085", dependencyResolver);
            var client = StartStaticRouteClient("8085");

            var toSend = "Test Data String";
            SendText(client, toSend).Wait();
            Thread.Sleep(100);

            socket.LastMessage.Should().NotBeNull();
            var received = Encoding.UTF8.GetString(
                socket.LastMessage.Array,
                socket.LastMessage.Offset,
                socket.LastMessage.Count);

            received.Should().Be(toSend);
        }

        [Fact]
        public void ReceiveTest()
        {
            var dependencyResolver = new Container();
            var socket = new TestConnection();
            dependencyResolver.Register<TestConnection>(() => socket, Lifestyle.Singleton);

             

            var server = CreateHttpServer("8086", dependencyResolver);
            var client = StartStaticRouteClient("8086");

            var buffer = new byte[64 * 1024];
            var segment = new ArraySegment<byte>(buffer);
            var receiveCount = 0;
            var receiveTask = Task.Run(async () =>
            {
                var result = await client.ReceiveAsync(segment, CancellationToken.None);
                receiveCount = result.Count;
            });

            var toSend = "Test Data String";
            SendText(client, toSend).Wait();

            receiveTask.Wait();

            var received = Encoding.UTF8.GetString(segment.Array, segment.Offset, receiveCount);
            received.Should().Be(toSend);
        }

        [Fact]
        public void ArgumentsTest()
        {
            var dependencyResolver = new Container();
            var socket = new TestConnection();
            dependencyResolver.Register<TestConnection>(() => socket, Lifestyle.Transient);

            


            var param1 = "foo1";
            var param2 = "foo2";
            var server = CreateHttpServer("8180", dependencyResolver);

            var client = StartRegextRouteClient("8180",param1, param2);

            socket.Arguments["capture1"].Should().Be(param1);
            socket.Arguments["capture2"].Should().Be(param2);
        }

        //[Fact]
        //public void BadRequestTest()
        //{
        //    var client = new WebClient();
        //    var t = new Action(() => client.OpenRead("http://localhost:8989/ws"));
        //    var ex = t.ShouldThrow<WebException>().Which;

        //    (((HttpWebResponse)(ex.Response)).StatusCode).Should().Be(HttpStatusCode.BadRequest);
        //}

        [Fact]
        public void SendTextTest()
        {
            var dependencyResolver = new Container();
            var socket = new TestConnection();
            dependencyResolver.Register<TestConnection>(() => socket, Lifestyle.Transient);

            

            var server = CreateHttpServer("8087", dependencyResolver);
            var client = StartStaticRouteClient("8087");

            var text = Encoding.UTF8.GetBytes("My Text to send");
            client.SendAsync(new ArraySegment<byte>(text), WebSocketMessageType.Text, true, CancellationToken.None)
                .Wait();

            Thread.Sleep(50);

            var val = Encoding.UTF8.GetString(socket.LastMessage.Array, socket.LastMessage.Offset, socket.LastMessage.Count);
            val.Should().Be("My Text to send");
            socket.LastMessageType.Should().Be(WebSocketMessageType.Text);
        }

        [Fact]
        public void SendBinaryTest()
        {
            var dependencyResolver = new Container();
            var socket = new TestConnection();
            dependencyResolver.Register<TestConnection>(() => socket, Lifestyle.Transient);

            

            var server = CreateHttpServer("8088", dependencyResolver);
            var client = StartStaticRouteClient("8088");

            var data = new byte[1024];
            data[9] = 4;
            data[599] = 123;
            client.SendAsync(new ArraySegment<byte>(data), WebSocketMessageType.Binary, true, CancellationToken.None)
                .Wait();

            Thread.Sleep(50);

            socket.LastMessage.Count.Should().Be(data.Length);
            socket.LastMessageType.Should().Be(WebSocketMessageType.Binary);

            for (var i = 0; i < socket.LastMessage.Count; i++)
            {
                (socket.LastMessage.Array[socket.LastMessage.Offset + i] == data[i]).Should().BeTrue();
            }
        }

        async Task SendText(ClientWebSocket socket, string data)
        {
            var t = Encoding.UTF8.GetBytes(data);
            await socket.SendAsync(
                new ArraySegment<byte>(t),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None);
        }

        public void Dispose()
        {
            if (sWeb != null)
                sWeb.Dispose();
        }
    }
     

    [WebSocketRoute("/wsa")]
    class TestConnection : WebSocketConnection
    {
        public ArraySegment<byte> LastMessage { get; set; }
        public WebSocketMessageType LastMessageType { get; set; }
        public bool OnOpenCalled { get; set; }
        public bool OnOpenAsyncCalled { get; set; }
        public bool OnCloseCalled { get; set; }
        public bool OnCloseAsyncCalled { get; set; }
        public IOwinRequest Request { get; set; }

        public WebSocketCloseStatus? CloseStatus { get; set; }
        public string CloseDescription { get; set; }

        public WebSocketCloseStatus? AsyncCloseStatus { get; set; }
        public string AsyncCloseDescription { get; set; }

        public override async Task OnMessageReceived(ArraySegment<byte> message, WebSocketMessageType type)
        {
            LastMessage = message;
            LastMessageType = type;

            //Echo it back
            await Send(message, true, type);
        }

        public override void OnOpen()
        {
            OnOpenCalled = true;
        }

        public override Task OnOpenAsync()
        {
            OnOpenAsyncCalled = true;
            return Task.Delay(0);
        }

        public override void OnClose(WebSocketCloseStatus? closeStatus, string closeStatusDescription)
        {
            OnCloseCalled = true;
            CloseStatus = closeStatus;
            CloseDescription = closeStatusDescription;
        }

        public override Task OnCloseAsync(WebSocketCloseStatus? closeStatus, string closeStatusDescription)
        {
            OnCloseAsyncCalled = true;
            AsyncCloseStatus = closeStatus;
            AsyncCloseDescription = closeStatusDescription;
            return Task.Delay(0);
        }

    }
}
