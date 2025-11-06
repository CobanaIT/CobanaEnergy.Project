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
    /// Handler for processing contract ended Ag Lost date contracts
    /// Updates contracts from "Renewal Window - Ag Lost" to "Contract Ended - Ag Lost" where CED == CurrentDate
    /// Uses query handlers for all read operations, focuses on write logic
    /// </summary>
    public class ProcessContractEndedAgLostDateCommandHandler 
        : ICommandHandler<ProcessContractEndedAgLostDateCommand, ProcessContractEndedDateResult>
    {
        private readonly ApplicationDBContext _db;
        private readonly IQueryHandler<GetContractsByStatusQuery, List<ContractBasicInfo>> _getContractsQuery;
        private readonly IQueryHandler<GetCommissionRecordByEIdQuery, CommissionRecordDto> _getCommissionQuery;
        private readonly ILoggerService _logger;

        public ProcessContractEndedAgLostDateCommandHandler(
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

        public async Task<ProcessContractEndedDateResult> HandleAsync(ProcessContractEndedAgLostDateCommand command)
        {
            var startTime = DateTime.Now;
            
            try
            {
                _logger.LogToFile("=== Handler: ProcessContractEndedAgLostDateCommandHandler Started ===");
                ValidateCommand(command);

                var result = new ProcessContractEndedDateResult
                {
                    ProcessedAt = DateTime.Now,
                    UpdatedEIds = new List<string>()
                };

                _logger.LogToFile($"📋 Processing contract ended Ag Lost date contracts for date: {command.CurrentDate:yyyy-MM-dd}");
                _logger.LogToFile($"📋 Source Status: {command.SourceStatus}");
                _logger.LogToFile($"📋 Target Status: {command.TargetStatus}");

                _logger.LogToFile($"📋 Step 1: Fetching contracts with status '{command.SourceStatus}'...");
                var contractsQuery = new GetContractsByStatusQuery { ContractStatus = command.SourceStatus };
                var sourceContracts = await _getContractsQuery.HandleAsync(contractsQuery);

                result.TotalSourceContracts = sourceContracts.Count;
                _logger.LogToFile($"📋 Step 1 Complete: Found {sourceContracts.Count} contracts with status '{command.SourceStatus}'");

                if (sourceContracts.Count == 0)
                {
                    _logger.LogToFile("📋 No contracts to process - exiting early");
                    return result;
                }

                var contractsToUpdate = new List<ContractBasicInfo>();

                _logger.LogToFile($"📋 Step 2: Checking each contract where CED == CurrentDate...");
                int checkedCount = 0;
                foreach (var contract in sourceContracts)
                {
                    checkedCount++;
                    bool shouldUpdate = await ShouldUpdateContractAsync(contract.EId, command.CurrentDate);

                    if (shouldUpdate)
                    {
                        _logger.LogToFile($"   ✓ EId {contract.EId} ({contract.Type}) - CED matches current date");
                        contractsToUpdate.Add(contract);
                    }
                    else
                    {
                        if (checkedCount % 50 == 0)
                        {
                            _logger.LogToFile($"   Progress: Checked {checkedCount}/{sourceContracts.Count} contracts...");
                        }
                    }
                }

                result.MatchedDateCount = contractsToUpdate.Count;
                _logger.LogToFile($"📋 Step 2 Complete: {contractsToUpdate.Count} contracts matched CED == CurrentDate criteria");

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

        private void ValidateCommand(ProcessContractEndedAgLostDateCommand command)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));
            if (command.CurrentDate == default)
                throw new ArgumentException("CurrentDate must be set", nameof(command.CurrentDate));
            if (string.IsNullOrWhiteSpace(command.SourceStatus))
                throw new ArgumentException("SourceStatus cannot be empty", nameof(command.SourceStatus));
            if (string.IsNullOrWhiteSpace(command.TargetStatus))
                throw new ArgumentException("TargetStatus cannot be empty", nameof(command.TargetStatus));
        }

        private async Task<bool> ShouldUpdateContractAsync(string eId, DateTime currentDate)
        {
            var commissionQuery = new GetCommissionRecordByEIdQuery { EId = eId };
            var commissionRecord = await _getCommissionQuery.HandleAsync(commissionQuery);

            if (!commissionRecord.Found || string.IsNullOrEmpty(commissionRecord.CED))
                return false;

            if (Helper.TryParseStartDate(commissionRecord.CED, out DateTime cedDate))
            {
                return cedDate.Date == currentDate.Date;
            }

            return false;
        }

        private async Task UpdateContractStatusesAsync(List<ContractBasicInfo> contractsToUpdate, string targetStatus, ProcessContractEndedDateResult result)
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
                
                _logger.LogContractChange(
                    eId: contract.EId,
                    type: contract.Type ?? "Unknown",
                    previousStatus: previousStatus,
                    newStatus: targetStatus,
                    reason: "CED equals current date"
                );
            }

            result.UpdatedCount = await _db.SaveChangesAsync();
            _logger.LogToFile($"📋 Database save completed: {result.UpdatedCount} rows affected");
        }
    }
}

