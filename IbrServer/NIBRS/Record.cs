using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace IbrServer.NIBRS
{
    public class Record
    {
        public long SOURCE_ID { get; set; } //SOURCE + SOURCE_REC
        public long SOURCE_REC { get; set; } //INCIDENT/ARREST/JUVENILE/USE OF FORCE RECNUM
        public string SOURCE_NUMBER { get; set; } //INCIDENT/ARREST/JUVENILE/USE OF FORCE NUMBER
        public DateTime REPORTED_DATE { get; set; } //INCIDENT DATE OR ARREST DATE
        public string SOURCE { get; set; } //I=INCIDENT, S=INCIDENT SUPPLEMENT, A-ARREST, J=JUVENILE, U=USE OF FORCE
        public string SOURCE_SYSTEM { get; set; }   //WEBRMS, CS RMS
        public string ACTION { get; set; }   //I=INSERT or D=DELETE or R=RESUBMISSION

        public string VALIDATED_STATUS { get; set; } //I=INVALID, V=VALID, W=VALID WITH WARNINGS, E=EXCEPTION
        public string SUBMISSION_TYPE { get; set; } //NORMAL, PRE-NIBRS, TIME-WINDOW
        public string PROCESS_ID { get; set; } //GENERATED ID or SUBMISSION ID for only monthly/yearly submissions

        public string SOURCE_DATA { get; set; }
        public string NIBRS_DATA { get; set; }
        public string VALIDATIONS { get; set; }
        public string REPORTING_DATA { get; set; }
    }
}