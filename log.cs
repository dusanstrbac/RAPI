namespace RAPI
{
    public class log
    {
        private const string FILE_EXT=".log";
        private readonly object fileLock = new object();
        private readonly string datetimeFormat;
        private readonly string logFilename;

        /*
         * Pokrenće instancu konstruktora klase log.
         * Ako log datoteka ne postoji, biće kreirana automatski.
         */

        public log()
        {
            datetimeFormat = "yyyy-MM-dd HH:mm:ss.fff";
            logFilename = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name + FILE_EXT;

            // Ako log datoteka ne postoji, biće kreirana automatski.
            string logHeader = logFilename + " is created.";
            if (!System.IO.File.Exists(logFilename))
            {
                WriteLine(System.DateTime.Now.ToString(datetimeFormat) + " " + logHeader);
            }
        }

        /*
         *Zapisuje DEBUG  poruku u datoteku.
         *<param name="text">Message</param>
         */
        public void Debug(string text)
        {
            WriteFormattedLog(LogLevel.DEBUG, text);
        }
        /*
         * zapisuje ERROR poruku u datoteku.
         * <param name="text">Message</param>
         */
        public void Error(string text)
        {
            WriteFormattedLog(LogLevel.ERROR, text);
        }
        /*
         * zapisuje FATAL ERROR poruku u datoteku.
         * <param name="text">Message</param>
         */
        public void Fatal(string text)
        {
            WriteFormattedLog(LogLevel.FATAL, text);
        }
        /*
         *zapisuje INFO poruku u datoteku.
         * <param name="text">Message</param>
         */
        public void Info(string text)
        {
            WriteFormattedLog(LogLevel.INFO, text);
        }
        /*
         * zapisuje TRACE poruku u datoteku.
         *<param name="text">Message</param>
         */
        public void Trace(string text)
        {
            WriteFormattedLog(LogLevel.TRACE, text);
        }
        /*
         *zapisuje WARNING poruku u datoteku.
         *<param name="text">Message</param>
         */
        public void Warning(string text)
        {
            WriteFormattedLog(LogLevel.WARNING, text);
        }

        /* zapisuje jednu liniju teksta u datoteku u datoteku.
         * <param name="level">Message level</param>
         * <param name="text">Message</param>
         */
        private void WriteLine(string text, bool append = false)
        {
            try
            {
                if (string.IsNullOrEmpty(text))
                {
                    return;
                }
                lock (fileLock)
                {
                    using (System.IO.StreamWriter writer = new System.IO.StreamWriter(logFilename, append, System.Text.Encoding.UTF8))
                    {
                        writer.WriteLine(text);
                    }
                }
            }
            catch
            {
                throw;
            }
        }


        /*
         *zapisuje poruku u datoteku.
         *<param name="level">Message level</param>
         *<param name="text">Message</param>
         */
        private void WriteFormattedLog(LogLevel level, string text)
        {
            string pretext;
            switch (level)
            {
                case LogLevel.TRACE:
                    pretext = System.DateTime.Now.ToString(datetimeFormat) + " [TRACE]   ";
                    break;
                case LogLevel.INFO:
                    pretext = System.DateTime.Now.ToString(datetimeFormat) + " [INFO]    ";
                    break;
                case LogLevel.DEBUG:
                    pretext = System.DateTime.Now.ToString(datetimeFormat) + " [DEBUG]   ";
                    break;
                case LogLevel.WARNING:
                    pretext = System.DateTime.Now.ToString(datetimeFormat) + " [WARNING] ";
                    break;
                case LogLevel.ERROR:
                    pretext = System.DateTime.Now.ToString(datetimeFormat) + " [ERROR]   ";
                    break;
                case LogLevel.FATAL:
                    pretext = System.DateTime.Now.ToString(datetimeFormat) + " [FATAL]   ";
                    break;
                default:
                    pretext = "";
                    break;
            }

            WriteLine(pretext + text, true);
        }

        [System.Flags]
        private enum LogLevel
        {
            TRACE,
            INFO,
            DEBUG,
            WARNING,
            ERROR,
            FATAL
        }
    }
}
