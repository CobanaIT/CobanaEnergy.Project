using CobanaEnergy.Project.Models;
using CobanaEnergy.Project.Models.Accounts;
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
    /// Handler for processing overdue contracts
    /// Identifies contracts where CurrentDate > StartDate/InitialStartDate
    /// Uses query handlers for all read operations, focuses on write logic
    /// </summary>
    public class ProcessOverdueContractsCommandHandler 
        : ICommandHandler<ProcessOverdueContractsCommand, ProcessOverdueContractsResult>
    {
        private readonly ApplicationDBContext _db;
        
        // Inject query handlers for all read operations
        private readonly IQueryHandler<GetContractsByStatusQuery, List<ContractBasicInfo>> _getContractsQuery;
        private readonly IQueryHandler<GetCommissionRecordByEIdQuery, CommissionRecordDto> _getCommissionQuery;
        private readonly IQueryHandler<GetContractDetailsByTypeQuery, ContractDetailsDto> _getContractDetailsQuery;
        private readonly ILoggerService _logger;

        public ProcessOverdueContractsCommandHandler(
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
        /// Handles the command to process overdue contracts
        /// </summary>
        public async Task<ProcessOverdueContractsResult> HandleAsync(ProcessOverdueContractsCommand command)
        {
            var startTime = DateTime.Now;
            
            try
            {
                _logger.LogToFile("=== Handler: ProcessOverdueContractsCommandHandler Started ===");
                
                ValidateCommand(command);

                var result = new ProcessOverdueContractsResult
                {
                    ProcessedAt = DateTime.Now,
                    OverdueEIds = new List<string>()
                };

                _logger.LogToFile($"📋 Processing overdue contracts for date: {command.CurrentDate:yyyy-MM-dd}");
                _logger.LogToFile($"📋 Source Status: {command.SourceStatus}");

                // Step 1: Get All Eids + Type from CE_ContractStatuses with ContractStatus == "Processing_Present Month"
                _logger.LogToFile("📋 Step 1: Fetching contracts with status 'Processing_Present Month'...");
                var contractsQuery = new GetContractsByStatusQuery
                {
                    ContractStatus = command.SourceStatus
                };
                var presentMonthContracts = await _getContractsQuery.HandleAsync(contractsQuery);

                result.TotalPresentMonthContracts = presentMonthContracts.Count;
                _logger.LogToFile($"📋 Step 1 Complete: Found {presentMonthContracts.Count} contracts with status '{command.SourceStatus}'");

                if (presentMonthContracts.Count == 0)
                {
                    _logger.LogToFile("📋 No contracts to process - exiting early");
                    return result; // No contracts to process
                }

                var overdueContracts = new List<OverdueContractInfo>();

                // Step 2 & 3: Check each contract against StartDate/InitialStartDate
                _logger.LogToFile("📋 Step 2 & 3: Checking each contract against StartDate/InitialStartDate criteria...");
                int checkedCount = 0;
                foreach (var contract in presentMonthContracts)
                {
                    checkedCount++;
                    bool isOverdue = await IsContractOverdueAsync(contract.EId, contract.Type, command.CurrentDate);

                    if (isOverdue)
                    {
                        _logger.LogToFile($"   ✓ EId {contract.EId} ({contract.Type}) - Contract Start Date has passed (OVERDUE)");
                        overdueContracts.Add(new OverdueContractInfo
                        {
                            EId = contract.EId,
                            Type = contract.Type
                        });
                    }
                    else
                    {
                        if (checkedCount % 50 == 0) // Log progress every 50 contracts
                        {
                            _logger.LogToFile($"   Progress: Checked {checkedCount}/{presentMonthContracts.Count} contracts...");
                        }
                    }
                }

                result.OverdueCount = overdueContracts.Count;
                _logger.LogToFile($"📋 Step 2 & 3 Complete: {overdueContracts.Count} contracts identified as overdue");

                // Step 4: Insert overdue contracts into CE_OverDueContracts table
                if (overdueContracts.Count > 0)
                {
                    _logger.LogToFile($"📋 Step 4: Inserting {overdueContracts.Count} overdue contracts into CE_OverDueContracts...");
                    await InsertOverdueContractsAsync(overdueContracts, command.CurrentDate, result);
                    _logger.LogToFile($"📋 Step 4 Complete: Successfully inserted {result.InsertedCount} new overdue contract records");
                }
                else
                {
                    _logger.LogToFile("📋 Step 4: No overdue contracts to insert");
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
        private void ValidateCommand(ProcessOverdueContractsCommand command)
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
        }

        /// <summary>
        /// Determines if a contract is overdue based on StartDate or InitialStartDate
        /// Uses Query Handlers for all read operations (CQRS pattern)
        /// Contract is overdue if CurrentDate > StartDate/InitialStartDate
        /// </summary>
        private async Task<bool> IsContractOverdueAsync(string eId, string type, DateTime currentDate)
        {
            // Step 2: Check CE_CommissionAndReconciliation for StartDate
            var commissionQuery = new GetCommissionRecordByEIdQuery { EId = eId };
            var commissionRecord = await _getCommissionQuery.HandleAsync(commissionQuery);

            if (commissionRecord.Found && !string.IsNullOrEmpty(commissionRecord.StartDate))
            {
                if (Helper.TryParseStartDate(commissionRecord.StartDate, out DateTime startDate))
                {
                    // Contract is overdue if CurrentDate > StartDate
                    return currentDate.Date > startDate.Date;
                }
            }
            else
            {
                // Step 3: If no StartDate exists, check CE_ElectricContract or CE_GasContracts for InitialStartDate
                return await CheckInitialStartDateForOverdueAsync(eId, type, currentDate);
            }

            return false;
        }

        /// <summary>
        /// Helper method to check InitialStartDate in Electric or Gas contracts for overdue status
        /// Uses Query Handler for read operations (CQRS pattern)
        /// </summary>
        private async Task<bool> CheckInitialStartDateForOverdueAsync(string eId, string type, DateTime currentDate)
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
                    // Contract is overdue if CurrentDate > InitialStartDate
                    return currentDate.Date > initialStartDate.Date;
                }
            }

            return false;
        }

        /// <summary>
        /// Inserts overdue contracts into CE_OverDueContracts table
        /// Checks for existing records to avoid duplicates
        /// </summary>
        private async Task InsertOverdueContractsAsync(List<OverdueContractInfo> overdueContracts, DateTime detectedDate, ProcessOverdueContractsResult result)
        {
            // Get existing EIds + Type combinations to avoid duplicates
            var existingEIds = await _db.CE_OverDueContracts
                .AsNoTracking()
                .Select(od => new { od.EId, od.Type })
                .ToListAsync();

            var existingLookup = existingEIds.ToDictionary(x => $"{x.EId}|{x.Type}", x => true);

            var contractsToInsert = new List<CE_OverDueContracts>();
            int skippedCount = 0;

            foreach (var overdueContract in overdueContracts)
            {
                var key = $"{overdueContract.EId}|{overdueContract.Type}";
                
                // Skip if already exists in CE_OverDueContracts
                if (existingLookup.ContainsKey(key))
                {
                    skippedCount++;
                    _logger.LogToFile($"   ⚠ EId {overdueContract.EId} ({overdueContract.Type}) - Already exists in CE_OverDueContracts, skipping");
                    continue;
                }

                // Create new overdue contract record
                var overdueRecord = new CE_OverDueContracts
                {
                    EId = overdueContract.EId,
                    Type = overdueContract.Type,
                    DetectedAsOverdueDate = detectedDate
                };

                contractsToInsert.Add(overdueRecord);
                result.OverdueEIds.Add(overdueContract.EId);
            }

            if (contractsToInsert.Count > 0)
            {
                _db.CE_OverDueContracts.AddRange(contractsToInsert);
                result.InsertedCount = await _db.SaveChangesAsync();
                _logger.LogToFile($"📋 Inserted {result.InsertedCount} new overdue contract records");
                
                if (skippedCount > 0)
                {
                    _logger.LogToFile($"📋 Skipped {skippedCount} duplicate records");
                }
            }
            else
            {
                _logger.LogToFile($"📋 All {overdueContracts.Count} overdue contracts already exist in CE_OverDueContracts - no new records inserted");
            }
        }


        /// <summary>
        /// Internal class to hold overdue contract information
        /// </summary>
        private class OverdueContractInfo
        {
            public string EId { get; set; }
            public string Type { get; set; }
        }
    }
}


