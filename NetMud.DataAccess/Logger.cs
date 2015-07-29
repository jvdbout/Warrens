﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.Web.Configuration;
using NetMud.DataAccess;
using NetMud.DataStructure.Base.Entity;
using NetMud.DataStructure.Base.EntityBackingData;
using NetMud.Utility;

namespace NetMud.DataAccess
{
    public enum LogChannels
    {
        CommandUse,
        Restore,
        Backup,
        AccountActivity,
        Authentication
    }

    public static class LoggingUtility
    {
        public static void LogError(Exception ex)
        {
            var errorContent = String.Format("{0}: {1}{2}{3}", ex.GetType().Name, ex.Message, Environment.NewLine, ex.StackTrace);

            CommitLog(errorContent, "SystemError", true);
        }

        public static void LogAdminCommandUsage(string commandString, string accountName)
        {
            var content = String.Format("{0}: {1}", accountName, commandString);

            CommitLog(content, "AdminCommandUse", true);
        }
        public static IEnumerable<string> GetCurrentLogNames()
        {
            var logger = new Logger(WebConfigurationManager.AppSettings["LogPath"]);

            return logger.GetCurrentLogNames();
        }

        public static string GetCurrentLogContent(string channel)
        {
            var logger = new Logger(WebConfigurationManager.AppSettings["LogPath"]);

            return logger.GetCurrentLogContent(channel);
        }

        public static void Log(string content, LogChannels channel, bool keepItQuiet = false)
        {
            CommitLog(content, channel.ToString(), keepItQuiet);
        }

        private static void CommitLog(string content, string channel, bool keepItQuiet)
        {
            var logger = new Logger(WebConfigurationManager.AppSettings["LogPath"]);

            logger.WriteToLog(content, channel, keepItQuiet);
        }
    }


    internal class Logger
    {
        private string BaseDirectory;

        public Logger(string logDirectoryPath)
        {
            BaseDirectory = logDirectoryPath;
        }

        public void WriteToLog(string content, bool beQuiet = false)
        {
            WriteToLog(content, "General", beQuiet);
        }

        public void WriteToLog(string content, string channel, bool beQuiet = false)
        {
            //Write to the log file first
            WriteToFile(content, channel);

            //Quiet means only write to file, non-quiet means write to whomever is subscribed
            if (!beQuiet)
            {
                //write to people in game
                var peeps = LiveCache.GetAll<IPlayer>().Where(peep => ((ICharacter)peep.DataTemplate).Account.LogChannelSubscriptions.Contains(channel));

                foreach (var peep in peeps)
                    peep.WriteTo(new string[] { content.EncapsulateOutput() });

                //TODO: Write to some source that can push to the web
            }
        }

        public IEnumerable<string> GetCurrentLogNames()
        {
            var names = Enumerable.Empty<string>();

            if (!String.IsNullOrWhiteSpace(BaseDirectory) && Directory.Exists(BaseDirectory) && Directory.Exists(BaseDirectory + "Current/"))
                names = Directory.EnumerateFiles(BaseDirectory + "Current/", "*.txt", SearchOption.TopDirectoryOnly);

            return names.Select(nm => nm.Substring(nm.LastIndexOf('/') + 1, nm.Length - nm.LastIndexOf('/') - 5));
        }

        public string GetCurrentLogContent(string channel)
        {
            var content = String.Empty;

            if (!String.IsNullOrWhiteSpace(BaseDirectory) 
                && Directory.Exists(BaseDirectory) 
                && Directory.Exists(BaseDirectory + "Current/") 
                && File.Exists(BaseDirectory + "Current/" + channel + ".txt"))
            { 
                using (var logFile = File.Open(BaseDirectory + "Current/" + channel + ".txt", FileMode.Open))
                {
                    byte[] bytes = new byte[logFile.Length];
                    logFile.Read(bytes, 0, (int)logFile.Length);
                    content = Encoding.UTF8.GetString(bytes);
                }
            }

            return content;
        }

        private void WriteToFile(string content, string channel)
        {
            FileStream thisLog = null;

            try
            {
                //Bail, why is there no base directory?
                if (String.IsNullOrWhiteSpace(BaseDirectory))
                    return;

                if (!Directory.Exists(BaseDirectory))
                    Directory.CreateDirectory(BaseDirectory);

                var currentDirectory = BaseDirectory + "Current/";
                if (!Directory.Exists(currentDirectory))
                    Directory.CreateDirectory(currentDirectory);

                if (!File.Exists(currentDirectory + channel + ".txt"))
                    thisLog = File.Create(currentDirectory + channel + ".txt");
                else
                    thisLog = File.Open(currentDirectory + channel + ".txt", FileMode.Append);
                //Add a line terminator PLEASE
                content += Environment.NewLine;
                var timeStamp = String.Format("[{0:0000}/{1:00}/{2:00} {3:00}:{4:00}:{5:00}]:  ", DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second);

                var bytes = Encoding.UTF8.GetBytes(timeStamp + content);
                thisLog.Write(bytes, 0, bytes.Length);

                //Don't forget to write the file out
                thisLog.Flush();
            }
            catch
            {
                //dont throw on trying to write the log
            }
            finally
            {
                //dont not do this everEVERVERRFCFEVVEEV
                if (thisLog != null)
                    thisLog.Dispose();
            }
        }
    }
}