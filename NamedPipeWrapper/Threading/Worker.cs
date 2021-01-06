using System;
using System.Threading;
using System.Threading.Tasks;
using Twosense.WindowsService.ExposedInterfaces;

namespace NamedPipeWrapper.Threading
{
    class Worker
    {
        private readonly TaskScheduler _callbackThread;
        private IExposedLogger _logger;

        private static TaskScheduler CurrentTaskScheduler
        {
            get
            {
                return (SynchronizationContext.Current != null
                            ? TaskScheduler.FromCurrentSynchronizationContext()
                            : TaskScheduler.Default);
            }
        }

        public event WorkerSucceededEventHandler Succeeded;
        public event WorkerExceptionEventHandler Error;

        public Worker() : this(CurrentTaskScheduler)
        {
        }

        public Worker(TaskScheduler callbackThread)
        {
            LogDebug("initialized");
            _callbackThread = callbackThread;
        }

        public void DoWork(Action action)
        {
            LogDebug("DoWork");
            new Task(DoWorkImpl, action, CancellationToken.None, TaskCreationOptions.LongRunning).Start();
        }

        private void DoWorkImpl(object oAction)
        {
            var action = (Action) oAction;
            try
            {
                action();
                Callback(Succeed);
            }
            catch (Exception e)
            {
                Callback(() => Fail(e));
            }
        }

        private void Succeed()
        {
            LogDebug("Succeed");
            if (Succeeded != null)
                Succeeded();
        }

        private void Fail(Exception exception)
        {
            if (Error != null)
            {
                LogError(exception, "Fail");
                Error(exception);
            }
        }

        private void Callback(Action action)
        {
            Task.Factory.StartNew(action, CancellationToken.None, TaskCreationOptions.None, _callbackThread);
        }

        public void SetLogger(IExposedLogger logger)
        {
            _logger = logger;
        }

        public void LogDebug(string message)
        {
            if (_logger != null)
            {
                _logger.LogDebug($"NamedPipeWrapper.Threading.Worker: {message}");
            }
        }

        public void LogError(Exception exception, string message)
        {
            if (_logger != null)
            {
                _logger.LogError(exception, $"NamedPipeWrapper.Threading.Worker: {message}");
            }
        }
    }

    internal delegate void WorkerSucceededEventHandler();
    internal delegate void WorkerExceptionEventHandler(Exception exception);
}
