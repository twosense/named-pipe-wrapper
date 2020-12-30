using System;
using System.Collections.Concurrent;
using System.IO.Pipes;
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
        private StringNamedPipeClient _client;

        private ConcurrentQueue<string> _serverMessageQueue;
        private ConcurrentQueue<string> _clientMessageQueue;
        
        private ManualResetEvent _serverReceivedMessageEvent;
        private ManualResetEvent _clientReceivedMessageEvent;

        [SetUp]
        public void SetUp()
        {
            _serverMessageQueue = new ConcurrentQueue<string>();
            _clientMessageQueue = new ConcurrentQueue<string>();
            
            _serverReceivedMessageEvent = new ManualResetEvent(false);
            _clientReceivedMessageEvent = new ManualResetEvent(false);

            _server = new StringNamedPipeServer(PipeName);
            _client = new StringNamedPipeClient(PipeName);

            _server.ClientMessage += OnClientMessageReceived;
            _server.Start();

            _client.ServerMessage += OnServerMessageReceived;
            _client.Start();
            _client.WaitForConnection();
        }

        private void OnServerMessageReceived(NamedPipeConnection<string, string> connection, string message)
        {
            _clientMessageQueue.Enqueue(message);
            _clientReceivedMessageEvent.Set();
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
            _client.PushMessage(message);

            _serverReceivedMessageEvent.WaitOne(Timeout);

            _serverMessageQueue.TryDequeue(out var messageReceived);
            messageReceived.Should().Be(message);
        }

        [Test]
        public void ClientShouldReceiveSameMessageServerSent()
        {
            var message = Guid.NewGuid().ToString();
            _server.PushMessage(message);
            
            _clientReceivedMessageEvent.WaitOne(Timeout);

            _clientMessageQueue.TryDequeue(out var messageReceived);
            messageReceived.Should().Be(message);
        }
    }

    public class StringNamedPipeClient : NamedPipeClient<string>
    {
        public StringNamedPipeClient(string pipeName) : base(pipeName)
        {
        }
    }

    public class StringNamedPipeServer : NamedPipeServer<string>
    {
        public StringNamedPipeServer(string pipeName) : base(pipeName)
        {
        }

        public StringNamedPipeServer(string pipeName, int bufferSize, PipeSecurity security) : base(pipeName,
            bufferSize, security)
        {
        }
    }
}