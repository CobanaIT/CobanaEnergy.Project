using CobanaEnergy.Project.Models;
using CobanaEnergy.BackgroundServices.CQRS.Commands;
using CobanaEnergy.BackgroundServices.CQRS.Handlers.Commands;
using CobanaEnergy.BackgroundServices.Models;
using CobanaEnergy.BackgroundServices.Helpers;
using CobanaEnergy.BackgroundServices.Services;
using System;
using System.Net;
using System.Threading.Tasks;
using System.Web.Http;

namespace CobanaEnergy.BackgroundServices.Controllers
{
    /// <summary>
    /// Controller for processing objection count contracts
    /// Updates contracts to "Objection Closed" where ObjectionCount == MaxObjectionCount for the supplier
    /// </summary>
    [RoutePrefix("api/objection-count")]
    public class ObjectionCountController : BaseApiController
    {
        private readonly ICommandHandler<ProcessObjectionCountCommand, ProcessObjectionCountResult> _commandHandler;

        public ObjectionCountController(
            ApplicationDBContext db,
            ILoggerService logger,
            ICommandHandler<ProcessObjectionCountCommand, ProcessObjectionCountResult> commandHandler)
            : base(db, logger)
        {
            _commandHandler = commandHandler ?? throw new ArgumentNullException(nameof(commandHandler));
        }

        /// <summary>
        /// Processes objection count contracts and updates them to "Objection Closed"
        /// Contracts are updated where ObjectionCount == MaxObjectionCount for the supplier
        /// </summary>
        /// <returns>Result containing the number of contracts updated</returns>
        [HttpPost]
        [Route("process")]
        public async Task<IHttpActionResult> Process()
        {
            var startTime = DateTime.Now;
            
            try
            {
                _logger.LogToFile("=== Processing Objection Count Contracts - Process Started ===");
                _logger.LogToFile($"📋 Source Status: Objection");
                _logger.LogToFile($"📋 Target Status: Objection Closed");
                _logger.LogToFile($"📋 Current Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                
                var command = new ProcessObjectionCountCommand
                {
                    SourceStatus = "Objection",
                    TargetStatus = "Objection Closed"
                };

                var result = await _commandHandler.HandleAsync(command);

                var executionTime = DateTime.Now - startTime;
                _logger.LogSummary(result.UpdatedCount, result.TotalObjectionContracts, executionTime);

                return Ok(ResponseHelper.Success(result, 
                    $"Successfully updated {result.UpdatedCount} contracts to Objection Closed"));
            }
            catch (Exception ex)
            {
                _logger.LogError("Process failed", ex);
                return HandleException(ex, "Error processing objection count contracts");
            }
        }
    }
}



