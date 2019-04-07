using IbrServer.NIBRSHelper;
using Intergraph.IPS.CommonComponents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Web;
using static Intergraph.IPS.CommonComponents.DatabaseUpdate;

namespace IbrServer.NIBRS
{
    public class Report
    {
        public List<Record> Records { get; set; }
        DatabaseType dbType;
        DatabaseUpdate _dbUpdate = null;
        SqlQueries sqlQueries;

        public Report()
        {
            sqlQueries = new SqlQueries();
            DatabaseUpdate _dbUpdate = new DatabaseUpdate();
            dbType = _dbUpdate.GetDatabaseType();
        }

        public List<Record> GetReportingRecords(DateTime startDate, DateTime endDate, bool includePriorData)
        {
            List<Record> result = null;
            string sql = string.Empty;

            try
            { }
                sql = sqlQueries.GetReportingRecordsQuery(startDate, endDate, includePriorData, dbType);
                if (_dbUpdate != null)
                    _dbUpdate.ExecuteSqlCommand(sql);

                sql = "";
            }
            catch (Exception ex)
            {
                ExceptionDispatchInfo.Capture(ex).Throw();
            }

            return result;
        }

    }
}