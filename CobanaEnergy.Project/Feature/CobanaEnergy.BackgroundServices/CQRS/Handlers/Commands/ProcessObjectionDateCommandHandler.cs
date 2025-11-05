using CobanaEnergy.Project.Models;
using CobanaEnergy.BackgroundServices.CQRS.Commands;
using CobanaEnergy.BackgroundServices.CQRS.Queries;
using CobanaEnergy.BackgroundServices.CQRS.Handlers.Queries;
using CobanaEnergy.BackgroundServices.Models;
using CobanaEnergy.BackgroundServices.Services;
using CobanaEnergy.BackgroundServices.Helpers;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace CobanaEnergy.BackgroundServices.CQRS.Handlers.Commands
{
    /// <summary>
    /// [CQRS ARCHITECTURE - PURE READ/WRITE SEPARATION]
    /// Handler for processing objection date contracts
    /// Updates contracts to "Objection Closed" where objectionDate + 1 day = CurrentDate
    /// Uses query handlers for all read operations, focuses on write logic
    /// </summary>
    public class ProcessObjectionDateCommandHandler 
        : ICommandHandler<ProcessObjectionDateCommand, ProcessObjectionDateResult>
    {
        private readonly ApplicationDBContext _db;
        
        // Inject query handlers for all read operations
        private readonly IQueryHandler<GetContractsByStatusQuery, List<ContractBasicInfo>> _getContractsQuery;
        private readonly IQueryHandler<GetPostSaleObjectionByEIdQuery, PostSaleObjectionDto> _getObjectionQuery;
        private readonly ILoggerService _logger;

        public ProcessObjectionDateCommandHandler(
            ApplicationDBContext db,
            IQueryHandler<GetContractsByStatusQuery, List<ContractBasicInfo>> getContractsQuery,
            IQueryHandler<GetPostSaleObjectionByEIdQuery, PostSaleObjectionDto> getObjectionQuery,
            ILoggerService logger)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _getContractsQuery = getContractsQuery ?? throw new ArgumentNullException(nameof(getContractsQuery));
            _getObjectionQuery = getObjectionQuery ?? throw new ArgumentNullException(nameof(getObjectionQuery));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Handles the command to process objection date contracts
        /// </summary>
        public async Task<ProcessObjectionDateResult> HandleAsync(ProcessObjectionDateCommand command)
        {
            var startTime = DateTime.Now;
            
            try
            {
                _logger.LogToFile("=== Handler: ProcessObjectionDateCommandHandler Started ===");
                
                ValidateCommand(command);

                var result = new ProcessObjectionDateResult
                {
                    ProcessedAt = DateTime.Now,
                    UpdatedEIds = new List<string>()
                };

                _logger.LogToFile($"📋 Processing objection date contracts for date: {command.CurrentDate:yyyy-MM-dd}");
                _logger.LogToFile($"📋 Source Status: {command.SourceStatus}");
                _logger.LogToFile($"📋 Target Status: {command.TargetStatus}");

                // Step 1: Get All Eids + Type from CE_ContractStatuses with ContractStatus == "Objection"
                _logger.LogToFile("📋 Step 1: Fetching contracts with status 'Objection'...");
                var contractsQuery = new GetContractsByStatusQuery
                {
                    ContractStatus = command.SourceStatus
                };
                var objectionContracts = await _getContractsQuery.HandleAsync(contractsQuery);

                result.TotalObjectionContracts = objectionContracts.Count;
                _logger.LogToFile($"📋 Step 1 Complete: Found {objectionContracts.Count} contracts with status '{command.SourceStatus}'");

                if (objectionContracts.Count == 0)
                {
                    _logger.LogToFile("📋 No contracts to process - exiting early");
                    return result; // No contracts to process
                }

                var contractsToUpdate = new List<ContractBasicInfo>();

                // Step 2: Check each contract against CE_PostSaleObjection where objectionDate + 1 day = CurrentDate
                _logger.LogToFile("📋 Step 2: Checking each contract against objectionDate criteria...");
                int checkedCount = 0;
                foreach (var contract in objectionContracts)
                {
                    checkedCount++;
                    bool shouldUpdate = await ShouldUpdateContractAsync(contract.EId, contract.Type, command.CurrentDate);

                    if (shouldUpdate)
                    {
                        _logger.LogToFile($"   ✓ EId {contract.EId} ({contract.Type}) - ObjectionDate + 1 day matches current date");
                        contractsToUpdate.Add(contract);
                    }
                    else
                    {
                        if (checkedCount % 50 == 0) // Log progress every 50 contracts
                        {
                            _logger.LogToFile($"   Progress: Checked {checkedCount}/{objectionContracts.Count} contracts...");
                        }
                    }
                }

                result.MatchedObjectionDateCount = contractsToUpdate.Count;
                _logger.LogToFile($"📋 Step 2 Complete: {contractsToUpdate.Count} contracts matched objectionDate criteria");

                // Step 3: Update contract statuses to "Objection Closed"
                if (contractsToUpdate.Count > 0)
                {
                    _logger.LogToFile($"📋 Step 3: Updating {contractsToUpdate.Count} contracts to '{command.TargetStatus}'...");
                    await UpdateContractStatusesAsync(contractsToUpdate, command.TargetStatus, result);
                    _logger.LogToFile($"📋 Step 3 Complete: Successfully updated {result.UpdatedCount} contracts");
                }
                else
                {
                    _logger.LogToFile("📋 Step 3: No contracts to update");
                }

                var executionTime = DateTime.Now - startTime;
                _logger.LogToFile($"📋 Handler execution completed in {executionTime.TotalSeconds:F2}s");

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError("Handler execution failed", ex);
                throw;
            }
        }

        /// <summary>
        /// Validates the command before processing
        /// </summary>
        private void ValidateCommand(ProcessObjectionDateCommand command)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            if (command.CurrentDate == default)
            {
                throw new ArgumentException("CurrentDate must be set", nameof(command.CurrentDate));
            }

            if (string.IsNullOrWhiteSpace(command.SourceStatus))
            {
                throw new ArgumentException("SourceStatus cannot be empty", nameof(command.SourceStatus));
            }

            if (string.IsNullOrWhiteSpace(command.TargetStatus))
            {
                throw new ArgumentException("TargetStatus cannot be empty", nameof(command.TargetStatus));
            }
        }

        /// <summary>
        /// Determines if a contract should be updated based on objectionDate + 1 day = CurrentDate
        /// Uses Query Handler for read operations (CQRS pattern)
        /// </summary>
        private async Task<bool> ShouldUpdateContractAsync(string eId, string type, DateTime currentDate)
        {
            // Use Query Handler to get PostSaleObjection record by EId
            var objectionQuery = new GetPostSaleObjectionByEIdQuery 
            { 
                EId = eId,
                ContractType = type 
            };
            var objectionRecord = await _getObjectionQuery.HandleAsync(objectionQuery);

            if (!objectionRecord.Found || string.IsNullOrEmpty(objectionRecord.ObjectionDate))
            {
                return false;
            }

            // Parse the objection date
            if (Helper.TryParseStartDate(objectionRecord.ObjectionDate, out DateTime objectionDate))
            {
                // Check if objectionDate + 1 day = CurrentDate
                // i.e., objectionDate was one day before current date
                var expectedDate = objectionDate.Date.AddDays(1);
                return expectedDate == currentDate.Date;
            }

            return false;
        }

        /// <summary>
        /// Updates the contract statuses for all matching contracts
        /// </summary>
        private async Task UpdateContractStatusesAsync(List<ContractBasicInfo> contractsToUpdate, string targetStatus, ProcessObjectionDateResult result)
        {
            var eidsToUpdate = contractsToUpdate.Select(c => c.EId).ToList();
            
            var contractsToModify = await _db.CE_ContractStatuses
                .Where(cs => eidsToUpdate.Contains(cs.EId))
                .ToListAsync();

            _logger.LogToFile($"📋 Found {contractsToModify.Count} contract records to update in database");

            foreach (var contract in contractsToModify)
            {
                var previousStatus = contract.ContractStatus;
                contract.ContractStatus = targetStatus;
                contract.ModifyDate = DateTime.Now;
                result.UpdatedEIds.Add(contract.EId);
                
                // Log each contract change
                _logger.LogContractChange(
                    eId: contract.EId,
                    type: contract.Type ?? "Unknown",
                    previousStatus: previousStatus,
                    newStatus: targetStatus,
                    reason: "ObjectionDate + 1 day matches current date"
                );
            }

            result.UpdatedCount = await _db.SaveChangesAsync();
            _logger.LogToFile($"📋 Database save completed: {result.UpdatedCount} rows affected");
        }
    }
}

