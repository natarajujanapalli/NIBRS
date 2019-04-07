using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;

namespace IbrServer
{
    public class ReportInfo
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string State { get; set; }
        public string OutputFormat { get; set; }
        public string GeneratedXmlFilesPath { get; set; }
        public string GeneratedOutputFilePath { get; set; }

        public DateTime StartTrackingDate { get; set; }
        public bool IsStateSupportingTimeWindowSubmission { get; set; }
        public bool IsStateSupportingPreNibrsSubmission { get; set; }
        public string IsBaseDateFromCurrentDateOrCurrentReportingMonth { get; set; }
        public bool IsStateSupportingCargoTheftSubmission { get; set; }
        public bool IsStateSupportingAllBiasMotivationsSubmission { get; set; }
        public bool IsStateSupportingLeokaSubmission { get; set; }
        public bool IsStateSupportingOffenderEthnicitySubmission { get; set; }
        public bool AppendStringToAdultArrestTransactionNumber { get; set; }
        public bool AppendStringToJuvenileArrestTransactionNumber { get; set; }
        public DateTime NibrsCutOffDate { get; set; }

        public DataRow NIB_INFO { get; set; }

        public string CityIndicator { get; set; }
        public string ORI { get; set; }

        public string AgencyId { get; set; }
        public string AgencyRec { get; set; }
        public string NIB_FD_VERSION { get; set; }
        public string NIB_STATE_VERSION { get; set; }
    }
}