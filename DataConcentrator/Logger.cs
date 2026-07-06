using System;
using System.IO;

namespace DataConcentrator
{
    public static class Logger
    {
            [Flags] // vrijednosti se mogu kombinovato
            public enum LogType
        {
            TagCUD           = 1,   // create/update/delete
            TagUpdate        = 2,   // promena vrednosti (scan/write)
            AlarmCUD         = 4,   // dodavanje/izmena/brisanje/aktivacija alarma
            AlarmAcknowledge = 8,   // potvrda alarma
            ImportExport     = 16,  // uvoz/izvoz koniguracije
            Login            = 32,  // prijava/odjava
            Error            = 64   //greska u aplikaciji
        }

        private const int AllLogTypes =
            (int)LogType.TagCUD | (int)LogType.TagUpdate | (int)LogType.AlarmCUD |
            (int)LogType.AlarmAcknowledge | (int)LogType.ImportExport | (int)LogType.Login | (int)LogType.Error;

        // po defaultu se loguje sve (osnovni zahtjev: svaka akcija se cuva u system.log)
        // ako vec postoji traceword.cfg, ta vrednost ce ga prepisati u LoadTraceWord()
        private static int traceWord = AllLogTypes;

        private static readonly string LogPath = "system.log";
        private static readonly string TraceWordPath = "traceword.cfg";
        private static readonly object Locker = new object();

        // cuva vrednosti
        public static int TraceWord
        {
            get { return traceWord; }
            set { traceWord = value; SaveTraceWord(); } // cuva promjenu
        }

        // poziva se pri prvom koriscenju klase - cita prethodno sacuvani TraceWord
        static Logger()
        {
            LoadTraceWord();
        }

        // snima u log fajl, ako je traceword postavljen
        public static void Log(LogType type, string message)
        {
            if ((traceWord & (int)type) == 0)   // da li je postavljen
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
            catch { }   // da ne dodje do pucanja ako je disk pun ili fajl zakljucan
        }

        private static void LoadTraceWord()
        {
            try
            {
                if (File.Exists(TraceWordPath) &&
                    int.TryParse(File.ReadAllText(TraceWordPath).Trim(), out int val))
                    traceWord = val;    // smo se desava ako su oba uslova ispunjena, inace stoji samo 0
            }
            catch { }
        }
    }
}
