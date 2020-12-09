using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using NUnit.Framework;
using NamedPipeWrapper;
using log4net.Appender;
using log4net.Config;
using log4net.Layout;
using System.IO.Pipes;
using System.IO;

namespace UnitTests
{
    [TestFixture]
    class ExternalClientTests
    {
        private static readonly log4net.ILog Logger =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        static ExternalClientTests()
        {
            var layout = new PatternLayout("%-6timestamp %-5level - %message%newline");
            var appender = new ConsoleAppender { Layout = layout };
            layout.ActivateOptions();
            appender.ActivateOptions();
            BasicConfigurator.Configure(appender);
        }

        private const string PipeName = "external_client_test_pipe";

        private NamedPipeServer<byte[]> _server;
        private NamedPipeClientStream _client;

        private DateTime _startTime;

        private readonly ManualResetEvent _barrier = new ManualResetEvent(false);

        #region Setup and teardown

        [SetUp]
        public void SetUp()
        {
            Logger.Debug("Setting up test...");

            _barrier.Reset();

            _server = new NamedPipeServer<byte[]>(PipeName, true);
            _client = new NamedPipeClientStream("external_client_test_pipe");

            _server.Error += ServerOnError;

            _server.StartServerOnly();
            _client.Connect();

            Logger.Debug("Client and server started");
            Logger.Debug("---");

            _startTime = DateTime.Now;
        }

        private void ServerOnError(Exception exception)
        {
            throw new NotImplementedException();
        }

        [TearDown]
        public void TearDown()
        {
            Logger.Debug("---");
            Logger.Debug("Stopping client and server...");

            _server.Stop();
            _client.Close();

            _server.Error -= ServerOnError;

            Logger.Debug("Client and server stopped");
            Logger.DebugFormat("Test took {0}", (DateTime.Now - _startTime));
            Logger.Debug("~~~~~~~~~~~~~~~~~~~~~~~~~~");
        }

        #endregion

        #region Tests

        [Test]
        public void TestEmptyMessageDoesNotDisconnectClient()
        {
            byte[] msg = Encoding.UTF8.GetBytes("\n");
            _client.Write(msg, 0, msg.Length);
            _barrier.WaitOne(TimeSpan.FromSeconds(2));
            Assert.IsTrue(_client.IsConnected);
        }

        [Test]
        public void TestSingleMessage()
        {
            byte[] msg = Encoding.UTF8.GetBytes("Hello!\n");
            _client.Write(msg, 0, msg.Length);
            _barrier.WaitOne(TimeSpan.FromSeconds(2));
            Assert.IsTrue(_client.IsConnected);
        }

        [Test]
        public void TestMultipleMessages()
        {
            byte[] msg = Encoding.UTF8.GetBytes("Message one\n");
            _client.Write(msg, 0, msg.Length);
            _barrier.WaitOne(TimeSpan.FromSeconds(2));
            msg = Encoding.UTF8.GetBytes("Message two!\n");
            _client.Write(msg, 0, msg.Length);
            msg = Encoding.UTF8.GetBytes("Message three!\n");
            _client.Write(msg, 0, msg.Length);
            _barrier.WaitOne(TimeSpan.FromSeconds(2));
            Assert.IsTrue(_client.IsConnected);
        }

        #endregion
    }
}

