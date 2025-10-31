using CobanaEnergy.Project.Models;
using System;
using System.Net;
using System.Threading.Tasks;
using System.Web.Http;
using CobanaEnergy.BackgroundServices.Models;
using CobanaEnergy.BackgroundServices.Helpers;
using CobanaEnergy.BackgroundServices.CQRS.Commands;
using CobanaEnergy.BackgroundServices.CQRS.Handlers.Commands;
using CobanaEnergy.BackgroundServices.Services;

namespace CobanaEnergy.BackgroundServices.Controllers
{
    /// <summary>
    /// Controller for processing future contracts and updating them to present month status.
    /// </summary>
    [RoutePrefix("api/processing-present-contracts")]
    public class ProcessingPresentContractsController : BaseApiController
    {
        private readonly ICommandHandler<ProcessFutureContractsCommand, ProcessContractsResult> _commandHandler;
        
        public ProcessingPresentContractsController(
            ApplicationDBContext db,
            ILoggerService logger,
            ICommandHandler<ProcessFutureContractsCommand, ProcessContractsResult> commandHandler) 
            : base(db, logger)
        {
            _commandHandler = commandHandler ?? throw new ArgumentNullException(nameof(commandHandler));
        }

        /// <summary>
        /// Processes contracts from Processing_Future Months to Processing_Present Month.
        /// Only processes contracts where StartDate or InitialStartDate matches the current month.
        /// </summary>
        /// <returns>Result containing the number of contracts updated</returns>
        [HttpPost]
        [Route("process")]
        public async Task<IHttpActionResult> Process()
        {
            var startTime = DateTime.Now;
            
            try
            {
                _logger.LogToFile("=== Processing Present Contracts - Process Started ===");
                _logger.LogToFile($"📋 Target Status: Processing_Present Month");
                _logger.LogToFile($"📋 Current Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                
                var command = new ProcessFutureContractsCommand
                {
                    CurrentDate = DateTime.Now,
                    TargetStatus = "Processing_Present Month"
                };

                var result = await _commandHandler.HandleAsync(command);

                var executionTime = DateTime.Now - startTime;
                _logger.LogSummary(result.UpdatedCount, result.TotalFutureContracts, executionTime);

                return Ok(ResponseHelper.Success(result, 
                    $"Successfully updated {result.UpdatedCount} contracts to Processing_Present Month"));
            }
            catch (Exception ex)
            {
                _logger.LogError("Process failed", ex);
                return HandleException(ex, "Error processing contracts");
            }
        }
    }
}

