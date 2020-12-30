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
        private ManualResetEvent _clientMessageReceivedEvent;

        [SetUp]
        public void SetUp()
        {
            _serverMessageQueue = new ConcurrentQueue<string>();
            _clientMessageReceivedEvent = new ManualResetEvent(false);

            _server = new StringNamedPipeServer(PipeName);
            _client = new StringNamedPipeClient(PipeName);

            _server.ClientMessage += OnClientMessageReceived;
            _server.Start();
            _client.Start();
            _client.WaitForConnection();
        }

        private void OnClientMessageReceived(NamedPipeConnection<string, string> connection, string message)
        {
            _serverMessageQueue.Enqueue(message);
            _clientMessageReceivedEvent.Set();
        }

        [Test]
        public void ReadMessageShouldMatchSentMessage()
        {
            var message = Guid.NewGuid().ToString();
            _client.PushMessage(message);

            _clientMessageReceivedEvent.WaitOne(Timeout);

            string messageReceived;
            _serverMessageQueue.TryDequeue(out messageReceived);
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