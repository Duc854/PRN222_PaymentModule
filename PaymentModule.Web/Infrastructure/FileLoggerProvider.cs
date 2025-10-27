using Microsoft.Extensions.Logging;
using System.IO;

namespace PaymentModule.Web.Infrastructure
{
    public class FileLoggerProvider : ILoggerProvider
    {
        private readonly string _filePath;
        private readonly object _lock = new();

        public FileLoggerProvider(string filePath)
        {
            _filePath = filePath;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new FileLogger(_filePath, _lock);
        }

        public void Dispose() { }

        private class FileLogger : ILogger
        {
            private readonly string _filePath;
            private readonly object _lock;

            public FileLogger(string filePath, object lockObj)
            {
                _filePath = filePath;
                _lock = lockObj;
            }

            public IDisposable BeginScope<TState>(TState state) => null!;
            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId,
                TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                if (!IsEnabled(logLevel)) return;

                var logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{logLevel}] {formatter(state, exception)}";
                if (exception != null)
                    logMessage += $" | Exception: {exception.Message}";

                lock (_lock)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
                    File.AppendAllText(_filePath, logMessage + Environment.NewLine);
                }
            }
        }
    }
}
