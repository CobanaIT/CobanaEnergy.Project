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
    /// Controller for processing renewal window date contracts
    /// Updates contracts from "Live" to "Renewal Window" where CED is within 180 days
    /// </summary>
    [RoutePrefix("api/renewal-window-date")]
    public class RenewalWindowDateController : BaseApiController
    {
        private readonly ICommandHandler<ProcessRenewalWindowDateCommand, ProcessRenewalWindowDateResult> _commandHandler;

        public RenewalWindowDateController(
            ApplicationDBContext db,
            ILoggerService logger,
            ICommandHandler<ProcessRenewalWindowDateCommand, ProcessRenewalWindowDateResult> commandHandler)
            : base(db, logger)
        {
            _commandHandler = commandHandler ?? throw new ArgumentNullException(nameof(commandHandler));
        }

        /// <summary>
        /// Processes renewal window date contracts and updates them to "Renewal Window"
        /// Contracts are updated where CED is within or equal to 180 days from current date
        /// </summary>
        /// <returns>Result containing the number of contracts updated</returns>
        [HttpPost]
        [Route("process")]
        public async Task<IHttpActionResult> Process()
        {
            var startTime = DateTime.Now;
            
            try
            {
                _logger.LogToFile("=== Processing Renewal Window Date Contracts - Process Started ===");
                _logger.LogToFile($"📋 Source Status: Live");
                _logger.LogToFile($"📋 Target Status: Renewal Window");
                _logger.LogToFile($"📋 Current Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                _logger.LogToFile($"📋 Days Threshold: 180 days");
                
                var command = new ProcessRenewalWindowDateCommand
                {
                    CurrentDate = DateTime.Now,
                    SourceStatus = "Live",
                    TargetStatus = "Renewal Window",
                    DaysThreshold = 180
                };

                var result = await _commandHandler.HandleAsync(command);

                var executionTime = DateTime.Now - startTime;
                _logger.LogSummary(result.UpdatedCount, result.TotalLiveContracts, executionTime);

                return Ok(ResponseHelper.Success(result, 
                    $"Successfully updated {result.UpdatedCount} contracts to Renewal Window"));
            }
            catch (Exception ex)
            {
                _logger.LogError("Process failed", ex);
                return HandleException(ex, "Error processing renewal window date contracts");
            }
        }
    }
}

