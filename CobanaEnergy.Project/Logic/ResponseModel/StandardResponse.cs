using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CobanaEnergy.Project.Models.ResponseModel
{
    public class StandardResponse
    {
        public bool success { get; set; }
        public int StatusCode { get; set; }
        public string message { get; set; }
        public object Data { get; set; }

        public static StandardResponse Ok(string message = null, object data = null)
        {
            return new StandardResponse
            {
                success = true,
                StatusCode = 200,
                message = message,
                Data = data
            };
        }

        public static StandardResponse Error(string message, int statusCode = 400)
        {
            return new StandardResponse
            {
                success = false,
                StatusCode = statusCode,
                message = message,
                Data = null
            };
        }

        public static StandardResponse ErrorRedirection(string message, int statusCode = 400, object data = null)
        {
            return new StandardResponse
            {
                success = false,
                StatusCode = statusCode,
                message = message,
                Data = data
            };
        }
    }
}