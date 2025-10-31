using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CobanaEnergy.Project.Common
{
    public static class SupportedSuppliers
    {
        public static readonly HashSet<string> Names = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "british gas business",
                "bg lite",
                "corona",
                "crown gas and power",
                "sse",
                "edf i&c",
                "total gas and power",
                "edf sme",
                "scottish power",
                "smartest energy"
            };

        public static readonly string[] MeterHeaders = { "meternum", "meterpoint", "mpancore", "mpr", "mpan", "meter point", "mpxn","supply no.","supply no","mpan/mpr","mpxn","supply number" };

        public static readonly Dictionary<string, int> _statusWaitDays = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
              {
                  { "Awaiting Invoice", 42 },
                  { "Awaiting 1st Reconciliation", 395 },
                  { "Awaiting 2nd initial", 395 },
                  { "Awaiting 2nd Reconciliation", 760 },
                  { "Awaiting 3rd initial", 760 },
                  { "Awaiting 3rd Reconciliation", 1125 },
                  { "Awaiting 4th initial", 1125 },
                  { "Awaiting 4th Reconciliation", 1490 },
                  { "Awaiting 5th initial", 1490 },
                  { "Awaiting Final Reconciliation", 60 },
                  { "Awaiting 1st Month Payment", 42 },
                  { "Awaiting 2nd Month Payment", 72 },
                  { "Awaiting 3rd Month Payment", 103 },
                  { "Awaiting 4th Month Payment", 133 },
                  { "Awaiting 5th Month Payment", 164 },
                  { "Awaiting 6th Month Payment", 194 },
                  { "Awaiting 7th Month Payment", 224 },
                  { "Awaiting 8th Month Payment", 255 },
                  { "Awaiting 9th Month Payment", 285 },
                  { "Awaiting 10th Month Payment", 316 },
                  { "Awaiting 11th Month Payment", 346 },
                  { "Awaiting 12th Month Payment", 375 },
                  { "Awaiting 13th Month Payment", 406 },
                  { "Awaiting 14th Month Payment", 436 },
                  { "Awaiting 15th Month Payment", 467 },
                  { "Awaiting 16th Month Payment", 497 },
                  { "Awaiting 17th Month Payment", 528 },
                  { "Awaiting 18th Month Payment", 558 },
                  { "Awaiting 19th Month Payment", 588 },
                  { "Awaiting 20th Month Payment", 619 },
                  { "Awaiting 21st Month Payment", 649 },
                  { "Awaiting 22nd Month Payment", 680 },
                  { "Awaiting 23rd Month Payment", 710 },
                  { "Awaiting 24th Month Payment", 740 },
                  { "Awaiting 25th Month Payment", 771 },
                  { "Awaiting 26th Month Payment", 801 },
                  { "Awaiting 27th Month Payment", 832 },
                  { "Awaiting 28th Month Payment", 862 },
                  { "Awaiting 29th Month Payment", 893 },
                  { "Awaiting 30th Month Payment", 923 },
                  { "Awaiting 31st Month Payment", 953 },
                  { "Awaiting 32nd Month Payment", 984 },
                  { "Awaiting 33rd Month Payment", 1014 },
                  { "Awaiting 34th Month Payment", 1045 },
                  { "Awaiting 35th Month Payment", 1075 },
                  { "Awaiting 36th Month Payment", 1105 },
                  { "Awaiting 37th Month Payment", 1136 },
                  { "Awaiting 38th Month Payment", 1166 },
                  { "Awaiting 39th Month Payment", 1197 },
                  { "Awaiting 40th Month Payment", 1227 },
                  { "Awaiting 41st Month Payment", 1258 },
                  { "Awaiting 42nd Month Payment", 1288 },
                  { "Awaiting 43rd Month Payment", 1318 },
                  { "Awaiting 44th Month Payment", 1349 },
                  { "Awaiting 45th Month Payment", 1379 },
                  { "Awaiting 46th Month Payment", 1410 },
                  { "Awaiting 47th Month Payment", 1440 },
                  { "Awaiting 48th Month Payment", 1470 },
                  { "Awaiting 49th Month Payment", 1501 },
                  { "Awaiting 50th Month Payment", 1531 },
                  { "Awaiting 51st Month Payment", 1562 },
                  { "Awaiting 52nd Month Payment", 1592 },
                  { "Awaiting 53rd Month Payment", 1623 },
                  { "Awaiting 54th Month Payment", 1653 },
                  { "Awaiting 55th Month Payment", 1683 },
                  { "Awaiting 56th Month Payment", 1714 },
                  { "Awaiting 57th Month Payment", 1744 },
                  { "Awaiting 58th Month Payment", 1775 },
                  { "Awaiting 59th Month Payment", 1805 },
                  { "Awaiting 60th Month Payment", 1836 },
                    // Quarterly EAC Payments
                  { "EDF Awaiting Invoice", 105 },
                  { "Awaiting 1st Year - Qtr 1", 100 },
                  { "Awaiting 1st Year - Qtr 2", 190 },
                  { "Awaiting 1st Year - Qtr 3", 280 },
                  { "Awaiting 1st Year - Qtr 4", 375 },
                  { "Awaiting 2nd Year - Qtr 1", 466 },
                  { "Awaiting 2nd Year - Qtr 2", 558 },
                  { "Awaiting 2nd Year - Qtr 3", 649 },
                  { "Awaiting 2nd Year - Qtr 4", 740 },
                  { "Awaiting 3rd Year - Qtr 1", 831 },
                  { "Awaiting 3rd Year - Qtr 2", 923 },
                  { "Awaiting 3rd Year - Qtr 3", 1014 },
                  { "Awaiting 3rd Year - Qtr 4", 1105 },
                  { "Awaiting 4th Year - Qtr 1", 1196 },
                  { "Awaiting 4th Year - Qtr 2", 1288 },
                  { "Awaiting 4th Year - Qtr 3", 1379 },
                  { "Awaiting 4th Year - Qtr 4", 1470 },
                  { "Awaiting 5th Year - Qtr 1", 1561 },
                  { "Awaiting 5th Year - Qtr 2", 1653 },
                  { "Awaiting 5th Year - Qtr 3", 1744 },
                  { "Awaiting 5th Year - Qtr 4", 1835 },
               };

        public static readonly Dictionary<string, int> _statusGasAndPower = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
              {
                   { "Awaiting 1st Month Payment", 90 },
                     { "Awaiting 2nd Month Payment", 120 },
                     { "Awaiting 3rd Month Payment", 150 },
                     { "Awaiting 4th Month Payment", 180 },
                     { "Awaiting 5th Month Payment", 210 },
                     { "Awaiting 6th Month Payment", 240 },
                     { "Awaiting 7th Month Payment", 270 },
                     { "Awaiting 8th Month Payment", 300 },
                     { "Awaiting 9th Month Payment", 330 },
                     { "Awaiting 10th Month Payment", 360 },
                     { "Awaiting 11th Month Payment", 390 },
                     { "Awaiting 12th Month Payment", 425 },
                     { "Awaiting 13th Month Payment", 450 },
                     { "Awaiting 14th Month Payment", 480 },
                     { "Awaiting 15th Month Payment", 510 },
                     { "Awaiting 16th Month Payment", 540 },
                     { "Awaiting 17th Month Payment", 570 },
                     { "Awaiting 18th Month Payment", 600 },
                     { "Awaiting 19th Month Payment", 630 },
                     { "Awaiting 20th Month Payment", 660 },
                     { "Awaiting 21st Month Payment", 690 },
                     { "Awaiting 22nd Month Payment", 720 },
                     { "Awaiting 23rd Month Payment", 750 },
                     { "Awaiting 24th Month Payment", 790 },
                     { "Awaiting 25th Month Payment", 810 },
                     { "Awaiting 26th Month Payment", 840 },
                     { "Awaiting 27th Month Payment", 870 },
                     { "Awaiting 28th Month Payment", 900 },
                     { "Awaiting 29th Month Payment", 930 },
                     { "Awaiting 30th Month Payment", 960 },
                     { "Awaiting 31st Month Payment", 990 },
                     { "Awaiting 32nd Month Payment", 1020 },
                     { "Awaiting 33rd Month Payment", 1050 },
                     { "Awaiting 34th Month Payment", 1080 },
                     { "Awaiting 35th Month Payment", 1110 },
                     { "Awaiting 36th Month Payment", 1155 },
                     { "Awaiting 37th Month Payment", 1170 },
                     { "Awaiting 38th Month Payment", 1200 },
                     { "Awaiting 39th Month Payment", 1230 },
                     { "Awaiting 40th Month Payment", 1260 },
                     { "Awaiting 41st Month Payment", 1290 },
                     { "Awaiting 42nd Month Payment", 1320 },
                     { "Awaiting 43rd Month Payment", 1350 },
                     { "Awaiting 44th Month Payment", 1380 },
                     { "Awaiting 45th Month Payment", 1410 },
                     { "Awaiting 46th Month Payment", 1440 },
                     { "Awaiting 47th Month Payment", 1470 },
                     { "Awaiting 48th Month Payment", 1520 },
                     { "Awaiting 49th Month Payment", 1530 },
                     { "Awaiting 50th Month Payment", 1560 },
                     { "Awaiting 51st Month Payment", 1590 },
                     { "Awaiting 52nd Month Payment", 1620 },
                     { "Awaiting 53rd Month Payment", 1650 },
                     { "Awaiting 54th Month Payment", 1680 },
                     { "Awaiting 55th Month Payment", 1710 },
                     { "Awaiting 56th Month Payment", 1740 },
                     { "Awaiting 57th Month Payment", 1770 },
                     { "Awaiting 58th Month Payment", 1800 },
                     { "Awaiting 59th Month Payment", 1830 },
                     { "Awaiting 60th Month Payment", 1910 }
               };


        public static bool TryGetWaitDays(string paymentStatus, out int waitDays, string supplierName)
        {
            var dictionary = supplierName?.Trim().Equals("total gas and power", StringComparison.OrdinalIgnoreCase) == true
                             ? _statusGasAndPower
                             : _statusWaitDays;
            return dictionary.TryGetValue(paymentStatus, out waitDays);
        }


        /// <summary>
        /// Add more supplier here 
        /// </summary>
        public static readonly Dictionary<string, Tuple<string, string>> Map =
        new Dictionary<string, Tuple<string, string>>(StringComparer.OrdinalIgnoreCase)
        {
            { "British Gas Business", Tuple.Create("BGBContract", "EditBGBContract") },
            { "BG Lite", Tuple.Create("BGLiteContract", "EditBGLiteContract") },
            { "Corona", Tuple.Create("CoronaContract", "EditCoronaContract") },
            { "Crown Gas and Power", Tuple.Create("CrownContract", "EditCrownContract") },
            { "SSE", Tuple.Create("SSEContract", "EditSEEContract") },
            { "EDF I&C", Tuple.Create("EDFContract", "EditEDFContract") },
            { "Total Gas & Power", Tuple.Create("TotalGasAndPowerContract", "EditTotalGasandPowerContract") },
            { "EDF SME", Tuple.Create("EDFSMEContract", "EditEDFSMEContract") },
            { "Scottish Power", Tuple.Create("ScottishPowerContract", "EditScottishPowerContract") }
            // add more suppliers here later
        };

        public const string DefaultAction = "EditSingleContract";
        public const string DefaultController = "Contract";
    }
}