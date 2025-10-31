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
    /// Controller for processing overdue present month contracts
    /// Identifies contracts where StartDate has passed but still in Processing_Present Month status
    /// </summary>
    [RoutePrefix("api/overdue-present-contracts")]
    public class OverduePresentContractsController : BaseApiController
    {
        private readonly ICommandHandler<ProcessOverdueContractsCommand, ProcessOverdueContractsResult> _commandHandler;

        public OverduePresentContractsController(
            ApplicationDBContext db,
            ILoggerService logger,
            ICommandHandler<ProcessOverdueContractsCommand, ProcessOverdueContractsResult> commandHandler)
            : base(db, logger)
        {
            _commandHandler = commandHandler ?? throw new ArgumentNullException(nameof(commandHandler));
        }

        /// <summary>
        /// Processes present month contracts and identifies overdue ones
        /// Overdue contracts are those where CurrentDate > StartDate/InitialStartDate
        /// </summary>
        /// <returns>Result containing the number of overdue contracts identified and inserted</returns>
        [HttpPost]
        [Route("process")]
        public async Task<IHttpActionResult> Process()
        {
            try
            {
                var command = new ProcessOverdueContractsCommand
                {
                    CurrentDate = DateTime.Now,
                    SourceStatus = "Processing_Present Month"
                };

                var result = await _commandHandler.HandleAsync(command);

                return Ok(ResponseHelper.Success(result,
                    $"Successfully identified {result.OverdueCount} overdue contracts. Inserted {result.InsertedCount} new records."));
            }
            catch (Exception ex)
            {
                return HandleException(ex, "Error processing overdue contracts");
            }
        }

        /// <summary>
        /// Processes present month contracts with a custom date (for testing or manual processing)
        /// </summary>
        /// <param name="processDate">The date to use for comparison</param>
        /// <returns>Result containing the number of overdue contracts identified and inserted</returns>
        [HttpPost]
        [Route("process/{processDate:datetime}")]
        public async Task<IHttpActionResult> ProcessWithDate(DateTime processDate)
        {
            try
            {
                var command = new ProcessOverdueContractsCommand
                {
                    CurrentDate = processDate,
                    SourceStatus = "Processing_Present Month"
                };

                var result = await _commandHandler.HandleAsync(command);

                return Ok(ResponseHelper.Success(result,
                    $"Successfully processed contracts for {processDate:yyyy-MM-dd}. " +
                    $"Identified {result.OverdueCount} overdue contracts, inserted {result.InsertedCount} new records."));
            }
            catch (Exception ex)
            {
                return HandleException(ex, $"Error processing overdue contracts for date {processDate:yyyy-MM-dd}");
            }
        }

        
    }
}


