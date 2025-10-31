using Logic;
using System;

namespace CobanaEnergy.BackgroundServices.Services
{
    /// <summary>
    /// Logger service implementation that wraps Logic.Logger static methods
    /// Manages controller-specific log files per request
    /// </summary>
    public class LoggerService : ILoggerService
    {
        private string _logFileName;

        /// <summary>
        /// Initializes controller-specific logging for this request
        /// </summary>
        public void StartControllerLog(string controllerName)
        {
            if (string.IsNullOrEmpty(controllerName))
            {
                controllerName = "Unknown";
            }

            _logFileName = Logger.StartControllerLog(controllerName);
        }

        /// <summary>
        /// Logs a message to the current controller log file
        /// </summary>
        public void LogToFile(string message)
        {
            if (string.IsNullOrEmpty(_logFileName))
            {
                return; // Log not initialized
            }

            Logger.LogToFile(_logFileName, message);
        }

        /// <summary>
        /// Logs errors with optional exception details
        /// </summary>
        public void LogError(string errorMessage, Exception ex = null)
        {
            if (string.IsNullOrEmpty(_logFileName))
            {
                return; // Log not initialized
            }

            Logger.LogError(_logFileName, errorMessage, ex);
        }

        /// <summary>
        /// Logs contract changes with all relevant details
        /// </summary>
        public void LogContractChange(string eId, string type, string previousStatus, string newStatus, string reason = "")
        {
            if (string.IsNullOrEmpty(_logFileName))
            {
                return; // Log not initialized
            }

            Logger.LogContractChange(_logFileName, eId, type, previousStatus, newStatus, reason);
        }

        /// <summary>
        /// Logs execution summary at end of process
        /// </summary>
        public void LogSummary(int updatedCount, int totalCount, TimeSpan? executionTime = null)
        {
            if (string.IsNullOrEmpty(_logFileName))
            {
                return; // Log not initialized
            }

            Logger.LogSummary(_logFileName, updatedCount, totalCount, executionTime);
        }
    }
}

