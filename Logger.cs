using System;
using System.IO;
using System.Text;

namespace IpAddressGetTrayApplication
{
   internal sealed class Logger
   {
      #region Singleton definition

      private static readonly Logger instance;

      // Explicit static constructor to tell C# compiler
      // not to mark type as beforefieldinit

      public static Logger Log { get { return instance; } }

      static Logger()
      {
         instance = new Logger();
      }

      private Logger()
      {
         if(File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logFile.log")))
         {
            int i = 1;
            while(File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, string.Format("logFile{0}.log", i))))
            {
               i++;
            }
            File.Move(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logFile.log"), Path.Combine(AppDomain.CurrentDomain.BaseDirectory, string.Format("logFile{0}.log", i)));
         }         
      }

      #endregion Singleton definition

      internal void WriteLog(string message)
      {
         try
         {
            using(FileStream stream = new FileStream(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logFile.log"), FileMode.Append, FileAccess.Write))
            {
               using(StreamWriter writer = new StreamWriter(stream))
               {
                  StringBuilder sb = new StringBuilder();
                  sb.Append(DateTime.Now.ToString());
                  sb.Append(@" : ");
                  sb.Append(message);
                  writer.WriteLine(sb.ToString());
               }
            }
         }
         catch
         { }
      }

      internal void WriteLog(Exception error)
      {
         try
         {
            StringBuilder sb = new StringBuilder();
            sb.Append(error.Message);
            sb.Append(error.StackTrace);
            while(error.InnerException != null)
            {
               error = error.InnerException;
               sb.Append(error.Message);
               sb.Append(error.StackTrace);
            }
            WriteLog(sb.ToString());
         }
         catch
         { }
      }
   }
}