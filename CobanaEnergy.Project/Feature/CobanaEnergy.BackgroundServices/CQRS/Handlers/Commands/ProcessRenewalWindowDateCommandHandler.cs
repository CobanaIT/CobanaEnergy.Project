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
    /// Handler for processing renewal window date contracts
    /// Updates contracts from "Live" to "Renewal Window" where CED is within 180 days
    /// Uses query handlers for all read operations, focuses on write logic
    /// </summary>
    public class ProcessRenewalWindowDateCommandHandler 
        : ICommandHandler<ProcessRenewalWindowDateCommand, ProcessRenewalWindowDateResult>
    {
        private readonly ApplicationDBContext _db;
        
        // Inject query handlers for all read operations
        private readonly IQueryHandler<GetContractsByStatusQuery, List<ContractBasicInfo>> _getContractsQuery;
        private readonly IQueryHandler<GetCommissionRecordByEIdQuery, CommissionRecordDto> _getCommissionQuery;
        private readonly ILoggerService _logger;

        public ProcessRenewalWindowDateCommandHandler(
            ApplicationDBContext db,
            IQueryHandler<GetContractsByStatusQuery, List<ContractBasicInfo>> getContractsQuery,
            IQueryHandler<GetCommissionRecordByEIdQuery, CommissionRecordDto> getCommissionQuery,
            ILoggerService logger)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _getContractsQuery = getContractsQuery ?? throw new ArgumentNullException(nameof(getContractsQuery));
            _getCommissionQuery = getCommissionQuery ?? throw new ArgumentNullException(nameof(getCommissionQuery));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Handles the command to process renewal window date contracts
        /// </summary>
        public async Task<ProcessRenewalWindowDateResult> HandleAsync(ProcessRenewalWindowDateCommand command)
        {
            var startTime = DateTime.Now;
            
            try
            {
                _logger.LogToFile("=== Handler: ProcessRenewalWindowDateCommandHandler Started ===");
                
                ValidateCommand(command);

                var result = new ProcessRenewalWindowDateResult
                {
                    ProcessedAt = DateTime.Now,
                    UpdatedEIds = new List<string>()
                };

                _logger.LogToFile($"📋 Processing renewal window date contracts for date: {command.CurrentDate:yyyy-MM-dd}");
                _logger.LogToFile($"📋 Source Status: {command.SourceStatus}");
                _logger.LogToFile($"📋 Target Status: {command.TargetStatus}");
                _logger.LogToFile($"📋 Days Threshold: {command.DaysThreshold} days");

                // Step 1: Get All Contracts with Status == "Live"
                _logger.LogToFile($"📋 Step 1: Fetching contracts with status '{command.SourceStatus}'...");
                var contractsQuery = new GetContractsByStatusQuery
                {
                    ContractStatus = command.SourceStatus
                };
                var liveContracts = await _getContractsQuery.HandleAsync(contractsQuery);

                result.TotalLiveContracts = liveContracts.Count;
                _logger.LogToFile($"📋 Step 1 Complete: Found {liveContracts.Count} contracts with status '{command.SourceStatus}'");

                if (liveContracts.Count == 0)
                {
                    _logger.LogToFile("📋 No contracts to process - exiting early");
                    return result; // No contracts to process
                }

                var contractsToUpdate = new List<ContractBasicInfo>();

                // Step 2: Check each contract - verify if CED is within or equal to 180 days
                _logger.LogToFile($"📋 Step 2: Checking each contract against CED threshold ({command.DaysThreshold} days)...");
                int checkedCount = 0;
                foreach (var contract in liveContracts)
                {
                    checkedCount++;
                    bool shouldUpdate = await ShouldUpdateContractAsync(contract.EId, command.CurrentDate, command.DaysThreshold);

                    if (shouldUpdate)
                    {
                        _logger.LogToFile($"   ✓ EId {contract.EId} ({contract.Type}) - CED is within {command.DaysThreshold} days");
                        contractsToUpdate.Add(contract);
                    }
                    else
                    {
                        if (checkedCount % 50 == 0) // Log progress every 50 contracts
                        {
                            _logger.LogToFile($"   Progress: Checked {checkedCount}/{liveContracts.Count} contracts...");
                        }
                    }
                }

                result.MatchedRenewalWindowCount = contractsToUpdate.Count;
                _logger.LogToFile($"📋 Step 2 Complete: {contractsToUpdate.Count} contracts matched CED threshold criteria");

                // Step 3: Update contract statuses to "Renewal Window"
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
        private void ValidateCommand(ProcessRenewalWindowDateCommand command)
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

            if (command.DaysThreshold <= 0)
            {
                throw new ArgumentException("DaysThreshold must be greater than 0", nameof(command.DaysThreshold));
            }
        }

        /// <summary>
        /// Determines if a contract should be updated based on CED being within 180 days
        /// Uses Query Handler for read operations (CQRS pattern)
        /// </summary>
        private async Task<bool> ShouldUpdateContractAsync(string eId, DateTime currentDate, int daysThreshold)
        {
            // Use Query Handler to get commission record by EId
            var commissionQuery = new GetCommissionRecordByEIdQuery { EId = eId };
            var commissionRecord = await _getCommissionQuery.HandleAsync(commissionQuery);

            if (!commissionRecord.Found || string.IsNullOrEmpty(commissionRecord.CED))
            {
                return false;
            }

            // Parse the CED date
            if (Helper.TryParseStartDate(commissionRecord.CED, out DateTime cedDate))
            {
                // Calculate the absolute difference in days between current date and CED
                var daysDifference = Math.Abs((currentDate.Date - cedDate.Date).Days);
                
                // Check if CED is within or equal to the threshold (180 days)
                bool shouldUpdate = daysDifference <= daysThreshold;

                if (shouldUpdate)
                {
                    _logger.LogToFile($"   ✓ EId {eId} - CED ({cedDate:yyyy-MM-dd}) is {daysDifference} days from current date (within {daysThreshold} days threshold)");
                }

                return shouldUpdate;
            }

            return false;
        }

        /// <summary>
        /// Updates the contract statuses for all matching contracts
        /// </summary>
        private async Task UpdateContractStatusesAsync(List<ContractBasicInfo> contractsToUpdate, string targetStatus, ProcessRenewalWindowDateResult result)
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
                    reason: "CED is within or equal to 180 days from current date"
                );
            }

            result.UpdatedCount = await _db.SaveChangesAsync();
            _logger.LogToFile($"📋 Database save completed: {result.UpdatedCount} rows affected");
        }
    }
}

