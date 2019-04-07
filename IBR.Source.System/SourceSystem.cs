using Intergraph.IPS.CommonComponents;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Tasks;

namespace IBR.Source.System
{
    public class SourceSystem
    {
        public SourceData SourceRmsData { get; set; }

        DatabaseFactory dbfac = null;
        IDatabase db = null;



        public SourceSystem()
        {
            dbfac = new DbFactory();
            db = dbfac.GetTargetDatabaseObject();

            this.SourceRmsData = new SourceData();
        }


        public SourceData GetRecordsAndData()
        {
            SourceData result = new SourceData();

            result.Records = GetRecords();
            //LoadSourceData(result);

            return result;
        }

        public List<Record> GetRecords()
        {
            List<Record> records = new List<Record>();
            string sql = $"SELECT * FROM NIB_TRANS ORDER BY SOURCE_REC";

            try
            {
                DataTable dt = db.ExecuteSelectCommandMultipleResultDataSet(sql, "NIB_TRANS").Tables[0];

                if (dt != null && dt.Rows != null && dt.Rows.Count > 0)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        records.Add(new Record
                        {
                            RecordType = row["SOURCE"].ToString(),
                            RecordNumber = Convert.ToInt64(row["SOURCE_REC"]),
                            AgencyNumber = Convert.ToInt64(row["AGENCY_REC"])
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                ExceptionDispatchInfo.Capture(ex).Throw();
            }

            this.SourceRmsData.Records = records;
            if (this.SourceRmsData.Records == null || this.SourceRmsData.Records.Count == 0)
                this.SourceRmsData.Records = new List<Record>();

            return records;
        }

        public string LoadSourceData(SourceData sourceData)
        {
            string result = string.Empty;

            try
            {
                DatabaseQueries queries = Queries.GetQueries();

                var records = sourceData.Records;

                //Load Master data
                foreach (var subquery in queries.MasterInfo.SqlQuery)
                {
                    if (!string.IsNullOrWhiteSpace(subquery.TableName) && !string.IsNullOrWhiteSpace(subquery.Query))
                    {
                        var sql = ReplaceStrings(subquery.Query, null);
                        var dt = db.ExecuteSelectCommandMultipleResultDataSet(sql, subquery.TableName).Tables[0];
                        sourceData.SourceMastInfo.Tables.Add(dt.Copy());
                    }
                }

                // Load Records Data
                foreach (Record record in records)
                {
                    result = GetRecordsRmsData(queries, record);
                    record.Source = result;
                }


            }
            catch (Exception ex)
            {
                ExceptionDispatchInfo.Capture(ex).Throw();
            }

            return result;
        }

        private string GetRecordsRmsData(DatabaseQueries queries, Record record)
        {
            string result = string.Empty;

            DataSet ds = new DataSet("WebRMS");
            DataTable dt = null;
            string sql = string.Empty;

            if (record != null)
            {
                switch (record.RecordType.ToUpper())
                {
                    case "I":

                        if (queries != null && queries.Incident != null && queries.Incident.SqlQuery != null && queries.Incident.SqlQuery.Count > 0)
                        {
                            foreach (var subquery in queries.Incident.SqlQuery)
                            {
                                if (!string.IsNullOrWhiteSpace(subquery.TableName) && !string.IsNullOrWhiteSpace(subquery.Query))
                                {
                                    sql = ReplaceStrings(subquery.Query, record);
                                    dt = db.ExecuteSelectCommandMultipleResultDataSet(sql, subquery.TableName).Tables[0];
                                    ds.Tables.Add(dt.Copy());
                                }
                            }
                        }

                        break;

                    case "A":

                        if (queries != null && queries.Arrest != null && queries.Arrest.SqlQuery != null && queries.Arrest.SqlQuery.Count > 0)
                        {
                            foreach (var subquery in queries.Arrest.SqlQuery)
                            {
                                if (!string.IsNullOrWhiteSpace(subquery.TableName) && !string.IsNullOrWhiteSpace(subquery.Query))
                                {
                                    sql = ReplaceStrings(subquery.Query, record);
                                    dt = db.ExecuteSelectCommandMultipleResultDataSet(sql, subquery.TableName).Tables[0];
                                    ds.Tables.Add(dt.Copy());
                                }
                            }
                        }

                        break;

                    case "J":

                        if (queries != null && queries.Juvenile != null && queries.Juvenile != null && queries.Juvenile.SqlQuery.Count > 0)
                        {
                            foreach (var subquery in queries.Juvenile.SqlQuery)
                            {
                                if (!string.IsNullOrWhiteSpace(subquery.TableName) && !string.IsNullOrWhiteSpace(subquery.Query))
                                {
                                    sql = ReplaceStrings(subquery.Query, record);
                                    dt = db.ExecuteSelectCommandMultipleResultDataSet(sql, subquery.TableName).Tables[0];
                                    ds.Tables.Add(dt.Copy());
                                }
                            }
                        }

                        break;

                    default:
                        break;
                }
            }

            StringWriter sw = new StringWriter();
            ds.WriteXml(sw, XmlWriteMode.WriteSchema);
            result = sw.ToString();

            return result;
        }

        private string ReplaceStrings(string sql, Record record)
        {
            string mainSchema = ConfigurationManager.AppSettings["MainSchema"].ToString();

            if (record != null)
            {
                sql = sql.Replace("##INCIRECS##", record.RecordNumber.ToString());
                sql = sql.Replace("##ARSTRECS##", record.RecordNumber.ToString());
                sql = sql.Replace("##ARREST_ID##", record.RecordID.ToString());
            }

            sql = sql.Replace("WEBRMS.DBO.", mainSchema);
            sql = sql.Replace("WEBUSER.", mainSchema);

            return sql;
        }


    }
}
