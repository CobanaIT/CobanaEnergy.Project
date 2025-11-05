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
    /// Handler for ProcessFutureContractsCommand.
    /// Contains all the business logic for processing future contracts to present month.
    /// 
    /// ARCHITECTURE NOTES:
    /// - Uses Query Handlers for ALL read operations (GetContractsByStatusQueryHandler, etc.)
    /// - Direct DB access ONLY for write operations (UpdateContractStatusesAsync)
    /// - This ensures pure CQRS with clear separation of concerns
    /// </summary>
    public class ProcessFutureContractsCommandHandler : ICommandHandler<ProcessFutureContractsCommand, ProcessContractsResult>
    {
        private readonly ApplicationDBContext _db;
        private readonly IQueryHandler<GetContractsByStatusQuery, List<ContractBasicInfo>> _getContractsQuery;
        private readonly IQueryHandler<GetCommissionRecordByEIdQuery, CommissionRecordDto> _getCommissionQuery;
        private readonly IQueryHandler<GetContractDetailsByTypeQuery, ContractDetailsDto> _getContractDetailsQuery;
        private readonly ILoggerService _logger;

        public ProcessFutureContractsCommandHandler(
            ApplicationDBContext db,
            IQueryHandler<GetContractsByStatusQuery, List<ContractBasicInfo>> getContractsQuery,
            IQueryHandler<GetCommissionRecordByEIdQuery, CommissionRecordDto> getCommissionQuery,
            IQueryHandler<GetContractDetailsByTypeQuery, ContractDetailsDto> getContractDetailsQuery,
            ILoggerService logger)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _getContractsQuery = getContractsQuery ?? throw new ArgumentNullException(nameof(getContractsQuery));
            _getCommissionQuery = getCommissionQuery ?? throw new ArgumentNullException(nameof(getCommissionQuery));
            _getContractDetailsQuery = getContractDetailsQuery ?? throw new ArgumentNullException(nameof(getContractDetailsQuery));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Handles the command to process future contracts to present month
        /// </summary>
        public async Task<ProcessContractsResult> HandleAsync(ProcessFutureContractsCommand command)
        {
            var startTime = DateTime.Now;
            
            try
            {
                _logger.LogToFile("=== Handler: ProcessFutureContractsCommandHandler Started ===");
                
                ValidateCommand(command);

                var result = new ProcessContractsResult
                {
                    ProcessedAt = DateTime.Now,
                    UpdatedEIds = new List<string>()
                };

                var currentMonth = command.CurrentDate.Month;
                var currentYear = command.CurrentDate.Year;

                _logger.LogToFile($"📋 Processing contracts for month: {currentMonth}/{currentYear}");
                _logger.LogToFile($"📋 Source Status: {command.SourceStatus}");
                _logger.LogToFile($"📋 Target Status: {command.TargetStatus}");

                // Step 1: Use Query Handler to get all EIds + Type from CE_ContractStatuses with source status
                _logger.LogToFile("📋 Step 1: Fetching contracts with source status...");
                var contractsQuery = new GetContractsByStatusQuery
                {
                    ContractStatus = command.SourceStatus
                };
                var futureContracts = await _getContractsQuery.HandleAsync(contractsQuery);

                result.TotalFutureContracts = futureContracts.Count;
                _logger.LogToFile($"📋 Step 1 Complete: Found {futureContracts.Count} contracts with status '{command.SourceStatus}'");

                if (futureContracts.Count == 0)
                {
                    _logger.LogToFile("📋 No contracts to process - exiting early");
                    return result; // No contracts to process
                }

                var eidsToUpdate = new List<string>();

                // Step 2 & 3: Check each contract against commission/reconciliation and electric and gas contract tables
                _logger.LogToFile("📋 Step 2 & 3: Checking each contract against StartDate/InitialStartDate criteria...");
                int checkedCount = 0;
                foreach (var contract in futureContracts)
                {
                    checkedCount++;
                    bool shouldUpdate = await ShouldUpdateContractAsync(contract.EId, contract.Type, currentMonth, currentYear);

                    if (shouldUpdate)
                    {
                        _logger.LogToFile($"   ✓ EId {contract.EId} ({contract.Type}) - Matched current month criteria");
                        eidsToUpdate.Add(contract.EId);
                    }
                    else
                    {
                        if (checkedCount % 50 == 0) // Log progress every 50 contracts
                        {
                            _logger.LogToFile($"   Progress: Checked {checkedCount}/{futureContracts.Count} contracts...");
                        }
                    }
                }

                result.MatchedCurrentMonth = eidsToUpdate.Count;
                _logger.LogToFile($"📋 Step 2 & 3 Complete: {eidsToUpdate.Count} contracts matched current month criteria");

                // Step 4: Update contract statuses for all matching EIds
                if (eidsToUpdate.Count > 0)
                {
                    _logger.LogToFile($"📋 Step 4: Updating {eidsToUpdate.Count} contracts to '{command.TargetStatus}'...");
                    await UpdateContractStatusesAsync(eidsToUpdate, command.TargetStatus, result);
                    _logger.LogToFile($"📋 Step 4 Complete: Successfully updated {result.UpdatedCount} contracts");
                }
                else
                {
                    _logger.LogToFile("📋 Step 4: No contracts to update");
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
        private void ValidateCommand(ProcessFutureContractsCommand command)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            if (string.IsNullOrWhiteSpace(command.TargetStatus))
            {
                throw new ArgumentException("TargetStatus cannot be empty", nameof(command.TargetStatus));
            }

            if (command.CurrentDate == default)
            {
                throw new ArgumentException("CurrentDate must be set", nameof(command.CurrentDate));
            }
        }

        /// <summary>
        /// Determines if a contract should be updated based on StartDate or InitialStartDate
        /// Uses Query Handlers for all read operations (CQRS pattern)
        /// </summary>
        private async Task<bool> ShouldUpdateContractAsync(string eId, string type, int currentMonth, int currentYear)
        {
            // Step 2: Use Query Handler to check CE_CommissionAndReconciliation for StartDate
            var commissionQuery = new GetCommissionRecordByEIdQuery { EId = eId };
            var commissionRecord = await _getCommissionQuery.HandleAsync(commissionQuery);

            if (commissionRecord.Found && !string.IsNullOrEmpty(commissionRecord.StartDate))
            {
                if (Helper.TryParseStartDate(commissionRecord.StartDate, out DateTime startDate))
                {
                    return startDate.Month == currentMonth && startDate.Year == currentYear;
                }
            }
            else
            {
                // Step 3: If no StartDate exists, use Query Handler to check CE_ElectricContract or CE_GasContracts
                return await CheckInitialStartDateAsync(eId, type, currentMonth, currentYear);
            }

            return false;
        }

        /// <summary>
        /// Helper method to check InitialStartDate in Electric or Gas contracts
        /// Uses Query Handler for read operations (CQRS pattern)
        /// </summary>
        private async Task<bool> CheckInitialStartDateAsync(string eId, string type, int currentMonth, int currentYear)
        {
            if (string.IsNullOrEmpty(type))
            {
                return false;
            }

            // Use Query Handler to get contract details by type
            var contractDetailsQuery = new GetContractDetailsByTypeQuery
            {
                EId = eId,
                ContractType = type
            };
            var contractDetails = await _getContractDetailsQuery.HandleAsync(contractDetailsQuery);

            if (contractDetails.Found && !string.IsNullOrEmpty(contractDetails.InitialStartDate))
            {
                if (Helper.TryParseStartDate(contractDetails.InitialStartDate, out DateTime initialStartDate))
                {
                    return initialStartDate.Month == currentMonth && initialStartDate.Year == currentYear;
                }
            }

            return false;
        }

        /// <summary>
        /// Updates the contract statuses for all matching EIds
        /// </summary>
        private async Task UpdateContractStatusesAsync(List<string> eidsToUpdate, string targetStatus, ProcessContractsResult result)
        {
            var contractsToUpdate = await _db.CE_ContractStatuses
                .Where(cs => eidsToUpdate.Contains(cs.EId))
                .ToListAsync();

            _logger.LogToFile($"📋 Found {contractsToUpdate.Count} contract records to update in database");

            foreach (var contract in contractsToUpdate)
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
                    reason: "StartDate/InitialStartDate matches current month"
                );
            }

            result.UpdatedCount = await _db.SaveChangesAsync();
            _logger.LogToFile($"📋 Database save completed: {result.UpdatedCount} rows affected");
        }

    }
}


