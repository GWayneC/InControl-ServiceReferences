using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace InControlCommunicator
{
    static class Logger
    {
        private static TextWriter GetLogger()
        {
            FileInfo logFIle = new FileInfo("Assistant.log");
            TextWriter writer;
            if (!logFIle.Exists)
            {
                writer = logFIle.CreateText();
            }
            else
            {
                writer = logFIle.AppendText();
            }
            return writer;
        }
        public static void LogInfo(string message)
        {
            //            if (!Assistant.Properties.Settings.Default.Logging) { return; }
            Console.WriteLine(message);
            try
            {
                TextWriter writer = GetLogger();
                writer.WriteLine(String.Format("{0} : {1} :Verbose", DateTime.Now.ToString(), message));
                writer.Close();
            }
            catch
            {
            }
        }
        public static void LogError(string error)
        {
            //            if (!Assistant.Properties.Settings.Default.Logging) { return; }
            Console.WriteLine(error);
            try
            {
                TextWriter writer = GetLogger();
                writer.WriteLine(String.Format("{0} : {1} :Error", DateTime.Now.ToString(), error));
                writer.Close();
            }
            catch
            {
            }
        }
    }
}
