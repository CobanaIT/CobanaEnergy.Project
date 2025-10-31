using CobanaEnergy.Project.Models;
using CobanaEnergy.BackgroundServices.CQRS.Queries;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace CobanaEnergy.BackgroundServices.CQRS.Handlers.Queries
{
    /// <summary>
    /// Handler for retrieving commission and reconciliation records by EId
    /// Returns null object pattern if not found
    /// </summary>
    public class GetCommissionRecordByEIdQueryHandler 
        : IQueryHandler<GetCommissionRecordByEIdQuery, CommissionRecordDto>
    {
        private readonly ApplicationDBContext _db;

        public GetCommissionRecordByEIdQueryHandler(ApplicationDBContext db)
        {
            _db = db;
        }

        public async Task<CommissionRecordDto> HandleAsync(GetCommissionRecordByEIdQuery query)
        {
            var record = await _db.CE_CommissionAndReconciliation
                .AsNoTracking()  // Read-only optimization
                .Where(cr => cr.EId == query.EId)
                .Select(cr => new CommissionRecordDto
                {
                    EId = cr.EId,
                    StartDate = cr.StartDate,
                    CED = cr.CED,
                    CED_COT = cr.CED_COT,
                    Found = true
                })
                .FirstOrDefaultAsync();

            // Null object pattern - return empty DTO if not found
            return record ?? new CommissionRecordDto 
            { 
                EId = query.EId, 
                Found = false 
            };
        }
    }
}


