using CobanaEnergy.Project.Models;
using CobanaEnergy.BackgroundServices.CQRS.Commands;
using CobanaEnergy.BackgroundServices.CQRS.Queries;
using CobanaEnergy.BackgroundServices.CQRS.Handlers.Queries;
using CobanaEnergy.BackgroundServices.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace CobanaEnergy.BackgroundServices.CQRS.Handlers.Commands
{
    /// <summary>
    /// Handler for processing overdue contracts
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

        public ProcessOverdueContractsCommandHandler(
            ApplicationDBContext db,
            IQueryHandler<GetContractsByStatusQuery, List<ContractBasicInfo>> getContractsQuery,
            IQueryHandler<GetCommissionRecordByEIdQuery, CommissionRecordDto> getCommissionQuery,
            IQueryHandler<GetContractDetailsByTypeQuery, ContractDetailsDto> getContractDetailsQuery)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _getContractsQuery = getContractsQuery ?? throw new ArgumentNullException(nameof(getContractsQuery));
            _getCommissionQuery = getCommissionQuery ?? throw new ArgumentNullException(nameof(getCommissionQuery));
            _getContractDetailsQuery = getContractDetailsQuery ?? throw new ArgumentNullException(nameof(getContractDetailsQuery));
        }

        public async Task<ProcessOverdueContractsResult> HandleAsync(ProcessOverdueContractsCommand command)
        {
            ValidateCommand(command);

            var result = new ProcessOverdueContractsResult
            {
                ProcessedAt = DateTime.Now,
                OverdueEIds = new List<string>()
            };

            return result;
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
        /// Internal class to hold overdue contract information
        /// </summary>
        private class OverdueContractInfo
        {
            public string EId { get; set; }
            public string Type { get; set; }
        }
    }
}


