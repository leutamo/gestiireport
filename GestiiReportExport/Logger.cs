using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
namespace Gestii
{
    public sealed class Logger
    {
        [Flags]
        public enum Targets
        {
            Console = 1,
            File = 2
        }

        public enum Level
        {
            Debug,
            Info,
            Warn,
            Error,
            Fatal
        }

        private class Entry
        {
            private DateTime dateTime;
            private Level level;
            private int threadId;
            private string message;

            public Entry(DateTime dateTime, Level level, int threadId, string message)
            {
                this.dateTime = dateTime;
                this.level = level;
                this.threadId = threadId;
                this.message = message.Replace("\t", "|").Replace("\r", "|").Replace("\n", "|");
            }

            public DateTime DateTime { get { return dateTime; } }
            public Level Level { get { return level; } }
            public int ThreadId { get { return threadId; } }
            public string Message { get { return message; } }

            public void WriteToConsole()
            {
                var dateTime2 = dateTime.ToString("yyyy.MM.dd HH:mm:ss.fff");
                var line = string.Format("{0}\t{1}\t{2}\t{3}", dateTime2, level, threadId, message);
                Console.WriteLine(line);
            }

            public void WriteToFile(string logDirectory, string loggerId)
            {
                var date = dateTime.ToString("yyyyMMdd");
                var logFileName = logDirectory + string.Format(string.IsNullOrEmpty(loggerId) ? "{0}.log" : "{0}_{1}.log", date, loggerId);
                using (var streamWriter = new StreamWriter(logFileName, true))
                {
                    var time = dateTime.ToString("HH:mm:ss.fff");
                    var line = string.Format("{0}\t{1}\t{2}\t{3}", time, level, threadId, message);
                    streamWriter.WriteLine(line);
                }
            }
        }

        public const Logger Null = null;

        private string id;
        private Targets targets;
        private object lockObject;
        private Queue<Entry> entries;
        private Thread thread;
        private bool isRunning;
        private string logDirectory;

        private void Run()
        {
            while (true)
            {
                lock (lockObject)
                {
                    if (entries.Count == 0)
                    {
                        if (!isRunning)
                            break;
                    }
                    else
                    {
                        try
                        {
                            var entry = entries.Dequeue();
                            if (targets.HasFlag(Targets.Console))
                                entry.WriteToConsole();
                            if (targets.HasFlag(Targets.File))
                                entry.WriteToFile(logDirectory, id);
                        }
                        catch (Exception exception)
                        {
                            Console.WriteLine(exception);
                            isRunning = false;
                        }
                    }
                }
                Thread.Sleep(10);
            }
        }

        public Logger(string id, Targets targets)
        {
            this.id = id;
            this.targets = targets;
            lockObject = new object();
            entries = new Queue<Entry>();
            thread = null;
            isRunning = false;
            logDirectory = AppDomain.CurrentDomain.BaseDirectory + "\\logs\\";
            Directory.CreateDirectory(logDirectory);
        }

        public void Start()
        {
            lock (lockObject)
            {
                if (isRunning)
                    return;
                isRunning = true;
            }
            thread = new Thread(Run);
            thread.Start();
        }

        public void Stop()
        {
            lock (lockObject)
            {
                if (!isRunning)
                    return;
                isRunning = false;
            }
            thread.Join();
            thread = null;
        }

        public void Log(Level level, string message)
        {
            lock (lockObject)
            {
                if (!isRunning)
                    return;
                var timeStamp = DateTime.Now;
                var threadId = Thread.CurrentThread.ManagedThreadId;
                var entry = new Entry(timeStamp, level, threadId, message);
                entries.Enqueue(entry);
            }
        }
        public void Debug(string message) { Log(Level.Debug, message); }
        public void Info(string message) { Log(Level.Info, message); }
        public void Warn(string message) { Log(Level.Warn, message); }
        public void Error(string message) { Log(Level.Error, message); }
        public void Fatal(string message) { Log(Level.Fatal, message); }

        public void Log(Level level, string format, params object[] args) { Log(level, string.Format(format, args)); }
        public void Debug(string format, params object[] args) { Log(Level.Debug, format, args); }
        public void Info(string format, params object[] args) { Log(Level.Info, format, args); }
        public void Warn(string format, params object[] args) { Log(Level.Warn, format, args); }
        public void Error(string format, params object[] args) { Log(Level.Error, format, args); }
        public void Fatal(string format, params object[] args) { Log(Level.Fatal, format, args); }

        public void Log(Level level, object obj) { Log(level, obj.ToString()); }
        public void Debug(object obj) { Log(Level.Debug, obj); }
        public void Info(object obj) { Log(Level.Info, obj); }
        public void Warn(object obj) { Log(Level.Warn, obj); }
        public void Error(object obj) { Log(Level.Error, obj); }
        public void Fatal(object obj) { Log(Level.Fatal, obj); }
    }
}