using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Logic
{
    public static class Logger
    {
        private static readonly string LogDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LOGS");

        /// <summary>
        /// Existing logging method for main project compatibility
        /// </summary>
        public static void Log(string message, [CallerFilePath] string file = "",
            [CallerMemberName] string member = "", [CallerLineNumber] int line = 0)
        {
            try
            {
                if (!Directory.Exists(LogDirectory))
                {
                    Directory.CreateDirectory(LogDirectory);
                }

                string fileName = $"log_{DateTime.Now:yyyy-MM-dd}.txt";
                string fullPath = Path.Combine(LogDirectory, fileName);

                var logEntry = new StringBuilder();
                logEntry.AppendLine("----- LOG ENTRY -----");
                logEntry.AppendLine($"Time: {DateTime.Now:HH:mm:ss}");
                logEntry.AppendLine($"Message: {message}");
                logEntry.AppendLine($"Location: {Path.GetFileName(file)} -> {member}() [Line {line}]");
                logEntry.AppendLine();

                File.AppendAllText(fullPath, logEntry.ToString());
            }
            catch
            {
                // Suppress exceptions from logging
            }
        }

        // ============================================================================================
        // NEW METHODS - For BackgroundServices controller-specific logging
        // ============================================================================================

        /// <summary>
        /// Starts a new controller-specific log file for this request
        /// Creates unique filename: ControllerName_yyyy-MM-dd_HHmmss.txt
        /// </summary>
        public static string StartControllerLog(string controllerName)
        {
            try
            {
                if (!Directory.Exists(LogDirectory))
                {
                    Directory.CreateDirectory(LogDirectory);
                }

                string fileName = $"{controllerName}_{DateTime.Now:yyyy-MM-dd_HHmmss}.txt";
                string fullPath = Path.Combine(LogDirectory, fileName);
                
                File.AppendAllText(fullPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] === PROCESS START ==={Environment.NewLine}");
                
                return fileName;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Logs a message to a specific log file
        /// </summary>
        public static void LogToFile(string logFileName, string message)
        {
            try
            {
                if (string.IsNullOrEmpty(logFileName)) return;
                
                if (!Directory.Exists(LogDirectory))
                {
                    Directory.CreateDirectory(LogDirectory);
                }
                
                string fullPath = Path.Combine(LogDirectory, logFileName);
                File.AppendAllText(fullPath, $"[{DateTime.Now:HH:mm:ss.fff}] {message}{Environment.NewLine}");
            }
            catch
            {
                // Suppress exceptions
            }
        }

        /// <summary>
        /// Logs errors with optional exception details
        /// </summary>
        public static void LogError(string logFileName, string errorMessage, Exception ex = null)
        {
            try
            {
                if (string.IsNullOrEmpty(logFileName)) return;
                
                LogToFile(logFileName, $"❌ ERROR: {errorMessage}");
                
                if (ex != null)
                {
                    LogToFile(logFileName, $"   Exception Type: {ex.GetType().FullName}");
                    LogToFile(logFileName, $"   Exception Message: {ex.Message}");
                    
                    if (ex.InnerException != null)
                    {
                        LogToFile(logFileName, $"   Inner Exception: {ex.InnerException.Message}");
                    }
                    
                    if (!string.IsNullOrEmpty(ex.StackTrace))
                    {
                        LogToFile(logFileName, $"   Stack Trace: {ex.StackTrace}");
                    }
                }
            }
            catch
            {
                // Suppress exceptions
            }
        }

        /// <summary>
        /// Logs contract changes with all relevant details
        /// </summary>
        public static void LogContractChange(string logFileName, string eId, string type, 
            string previousStatus, string newStatus, string reason = "")
        {
            try
            {
                if (string.IsNullOrEmpty(logFileName)) return;
                
                var changeLog = $"EId: {eId} | Type: {type} | Previous: {previousStatus} | New: {newStatus}";
                if (!string.IsNullOrEmpty(reason))
                {
                    changeLog += $" | Reason: {reason}";
                }
                
                LogToFile(logFileName, changeLog);
            }
            catch
            {
                // Suppress exceptions
            }
        }

        /// <summary>
        /// Logs execution summary at end of process
        /// </summary>
        public static void LogSummary(string logFileName, int updatedCount, int totalCount, TimeSpan? executionTime = null)
        {
            try
            {
                if (string.IsNullOrEmpty(logFileName)) return;
                
                LogToFile(logFileName, "========================================");
                LogToFile(logFileName, "SUMMARY");
                LogToFile(logFileName, $"Updated: {updatedCount} contracts");
                LogToFile(logFileName, $"Total Found: {totalCount} contracts");
                
                if (executionTime.HasValue)
                {
                    LogToFile(logFileName, $"Execution Time: {executionTime.Value.TotalSeconds:F2}s");
                }
                
                LogToFile(logFileName, "========================================");
                LogToFile(logFileName, $"=== PROCESS END ===");
            }
            catch
            {
                // Suppress exceptions
            }
        }
    }
}
