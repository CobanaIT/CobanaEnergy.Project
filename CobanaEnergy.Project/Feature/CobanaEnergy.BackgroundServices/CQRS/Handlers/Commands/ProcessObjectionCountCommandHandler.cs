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
    /// Handler for processing objection count contracts
    /// Updates contracts to "Objection Closed" where ObjectionCount == MaxObjectionCount for the supplier
    /// Uses query handlers for all read operations, focuses on write logic
    /// </summary>
    public class ProcessObjectionCountCommandHandler 
        : ICommandHandler<ProcessObjectionCountCommand, ProcessObjectionCountResult>
    {
        private readonly ApplicationDBContext _db;
        
        // Inject query handlers for all read operations
        private readonly IQueryHandler<GetContractsByStatusQuery, List<ContractBasicInfo>> _getContractsQuery;
        private readonly IQueryHandler<GetContractDetailsByTypeQuery, ContractDetailsDto> _getContractDetailsQuery;
        private readonly IQueryHandler<GetPostSaleObjectionByEIdQuery, PostSaleObjectionDto> _getObjectionQuery;
        private readonly ILoggerService _logger;

        public ProcessObjectionCountCommandHandler(
            ApplicationDBContext db,
            IQueryHandler<GetContractsByStatusQuery, List<ContractBasicInfo>> getContractsQuery,
            IQueryHandler<GetContractDetailsByTypeQuery, ContractDetailsDto> getContractDetailsQuery,
            IQueryHandler<GetPostSaleObjectionByEIdQuery, PostSaleObjectionDto> getObjectionQuery,
            ILoggerService logger)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _getContractsQuery = getContractsQuery ?? throw new ArgumentNullException(nameof(getContractsQuery));
            _getContractDetailsQuery = getContractDetailsQuery ?? throw new ArgumentNullException(nameof(getContractDetailsQuery));
            _getObjectionQuery = getObjectionQuery ?? throw new ArgumentNullException(nameof(getObjectionQuery));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Handles the command to process objection count contracts
        /// </summary>
        public async Task<ProcessObjectionCountResult> HandleAsync(ProcessObjectionCountCommand command)
        {
            var startTime = DateTime.Now;
            
            try
            {
                _logger.LogToFile("=== Handler: ProcessObjectionCountCommandHandler Started ===");
                
                ValidateCommand(command);

                var result = new ProcessObjectionCountResult
                {
                    ProcessedAt = DateTime.Now,
                    UpdatedEIds = new List<string>()
                };

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

                // Step 2 & 3: Get SupplierIDs and check ObjectionCount against MaxObjectionCount
                _logger.LogToFile("📋 Step 2 & 3: Checking each contract against Supplier MaxObjectionCount criteria...");
                int checkedCount = 0;
                foreach (var contract in objectionContracts)
                {
                    checkedCount++;
                    bool shouldUpdate = await ShouldUpdateContractAsync(contract.EId, contract.Type);

                    if (shouldUpdate)
                    {
                        _logger.LogToFile($"   ✓ EId {contract.EId} ({contract.Type}) - ObjectionCount reached MaxObjectionCount");
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

                result.MatchedMaxObjectionCount = contractsToUpdate.Count;
                _logger.LogToFile($"📋 Step 2 & 3 Complete: {contractsToUpdate.Count} contracts matched MaxObjectionCount criteria");

                // Step 4: Update contract statuses to "Objection Closed"
                if (contractsToUpdate.Count > 0)
                {
                    _logger.LogToFile($"📋 Step 4: Updating {contractsToUpdate.Count} contracts to '{command.TargetStatus}'...");
                    await UpdateContractStatusesAsync(contractsToUpdate, command.TargetStatus, result);
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
        private void ValidateCommand(ProcessObjectionCountCommand command)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
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
        /// Determines if a contract should be updated based on ObjectionCount == MaxObjectionCount
        /// Uses Query Handlers for all read operations (CQRS pattern)
        /// </summary>
        private async Task<bool> ShouldUpdateContractAsync(string eId, string type)
        {
            // Step 2: Get SupplierID from CE_ElectricContract or CE_GasContracts
            var contractDetailsQuery = new GetContractDetailsByTypeQuery
            {
                EId = eId,
                ContractType = type
            };
            var contractDetails = await _getContractDetailsQuery.HandleAsync(contractDetailsQuery);

            if (!contractDetails.Found || contractDetails.SupplierId == 0)
            {
                _logger.LogToFile($"   ⚠ EId {eId} ({type}) - Contract or SupplierId not found, skipping");
                return false;
            }

            // Get MaxObjectionCount for this supplier
            var maxObjectionCount = SupplierHelper.GetMaxObjectionCount(contractDetails.SupplierId);
            var supplierName = SupplierHelper.GetSupplierName(contractDetails.SupplierId);
            _logger.LogToFile($"   📋 EId {eId} ({type}) - Supplier: {supplierName} (ID: {contractDetails.SupplierId}), MaxObjectionCount: {maxObjectionCount}");

            // Step 3: Get PostSaleObjection record and check ObjectionCount
            var objectionQuery = new GetPostSaleObjectionByEIdQuery 
            { 
                EId = eId,
                ContractType = type 
            };
            var objectionRecord = await _getObjectionQuery.HandleAsync(objectionQuery);

            if (!objectionRecord.Found)
            {
                _logger.LogToFile($"   ⚠ EId {eId} ({type}) - PostSaleObjection record not found, skipping");
                return false;
            }

            // Check if ObjectionCount == MaxObjectionCount
            bool shouldUpdate = objectionRecord.ObjectionCount == maxObjectionCount;
            
            if (shouldUpdate)
            {
                _logger.LogToFile($"   ✓ EId {eId} ({type}) - ObjectionCount ({objectionRecord.ObjectionCount}) == MaxObjectionCount ({maxObjectionCount})");
            }
            else
            {
                _logger.LogToFile($"   - EId {eId} ({type}) - ObjectionCount ({objectionRecord.ObjectionCount}) < MaxObjectionCount ({maxObjectionCount})");
            }

            return shouldUpdate;
        }

        /// <summary>
        /// Updates the contract statuses for all matching contracts
        /// </summary>
        private async Task UpdateContractStatusesAsync(List<ContractBasicInfo> contractsToUpdate, string targetStatus, ProcessObjectionCountResult result)
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
                    reason: "ObjectionCount reached MaxObjectionCount for supplier"
                );
            }

            result.UpdatedCount = await _db.SaveChangesAsync();
            _logger.LogToFile($"📋 Database save completed: {result.UpdatedCount} rows affected");
        }
    }
}



