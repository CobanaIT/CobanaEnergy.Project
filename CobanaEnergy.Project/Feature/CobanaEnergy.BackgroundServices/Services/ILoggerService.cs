using System;

namespace CobanaEnergy.BackgroundServices.Services
{
    /// <summary>
    /// Logger service interface for BackgroundServices
    /// Wraps Logic.Logger static methods with dependency injection
    /// </summary>
    public interface ILoggerService
    {
        /// <summary>
        /// Initializes controller-specific logging for this request
        /// </summary>
        void StartControllerLog(string controllerName);

        /// <summary>
        /// Logs a message to the current controller log file
        /// </summary>
        void LogToFile(string message);

        /// <summary>
        /// Logs errors with optional exception details
        /// </summary>
        void LogError(string errorMessage, Exception ex = null);

        /// <summary>
        /// Logs contract changes with all relevant details
        /// </summary>
        void LogContractChange(string eId, string type, string previousStatus, string newStatus, string reason = "");

        /// <summary>
        /// Logs execution summary at end of process
        /// </summary>
        void LogSummary(int updatedCount, int totalCount, TimeSpan? executionTime = null);
    }
}

