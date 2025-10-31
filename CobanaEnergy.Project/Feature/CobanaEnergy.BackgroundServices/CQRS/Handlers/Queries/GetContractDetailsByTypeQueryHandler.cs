using CobanaEnergy.Project.Models;
using CobanaEnergy.BackgroundServices.CQRS.Queries;
using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace CobanaEnergy.BackgroundServices.CQRS.Handlers.Queries
{
    /// <summary>
    /// Handler for retrieving Electric or Gas contract details
    /// Handles both contract types in a single handler
    /// </summary>
    public class GetContractDetailsByTypeQueryHandler 
        : IQueryHandler<GetContractDetailsByTypeQuery, ContractDetailsDto>
    {
        private readonly ApplicationDBContext _db;

        public GetContractDetailsByTypeQueryHandler(ApplicationDBContext db)
        {
            _db = db;
        }

        public async Task<ContractDetailsDto> HandleAsync(GetContractDetailsByTypeQuery query)
        {
            if (string.IsNullOrEmpty(query.ContractType))
            {
                return new ContractDetailsDto { EId = query.EId, Found = false };
            }

            if (query.ContractType.Equals("Electric", StringComparison.OrdinalIgnoreCase))
            {
                return await GetElectricContractAsync(query.EId);
            }
            else if (query.ContractType.Equals("Gas", StringComparison.OrdinalIgnoreCase))
            {
                return await GetGasContractAsync(query.EId);
            }

            return new ContractDetailsDto { EId = query.EId, ContractType = query.ContractType, Found = false };
        }

        private async Task<ContractDetailsDto> GetElectricContractAsync(string eId)
        {
            var contract = await _db.CE_ElectricContracts
                .AsNoTracking()  // Read-only optimization
                .Where(ec => ec.EId == eId)
                .Select(ec => new ContractDetailsDto
                {
                    EId = ec.EId,
                    ContractType = "Electric",
                    InitialStartDate = ec.InitialStartDate,
                    BusinessName = ec.BusinessName,
                    MPAN_MPRN = ec.MPAN,
                    Found = true
                })
                .FirstOrDefaultAsync();

            return contract ?? new ContractDetailsDto 
            { 
                EId = eId, 
                ContractType = "Electric", 
                Found = false 
            };
        }

        private async Task<ContractDetailsDto> GetGasContractAsync(string eId)
        {
            var contract = await _db.CE_GasContracts
                .AsNoTracking()  // Read-only optimization
                .Where(gc => gc.EId == eId)
                .Select(gc => new ContractDetailsDto
                {
                    EId = gc.EId,
                    ContractType = "Gas",
                    InitialStartDate = gc.InitialStartDate,
                    BusinessName = gc.BusinessName,
                    MPAN_MPRN = gc.MPRN,
                    Found = true
                })
                .FirstOrDefaultAsync();

            return contract ?? new ContractDetailsDto 
            { 
                EId = eId, 
                ContractType = "Gas", 
                Found = false 
            };
        }
    }
}


