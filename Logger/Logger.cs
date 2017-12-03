using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileLogger
{
    public class Logger
    {
        private static Logger instance;
        private string test = "logger.log";
        private string DatetimeFormat = "HH:mm:ss";

        private static object syncRoot = new Object();
        private static object syncWrite = new Object();


        public static Logger getInstance()
        {
            if (instance == null)
            {
                lock (syncRoot)
                {
                    if (instance == null)
                        instance = new Logger();
                }
            }
            return instance;
        }

        public void LogInfo(string text)
        {
            SyncWriteLog(" INFO: " + text);
        }

        public void LogWarning(string text)
        {
            SyncWriteLog(" WARNING: " + text);
        }

        public void LogError(string text)
        {
            SyncWriteLog(" ERROR: " + text);
        }

        private void SyncWriteLog(string text)
        {
            lock (syncWrite)
            {
                using (StreamWriter writer = new StreamWriter(test, true))
                {
                    writer.WriteLine(DateTime.Now.ToString(DatetimeFormat) + text);
                }
            }
        }
    }
}
