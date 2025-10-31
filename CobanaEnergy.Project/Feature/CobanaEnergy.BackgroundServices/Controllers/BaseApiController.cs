using System;
using System.Net;
using System.Web.Http;
using System.Web.Http.Controllers;
using CobanaEnergy.BackgroundServices.Helpers;
using CobanaEnergy.BackgroundServices.Services;
using CobanaEnergy.Project.Models;

namespace CobanaEnergy.BackgroundServices.Controllers
{
    /// <summary>
    /// Base API controller providing common functionality for all controllers
    /// </summary>
    public abstract class BaseApiController : ApiController
    {
        protected readonly ApplicationDBContext _db;
        protected readonly ILoggerService _logger;

        /// <summary>
        /// Constructor with dependency injection
        /// </summary>
        protected BaseApiController(ApplicationDBContext db, ILoggerService logger)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Initialize is called by Web API framework after controller construction
        /// This is where ControllerContext becomes available
        /// </summary>
        protected override void Initialize(HttpControllerContext controllerContext)
        {
            base.Initialize(controllerContext);
            
            // Initialize logger with controller name now that ControllerContext is available
            var controllerName = ControllerContext?.ControllerDescriptor?.ControllerName ?? "Unknown";
            _logger.StartControllerLog(controllerName);
        }

        /// <summary>
        /// Dispose database context
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _db?.Dispose();
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// Standard exception handling helper
        /// </summary>
        protected IHttpActionResult HandleException(Exception ex, string operation)
        {
            _logger?.LogError($"Error in {operation}: {ex.Message}", ex);

            return Content(HttpStatusCode.InternalServerError,
                ResponseHelper.InternalServerError($"Failed to {operation}", ex));
        }
    }
}

