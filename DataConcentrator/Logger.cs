using System;
using System.IO;

namespace DataConcentrator
{
    public static class Logger
    {
        [Flags]
        public enum LogType
        {
            TagCUD           = 1,
            TagUpdate        = 2,
            AlarmCUD         = 4,
            AlarmAcknowledge = 8,
            ImportExport     = 16,
            Login            = 32,
            Error            = 64
        }

        private static int traceWord = 0;
        private static readonly string LogPath = "system.log";
        private static readonly string TraceWordPath = "traceword.cfg";
        private static readonly object Locker = new object();

        public static int TraceWord
        {
            get { return traceWord; }
            set { traceWord = value; SaveTraceWord(); }
        }

        static Logger()
        {
            LoadTraceWord();
        }

        public static void Log(LogType type, string message)
        {
            if ((traceWord & (int)type) == 0)
                return;

            lock (Locker)
            {
                File.AppendAllText(LogPath,
                    $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{type}] {message}{Environment.NewLine}");
            }
        }

        private static void SaveTraceWord()
        {
            try { File.WriteAllText(TraceWordPath, traceWord.ToString()); }
            catch { }
        }

        private static void LoadTraceWord()
        {
            try
            {
                if (File.Exists(TraceWordPath) &&
                    int.TryParse(File.ReadAllText(TraceWordPath).Trim(), out int val))
                    traceWord = val;
            }
            catch { }
        }
    }
}
