using System;
using System.Collections.Generic;
using System.Net;
using CobanaEnergy.BackgroundServices.Models;

namespace CobanaEnergy.BackgroundServices.Helpers
{
    /// <summary>
    /// Helper class to create standardized API responses
    /// </summary>
    public static class ResponseHelper
    {
        /// <summary>
        /// Create a successful response with data
        /// </summary>
        public static ApiResponse<T> Success<T>(T data, string message = "Request completed successfully")
        {
            return new ApiResponse<T>
            {
                Success = true,
                StatusCode = (int)HttpStatusCode.OK,
                Message = message,
                Data = data
            };
        }

        /// <summary>
        /// Create a successful response without data
        /// </summary>
        public static ApiResponse<object> Success(string message = "Request completed successfully")
        {
            return new ApiResponse<object>
            {
                Success = true,
                StatusCode = (int)HttpStatusCode.OK,
                Message = message,
                Data = null
            };
        }

        /// <summary>
        /// Create a created response (201)
        /// </summary>
        public static ApiResponse<T> Created<T>(T data, string message = "Resource created successfully")
        {
            return new ApiResponse<T>
            {
                Success = true,
                StatusCode = (int)HttpStatusCode.Created,
                Message = message,
                Data = data
            };
        }

        /// <summary>
        /// Create a no content response (204)
        /// </summary>
        public static ApiResponse<object> NoContent(string message = "Request completed successfully")
        {
            return new ApiResponse<object>
            {
                Success = true,
                StatusCode = (int)HttpStatusCode.NoContent,
                Message = message,
                Data = null
            };
        }

        /// <summary>
        /// Create a bad request response (400)
        /// </summary>
        public static ApiResponse<object> BadRequest(string message = "Bad request", List<string> errors = null)
        {
            return new ApiResponse<object>
            {
                Success = false,
                StatusCode = (int)HttpStatusCode.BadRequest,
                Message = message,
                Data = null,
                Errors = errors ?? new List<string>()
            };
        }

        /// <summary>
        /// Create a bad request response with single error
        /// </summary>
        public static ApiResponse<object> BadRequest(string message, string error)
        {
            return BadRequest(message, new List<string> { error });
        }

        /// <summary>
        /// Create an unauthorized response (401)
        /// </summary>
        public static ApiResponse<object> Unauthorized(string message = "Unauthorized access")
        {
            return new ApiResponse<object>
            {
                Success = false,
                StatusCode = (int)HttpStatusCode.Unauthorized,
                Message = message,
                Data = null
            };
        }

        /// <summary>
        /// Create a forbidden response (403)
        /// </summary>
        public static ApiResponse<object> Forbidden(string message = "Access forbidden")
        {
            return new ApiResponse<object>
            {
                Success = false,
                StatusCode = (int)HttpStatusCode.Forbidden,
                Message = message,
                Data = null
            };
        }

        /// <summary>
        /// Create a not found response (404)
        /// </summary>
        public static ApiResponse<object> NotFound(string message = "Resource not found")
        {
            return new ApiResponse<object>
            {
                Success = false,
                StatusCode = (int)HttpStatusCode.NotFound,
                Message = message,
                Data = null
            };
        }

        /// <summary>
        /// Create a conflict response (409)
        /// </summary>
        public static ApiResponse<object> Conflict(string message = "Resource conflict", List<string> errors = null)
        {
            return new ApiResponse<object>
            {
                Success = false,
                StatusCode = (int)HttpStatusCode.Conflict,
                Message = message,
                Data = null,
                Errors = errors ?? new List<string>()
            };
        }

        /// <summary>
        /// Create an internal server error response (500)
        /// </summary>
        public static ApiResponse<object> InternalServerError(string message = "Internal server error", Exception ex = null)
        {
            var errors = new List<string>();
            if (ex != null)
            {
                errors.Add(ex.Message);
                if (ex.InnerException != null)
                {
                    errors.Add($"Inner: {ex.InnerException.Message}");
                }
            }

            return new ApiResponse<object>
            {
                Success = false,
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Message = message,
                Data = null,
                Errors = errors
            };
        }

        /// <summary>
        /// Create a custom response with specific status code
        /// </summary>
        public static ApiResponse<T> Custom<T>(
            bool success,
            int statusCode,
            string message,
            T data = default(T),
            List<string> errors = null)
        {
            return new ApiResponse<T>
            {
                Success = success,
                StatusCode = statusCode,
                Message = message,
                Data = data,
                Errors = errors ?? new List<string>()
            };
        }
    }
}

