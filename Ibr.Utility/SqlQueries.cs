using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;

namespace IBR.Utility
{
    public enum DatabaseType { Oracle, SqlServer }



    public class SqlQueries
    {
        DatabaseType dbType;
        string agencyRec;
        public SqlQueries(string AgencyRec, DatabaseType DbType)
        {
            dbType = DbType;
            agencyRec = AgencyRec;
        }

        public SqlQueries(string agencyRec)
        {
            dbType = DatabaseType.SqlServer;
            string databaseType = ConfigurationManager.AppSettings["DatabaseType"].ToString().Trim().ToUpper();
            if (databaseType.Equals("ORACLE"))
                dbType = DatabaseType.Oracle;
        }

        public string GetReportingRecordsFromNIBTRANSQuery(DateTime startDate, DateTime endDate, bool includePriorData)
        {
            string sql = string.Empty;
            string temp = string.Empty;

            if (dbType == DatabaseType.Oracle)
            {
                if (includePriorData)
                    temp = $"REPORTED_DATE <= TO_DATE('{endDate.ToString("yyyy/MM/dd HH:mm:ss")}', 'YYYY/MM/DD HH24:MI:SS'); ";
                else
                    temp = $"REPORTED_DATE BETWEEN TO_DATE ('{startDate.ToString("yyyy/MM/dd HH:mm:ss")}', 'YYYY/MM/DD HH24:MI:SS') AND TO_DATE('{startDate.ToString("yyyy/MM/dd HH:mm:ss")}', 'YYYY/MM/DD HH24:MI:SS') ";
            }
            else
            {
                if (includePriorData)
                    temp = $"REPORTED_DATE <= CONVERT(DATETIME, '{endDate.ToString("yyyy-MM-dd HH:mm:ss")}', 120) ";
                else
                    temp = $"REPORTED_DATE BETWEEN CONVERT(DATETIME, '{startDate.ToString("yyyy-MM-dd HH:mm:ss")}', 120) AND CONVERT(DATETIME, '{endDate.ToString("yyyy-MM-dd HH:mm:ss")}', 120) ";
            }

            if (dbType == DatabaseType.Oracle)
                sql = $"INSERT INTO NIB_REPORTING_RECORDS (SOURCE_ID, SOURCE, SOURCE_REC, SOURCE_NUMBER, REPORTED_DATE) SELECT (SOURCE || SOURCE_REC) SOURCE_ID, SOURCE, SOURCE_REC, SOURCE_NUM SOURCE_NUMBER, REPORTED_DATE FROM NIB_TRANS WHERE AGENCY_REC = '{agencyRec}' AND {temp}";
            else
                sql = $"INSERT INTO NIB_REPORTING_RECORDS (SOURCE_ID, SOURCE, SOURCE_REC, SOURCE_NUMBER, REPORTED_DATE) SELECT CONCAT(RMS_SOURCE, RMS_SOURCE_RECNUM) SOURCE_ID, SOURCE, SOURCE_REC, SOURCE_NUM SOURCE_NUMBER, REPORTED_DATE FROM NIB_TRANS WHERE AGENCY_REC = '{agencyRec}' AND {temp}";

            return sql;
        }

        public string GetReportingRecordsQuery()
        {
            return $"SELECT * FROM NIB_REPORTING_RECORDS WHERE AGENCY_REC = '{agencyRec}' ORDER BY SOURCE, SOURCE_REC";
        }

    }

}
