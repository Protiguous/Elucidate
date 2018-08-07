﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using NLog;
using NLog.Targets;

namespace Elucidate.Logging
{
    internal static class Log
    {
        public static Queue<LogString> LogDisplayQueue { get; } = new Queue<LogString>();
        public static void ProcessLogQueue()
        {
            while (LogDisplayQueue.Any())
            {
                LogString log = LogDisplayQueue.Dequeue();
                Instance.Info(log.Message);
            }
        }

        public class LogString
        {
            public LogLevels Level;
            public string Message;
        };
        public static void LogMethod(LogLevels level, string message)
        {
            LogDisplayQueue.Enqueue(new LogString
            {
                Level = level,
                Message = message
            });
        }



        public const string DefaultLogLocation = @"${specialfolder:folder=CommonApplicationData}/Elucidate/Logs";
        private const string DefaultLogFilename = @"/${date:format=yyyyMMdd}.log";
        private const string DefaultArchiveLogFilename = @"/{###}.log";

        public static Logger Instance { get; private set; }

        public enum LogLevels
        {
            Trace,
            Debug,
            Warn,
            Info
        }

        static Log()
        {
#if !DEBUG
            DisableLogLevel(LogLevels.Trace);
            DisableLogLevel(LogLevels.Debug);
#endif
            if (!string.IsNullOrEmpty(Properties.Settings.Default.NlogFileLocation))
            {
                SetLogPath(Properties.Settings.Default.NlogFileLocation);
            }
            LogManager.ReconfigExistingLoggers();
            Instance = LogManager.GetCurrentClassLogger();
        }

        public static void SetLogPath(string logFilePath = DefaultLogLocation)
        {
            try
            {
                if (LogManager.Configuration.FindTargetByName("file") is FileTarget target)
                {
                    target.FileName = $"{logFilePath}{DefaultLogFilename}";
                    target.ArchiveFileName = $"{logFilePath}{DefaultArchiveLogFilename}";
                }
                LogManager.ReconfigExistingLoggers();
                // persist change
                Properties.Settings.Default.NlogFileLocation = logFilePath == DefaultLogLocation ? string.Empty : logFilePath;
                Properties.Settings.Default.Save();
            }
            catch (Exception ex)
            {
                ExceptionHandler.ReportException(ex);
            }
        }
        
        public static void SetLogLevel(LogLevels logLevel, bool enable)
        {
            LogLevel targetLogLevel;
            switch (logLevel)
            {
                case LogLevels.Trace:
                    targetLogLevel = LogLevel.Trace;
                    break;
                case LogLevels.Debug:
                    targetLogLevel = LogLevel.Debug;
                    break;
                case LogLevels.Warn:
                    targetLogLevel = LogLevel.Warn;
                    break;
                default:
                    return;
            }
            foreach (var rule in LogManager.Configuration.LoggingRules)
            {
                if (enable)
                {
                    rule.EnableLoggingForLevel(targetLogLevel);
                }
                else
                {
                    rule.DisableLoggingForLevel(targetLogLevel);
                }
            }
            LogManager.ReconfigExistingLoggers();
        }
        // ReSharper disable once UnusedMember.Local
        private static void DisableLogLevel(LogLevels logLevel)
        {
            SetLogLevel(logLevel, false);
        }
        // ReSharper disable once UnusedMember.Local
        private static void EnableLogLevel(LogLevels logLevel)
        {
            SetLogLevel(logLevel, true);
        }
        public static void Shutdown()
        {
            Instance.Debug("Shutdown logging");
            LogManager.Shutdown(); // Flush and close down internal threads and timers
        }

    }
}
