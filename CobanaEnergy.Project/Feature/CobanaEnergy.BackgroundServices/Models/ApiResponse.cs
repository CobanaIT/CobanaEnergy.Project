using System;
using System.Collections.Generic;

namespace CobanaEnergy.BackgroundServices.Models
{
    /// <summary>
    /// Standard API response wrapper for all Background Services endpoints
    /// </summary>
    /// <typeparam name="T">The type of data being returned</typeparam>
    public class ApiResponse<T>
    {
        /// <summary>
        /// Indicates if the request was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// HTTP status code
        /// </summary>
        public int StatusCode { get; set; }

        /// <summary>
        /// Response message
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// The actual data payload
        /// </summary>
        public T Data { get; set; }

        /// <summary>
        /// List of errors (if any)
        /// </summary>
        public List<string> Errors { get; set; }

        /// <summary>
        /// Timestamp when response was generated
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Request tracking ID for debugging
        /// </summary>
        public string RequestId { get; set; }

        public ApiResponse()
        {
            Timestamp = DateTime.UtcNow;
            Errors = new List<string>();
            RequestId = Guid.NewGuid().ToString();
        }
    }
}

