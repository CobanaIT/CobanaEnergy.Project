using CobanaEnergy.Project.Models;
using CobanaEnergy.BackgroundServices.CQRS.Queries;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace CobanaEnergy.BackgroundServices.CQRS.Handlers.Queries
{
    /// <summary>
    /// Handler for retrieving PostSaleObjection records by EId
    /// Returns null object pattern if not found
    /// Optimized for read-only operations with AsNoTracking()
    /// </summary>
    public class GetPostSaleObjectionByEIdQueryHandler 
        : IQueryHandler<GetPostSaleObjectionByEIdQuery, PostSaleObjectionDto>
    {
        private readonly ApplicationDBContext _db;

        public GetPostSaleObjectionByEIdQueryHandler(ApplicationDBContext db)
        {
            _db = db;
        }

        public async Task<PostSaleObjectionDto> HandleAsync(GetPostSaleObjectionByEIdQuery query)
        {
            var dbQuery = _db.CE_PostSaleObjections
                .AsNoTracking()  // Read-only optimization
                .Where(o => o.EId == query.EId);

            // Apply optional ContractType filter
            if (!string.IsNullOrEmpty(query.ContractType))
            {
                dbQuery = dbQuery.Where(o => o.ContractType == query.ContractType);
            }

            var record = await dbQuery
                .Select(o => new PostSaleObjectionDto
                {
                    EId = o.EId,
                    ContractType = o.ContractType,
                    ObjectionDate = o.ObjectionDate,
                    QueryType = o.QueryType,
                    ObjectionCount = o.ObjectionCount,
                    Found = true
                })
                .FirstOrDefaultAsync();

            // Null object pattern - return empty DTO if not found
            return record ?? new PostSaleObjectionDto 
            { 
                EId = query.EId,
                ContractType = query.ContractType,
                Found = false 
            };
        }
    }
}

