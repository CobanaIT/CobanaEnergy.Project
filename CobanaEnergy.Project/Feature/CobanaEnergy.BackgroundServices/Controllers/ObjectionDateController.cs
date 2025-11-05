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
    /// Controller for processing objection date contracts
    /// Updates contracts to "Objection Closed" where objectionDate + 1 day = CurrentDate
    /// </summary>
    [RoutePrefix("api/objection-date")]
    public class ObjectionDateController : BaseApiController
    {
        private readonly ICommandHandler<ProcessObjectionDateCommand, ProcessObjectionDateResult> _commandHandler;

        public ObjectionDateController(
            ApplicationDBContext db,
            ILoggerService logger,
            ICommandHandler<ProcessObjectionDateCommand, ProcessObjectionDateResult> commandHandler)
            : base(db, logger)
        {
            _commandHandler = commandHandler ?? throw new ArgumentNullException(nameof(commandHandler));
        }

        /// <summary>
        /// Processes objection date contracts and updates them to "Objection Closed"
        /// Contracts are updated where objectionDate + 1 day = CurrentDate
        /// </summary>
        /// <returns>Result containing the number of contracts updated</returns>
        [HttpPost]
        [Route("process")]
        public async Task<IHttpActionResult> Process()
        {
            var startTime = DateTime.Now;
            
            try
            {
                _logger.LogToFile("=== Processing Objection Date Contracts - Process Started ===");
                _logger.LogToFile($"📋 Source Status: Objection");
                _logger.LogToFile($"📋 Target Status: Objection Closed");
                _logger.LogToFile($"📋 Current Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                
                var command = new ProcessObjectionDateCommand
                {
                    CurrentDate = DateTime.Now,
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
                return HandleException(ex, "Error processing objection date contracts");
            }
        }
    }
}

