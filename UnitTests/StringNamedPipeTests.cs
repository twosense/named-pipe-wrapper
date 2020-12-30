using System;
using System.Collections.Concurrent;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using FluentAssertions;
using NamedPipeWrapper;
using NUnit.Framework;

namespace UnitTests
{
    [TestFixture]
    public class StringNamedPipeTests
    {
        private const string PipeName = "test-pipe";
        private const int Timeout = 1000;

        private StringNamedPipeServer _server;
        private NamedPipeClientStream _client;

        private ConcurrentQueue<string> _serverMessageQueue;

        private ManualResetEvent _serverReceivedMessageEvent;

        [SetUp]
        public void SetUp()
        {
            _serverMessageQueue = new ConcurrentQueue<string>();
            
            _serverReceivedMessageEvent = new ManualResetEvent(false);

            StartServer();
            StartClient();
        }

        private void StartServer()
        {
            _server = new StringNamedPipeServer(PipeName);
            _server.ClientMessage += OnClientMessageReceived;
            _server.ClientConnected += OnClientConnected;
            _server.Start();
        }

        private void StartClient()
        {
            _client = new NamedPipeClientStream(PipeName);
            _client.Connect();

            // Get pipe name
            const int bufferSize = 1024;
            var buffer = new byte[bufferSize];
            _client.Read(buffer, 0, bufferSize);
            var pipeName = buffer.ToString();
            _client.Close();
            
            _client = new NamedPipeClientStream(pipeName);
            _client.Connect();
        }

        private void OnClientConnected(NamedPipeConnection<string, string> connection)
        {
            return;
        }

        private void OnClientMessageReceived(NamedPipeConnection<string, string> connection, string message)
        {
            _serverMessageQueue.Enqueue(message);
            _serverReceivedMessageEvent.Set();
        }

        [Test]
        public void ServerShouldReceiveSameMessageClientSent()
        {
            var message = Guid.NewGuid().ToString();
            ClientSendMessage(message);

            _serverReceivedMessageEvent.WaitOne(Timeout);

            _serverMessageQueue.TryDequeue(out var messageReceived);
            messageReceived.Should().Be(message);
        }

        private void ClientSendMessage(string message)
        {
            var messageBytes = Encoding.UTF8.GetBytes(message);
            _client.Write(messageBytes, 0, messageBytes.Length);
            _client.Flush();
        }
        //
        // [Test]
        // public void ClientShouldReceiveSameMessageServerSent()
        // {
        //     var message = Guid.NewGuid().ToString();
        //     _server.PushMessage(message);
        //     
        //     _clientReceivedMessageEvent.WaitOne(Timeout);
        //
        //     _clientMessageQueue.TryDequeue(out var messageReceived);
        //     messageReceived.Should().Be(message);
        // }
    }
}