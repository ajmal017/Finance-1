using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Finance
{
    /// <summary>
    /// Provides static methods for outputting to a text log
    /// </summary>
    public static class Logger
    {
        public delegate void LogEventHandler(object sender, LogEventArgs e);
        public static event LogEventHandler LogEvent;
        private static void OnLogEvent(LogMessage message)
        {
            while (message.Message.Length > 100)
            {                
                var m = new LogMessage(message.Sender, message.Message.Substring(0, 100), message.MessageType);
                LogEvent?.Invoke(null, new LogEventArgs(m));

                message = new LogMessage(message.Sender, message.Message.Remove(0, 100), message.MessageType);
            }

            LogEvent?.Invoke(null, new LogEventArgs(message));
        }
        public static void Log(LogMessage message)
        {
            OnLogEvent(message);
        }

    }
    public class LogMessage
    {
        public LogMessageType MessageType { get; }
        public string Sender { get; }
        public string Message { get; }
        public DateTime Created { get; }

        public LogMessage(string sender, string message, LogMessageType messageType = LogMessageType.Debug)
        {
            MessageType = messageType;
            Sender = sender ?? throw new ArgumentNullException(nameof(sender));
            Message = message ?? "NA";
            Created = DateTime.Now;
        }

        public override string ToString()
        {
            return string.Format($"{Created.ToString("yyyyMMdd HH:mm:ss.fff")} >> " +
                $"{Message} [From: {Sender}] " +
                $"({Enum.GetName(typeof(LogMessageType), MessageType)})");
        }
    }
}
