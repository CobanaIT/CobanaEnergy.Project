using CobanaEnergy.Project.Controllers.Base;
using CobanaEnergy.Project.Models;
using Logic.ResponseModel.Helper;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace CobanaEnergy.Project.Controllers.Common
{
    public class CommonController : BaseController
    {
        private readonly ApplicationDBContext _db;
        public CommonController(ApplicationDBContext db)
        {
            _db = db;
        }


        // ✅ Generic endpoint for any SectorType
        [HttpGet]
        public async Task<JsonResult> Sectors(string sectorType)
        {
            if (string.IsNullOrWhiteSpace(sectorType))
                return Json(new { error = "Sector type is required" });

            var sectors = await _db.CE_Sector
                .Where(s => s.SectorType == sectorType && s.Active)
                .Select(s => new { Id = s.SectorID, Name = s.Name })
                .ToListAsync();

            return JsonResponse.Ok(sectors);
        }

        // ✅ Brokerages → Staff
        [HttpGet]
        public async Task<JsonResult> BrokerageStaff(int brokerageId)
        {
            var staff = await _db.CE_BrokerageStaff
                .Where(st => st.SectorID == brokerageId && st.Active)
                .Select(st => new { Id = st.BrokerageStaffID, Name = st.BrokerageStaffName })
                .ToListAsync();
            return JsonResponse.Ok(staff);
        }

        // ✅ Brokerages → Sub-Brokerage
        [HttpGet]
        public async Task<JsonResult> SubBrokerages(int brokerageId)
        {
            var subs = await _db.CE_SubBrokerage
                .Where(sb => sb.SectorID == brokerageId && sb.Active)
                .Select(sb => new { Id = sb.SubBrokerageID, Name = sb.SubBrokerageName })
                .ToListAsync();

            return JsonResponse.Ok(subs);
        }

        // ✅ In-house → Closers
        [HttpGet]
        public async Task<JsonResult> Closers()
        {
            var closers = await _db.CE_Sector
                .Where(s => s.SectorType == "Closer" && s.Active)
                .Select(s => new { Id = s.SectorID, Name = s.Name })
                .ToListAsync();
            return JsonResponse.Ok(closers);
        }

        // ✅ In-house → Lead Generators
        [HttpGet]
        public async Task<JsonResult> LeadGenerators()
        {
            // Option 1: if LeadGen is linked to Closer by SectorID
            var leads = await _db.CE_Sector
                .Where(s => s.SectorType == "Leads Generator" && s.Active == true)
                .Select(s => new { Id = s.SectorID, Name = s.Name })
                .ToListAsync();
            return JsonResponse.Ok(leads);
        }

        // ✅ In-house → Referral Partners
        [HttpGet]
        public async Task<JsonResult> ReferralPartners()
        {
            var refs = await _db.CE_Sector
                .Where(r => r.SectorType == "Referral Partner" && r.Active)
                .Select(s => new { Id = s.SectorID, Name = s.Name })
                .ToListAsync();
            return JsonResponse.Ok(refs);
        }

        [HttpGet]
        public async Task<JsonResult> SubReferralPartners(int referralId)
        {
            var refs = await _db.CE_SubReferral
                .Where(r => r.SectorID == referralId && r.Active)
                .Select(r => new { Id = r.SubReferralID, Name = r.SubReferralPartnerName })
                .ToListAsync();
            return JsonResponse.Ok(refs);
        }

        // ✅ Introducers
        [HttpGet]
        public async Task<JsonResult> Introducers()
        {
            var intros = await _db.CE_Sector
                .Where(s => s.SectorType == "Introducer" && s.Active)
                .Select(s => new { Id = s.SectorID, Name = s.Name })
                .ToListAsync();
            return JsonResponse.Ok(intros);
        }

        // ✅ Sub-Introducers
        [HttpGet]
        public async Task<JsonResult> SubIntroducers(int introducerId)
        {
            var subs = await _db.CE_SubIntroducer
                .Where(si => si.SectorID == introducerId && si.Active)
                .Select(si => new { Id = si.SubIntroducerID, Name = si.SubIntroducerName })
                .ToListAsync();
            return JsonResponse.Ok(subs);
        }
    }
}