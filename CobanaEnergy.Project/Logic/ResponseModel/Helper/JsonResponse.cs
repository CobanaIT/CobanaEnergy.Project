using CobanaEnergy.Project.Models.ResponseModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http.Results;
using System.Web.Mvc;

namespace Logic.ResponseModel.Helper
{
    public static class JsonResponse
    {
        public static System.Web.Mvc.JsonResult Ok(object data = null, string message = "success")
        {
            return new System.Web.Mvc.JsonResult
            {
                Data = StandardResponse.Ok(message, data),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet
            };
        }

        public static JsonResult Fail(string message = "Something went wrong", int code = 400)
        {
            return new JsonResult
            {
                Data = StandardResponse.Error(message, code),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet
            };
        }

        public static JsonResult FailRedirection(object data = null, string message = "Something went wrong", int code = 400)
        {
            return new JsonResult
            {
                Data = StandardResponse.ErrorRedirection(message, code,data),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet
            };
        }
    }
}
