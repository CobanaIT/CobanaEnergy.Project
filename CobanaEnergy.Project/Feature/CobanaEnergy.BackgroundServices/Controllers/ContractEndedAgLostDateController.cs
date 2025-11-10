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
    /// Controller for processing contract ended Ag Lost date contracts
    /// Updates contracts from "Renewal Window - Ag Lost" to "Contract Ended - Ag Lost" where CED == CurrentDate
    /// </summary>
    [RoutePrefix("api/contract-ended-ag-lost-date")]
    public class ContractEndedAgLostDateController : BaseApiController
    {
        private readonly ICommandHandler<ProcessContractEndedAgLostDateCommand, ProcessContractEndedDateResult> _commandHandler;

        public ContractEndedAgLostDateController(
            ApplicationDBContext db,
            ILoggerService logger,
            ICommandHandler<ProcessContractEndedAgLostDateCommand, ProcessContractEndedDateResult> commandHandler)
            : base(db, logger)
        {
            _commandHandler = commandHandler ?? throw new ArgumentNullException(nameof(commandHandler));
        }

        /// <summary>
        /// Processes contract ended Ag Lost date contracts and updates them to "Contract Ended - Ag Lost"
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
                _logger.LogToFile("=== Processing Contract Ended Ag Lost Date Contracts - Process Started ===");
                _logger.LogToFile($"📋 Source Status: Renewal Window - Ag Lost");
                _logger.LogToFile($"📋 Target Status: Contract Ended - Ag Lost");
                _logger.LogToFile($"📋 Current Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                
                var command = new ProcessContractEndedAgLostDateCommand
                {
                    CurrentDate = DateTime.Now,
                    SourceStatus = "Renewal Window - Ag Lost",
                    TargetStatus = "Contract Ended - Ag Lost"
                };

                var result = await _commandHandler.HandleAsync(command);

                var executionTime = DateTime.Now - startTime;
                _logger.LogSummary(result.UpdatedCount, result.TotalSourceContracts, executionTime);

                return Ok(ResponseHelper.Success(result, 
                    $"Successfully updated {result.UpdatedCount} contracts to Contract Ended - Ag Lost"));
            }
            catch (Exception ex)
            {
                _logger.LogError("Process failed", ex);
                return HandleException(ex, "Error processing contract ended Ag Lost date contracts");
            }
        }
    }
}

