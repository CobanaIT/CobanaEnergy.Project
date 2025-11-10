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
    /// Controller for processing contract ended Not Renewed date contracts
    /// Updates contracts from "Renewal Window" to "Contract Ended - Not Renewed" where CED == CurrentDate
    /// </summary>
    [RoutePrefix("api/contract-ended-not-renewed-date")]
    public class ContractEndedNotRenewedDateController : BaseApiController
    {
        private readonly ICommandHandler<ProcessContractEndedNotRenewedDateCommand, ProcessContractEndedDateResult> _commandHandler;

        public ContractEndedNotRenewedDateController(
            ApplicationDBContext db,
            ILoggerService logger,
            ICommandHandler<ProcessContractEndedNotRenewedDateCommand, ProcessContractEndedDateResult> commandHandler)
            : base(db, logger)
        {
            _commandHandler = commandHandler ?? throw new ArgumentNullException(nameof(commandHandler));
        }

        /// <summary>
        /// Processes contract ended Not Renewed date contracts and updates them to "Contract Ended - Not Renewed"
        /// Contracts are updated where CED == CurrentDate
        /// </summary>
        /// <returns>Result containing the number of contracts updated</returns>
        [HttpPost]
        [Route("process")]
        public async Task<IHttpActionResult> Process()
        {
            var startTime = DateTime.Now;
            
            try
            {
                _logger.LogToFile("=== Processing Contract Ended Not Renewed Date Contracts - Process Started ===");
                _logger.LogToFile($"📋 Source Status: Renewal Window");
                _logger.LogToFile($"📋 Target Status: Contract Ended - Not Renewed");
                _logger.LogToFile($"📋 Current Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                
                var command = new ProcessContractEndedNotRenewedDateCommand
                {
                    CurrentDate = DateTime.Now,
                    SourceStatus = "Renewal Window",
                    TargetStatus = "Contract Ended - Not Renewed"
                };

                var result = await _commandHandler.HandleAsync(command);

                var executionTime = DateTime.Now - startTime;
                _logger.LogSummary(result.UpdatedCount, result.TotalSourceContracts, executionTime);

                return Ok(ResponseHelper.Success(result, 
                    $"Successfully updated {result.UpdatedCount} contracts to Contract Ended - Not Renewed"));
            }
            catch (Exception ex)
            {
                _logger.LogError("Process failed", ex);
                return HandleException(ex, "Error processing contract ended Not Renewed date contracts");
            }
        }
    }
}

