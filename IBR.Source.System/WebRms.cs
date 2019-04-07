using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.ExceptionServices;
using System.Data;
using System.IO;
using Intergraph.IPS.CommonComponents;
using System.Configuration;
using System.Xml;
using IBR.Utility;

namespace IBR.Source.System
{
    public class WebRms : ISystem
    {
        public List<ColumnInfo> SourceColumnsInfo { get; set; }
        public List<SqlQuery> SqlQueries { get; set; }

        //public Source Source { get; set; }

        public List<Record> Records { get; set; }

        public DataTable MASTCODE { get; set; }
        public DataTable MASTNAME { get; set; }
        public DataTable STATUTE_CODE { get; set; }

        public string MainSchema { get; set; }

        public Record CurrentRecord { get; set; }


        DatabaseFactory dbfac = null;
        IDatabase db = null;


        public WebRms()
        {
            SourceColumnsInfo = ColumnsInfo.GetColumnsInfo();
            SqlQueries = Queries.GetQueryInfo();

            dbfac = new DbFactory();
            db = dbfac.GetTargetDatabaseObject();

            MainSchema = ConfigurationManager.AppSettings["MainSchema"].ToString();
        }

        public virtual void LoadConfigurationData()
        {
            string sql = string.Empty;
            string tableName = string.Empty;
            List<SqlQuery> sourceSqlQueries = SqlQueries.Where(r => r.RECORD_TYPE.ToUpper().Contains("C")).ToList();

            foreach (SqlQuery item in sourceSqlQueries)
            {
                tableName = item.TABLE_NAME.ToUpper();

                sql = item.QUERY;
                sql = ReplaceLiterals(sql);

                var dt = db.ExecuteSelectCommandMultipleResultDataSet(sql, item.TABLE_NAME).Tables[item.TABLE_NAME];

                switch(tableName)
                {
                    case "MASTCODE":
                        MASTCODE = dt;
                        break;

                    case "STATUTE_CODE":
                        STATUTE_CODE = dt;
                        break;

                    case "MASTNAME":
                        MASTNAME = dt;
                        break;

                    default:
                        break;
                }

            }
        }

        public void LoadRecordsData(List<Record> records)
        {
            if (records == null || records.Count == 0)
                return;

            List<string> sources = records.Select(r => r.Source.ToUpper()).Distinct().ToList();

            foreach (string item in sources)
            {
                var sourceRecords = records.Where(r => r.Source.ToUpper().Equals(item)).ToList();
                LoadSourceRecordData(item, sourceRecords);
            }

        }

        public virtual void LoadSourceRecordData(string source, List<Record> records)
        {
            if (string.IsNullOrWhiteSpace(source))
                return;

            DataSet ds = new DataSet("WEBRMS");
            int i = 0;
            List<Record> chunk = new List<Record>();

            foreach (Record record in records)
            {
                i += 1;
                chunk.Add(record);

                if (i == 1000 || i == records.Count)
                {
                    LoadChunkData(source, chunk);

                    i = 0;
                    chunk = new List<Record>();
                }
            }
        }

        public virtual void LoadChunkData(string source, List<Record> records)
        {
            DataSet result = new DataSet();
            string sql = string.Empty;
            string chunkRecnums = string.Join(", ", records.Select(r => r.RecordNumber.ToString()).ToList());

            List<SqlQuery> sourceSqlQueries = SqlQueries.Where(r => r.RECORD_TYPE.ToUpper().Contains(source)).ToList();

            foreach (SqlQuery item in sourceSqlQueries)
            {
                sql = item.QUERY;
                sql = sql.Replace("##INCIRECS##", chunkRecnums);
                sql = ReplaceLiterals(sql);

                var dt = db.ExecuteSelectCommandMultipleResultDataSet(sql, item.TABLE_NAME).Tables[item.TABLE_NAME];
                result.Tables.Add(dt);
            }

            GetRecordData(records, result);

        }

        public virtual void GetRecordData(List<Record> records, DataSet ds)
        {
            string result = string.Empty;

            foreach (Record item in records)
                GetRecordXmlData(item, ds);

        }

        public virtual void GetRecordXmlData(Record record, DataSet ds)
        {
            if (ds == null)
                return;

            DataSet result = ds.Clone();
            string tableName = string.Empty;
            string columnName = string.Empty;

            foreach(DataTable table in ds.Tables)
            {
                tableName = table.TableName;
                columnName = GetSourcePrimaryColumn(record.Source, tableName);

                var rows = table.Select($"{columnName} == {record.RecordNumber}");

                if (rows == null || rows.Length == 0)
                    continue;

                foreach(DataRow row in rows)
                    result.Tables[tableName].Rows.Add(row);
            }

            StringWriter sw = new StringWriter();
            result.WriteXml(sw, XmlWriteMode.WriteSchema);
            record.Source = sw.ToString();
            //record.
        }

        public virtual string GetSourcePrimaryColumn(string source, string tableName)
        {
            string columnName = string.Empty;
            source = source.ToUpper().Trim();
            tableName = tableName.ToUpper().Trim();

            switch (source)
            {
                case "I":
                    columnName = "INCIDENT_REC";
                    break;

                case "S":
                    columnName = "INCIDENT_REC";
                    break;

                case "A":
                    columnName = "ARST_REC";
                    break;

                case "J":
                    columnName = "JC_REC";
                    break;

                case "U":
                    columnName = "UOF_REC";
                    break;

                default:
                    columnName = "INCIDENT_REC";
                    break;
            }

            return columnName;
        }

        public virtual string GetJsonData(DataSet ds)
        {
            string result = string.Empty;
            string tableName = string.Empty;
            string columnName = string.Empty;
            StringBuilder sb = new StringBuilder();

            foreach(DataTable dt in ds.Tables)
            {
                tableName = dt.TableName;
                sb.AppendLine($"<{dt.TableName}>");

                foreach (DataRow row in dt.Rows)
                {
                    sb.AppendLine($"<row>");

                    foreach (var column in dt.Columns)
                    {
                        columnName = columnName.ToString();
                        var temp = SourceColumnsInfo.Where(r => r.TableName == tableName && r.ColumnName == columnName).Select(r => r.ColumnSourceTable).FirstOrDefault();

                        if (temp == null)
                        {
                            sb.AppendLine($"<{columnName}>{GetValue(columnName, row)}</{columnName}>");

                        }
                        else if (temp.Equals("STATUTE_CODE"))
                        {
                            sb.AppendLine($"<{columnName}>{GetStatuteElement(row[columnName].ToString())}</{columnName}>");
                        }
                        else if (temp.Equals("MASTCODE"))
                        {
                            sb.AppendLine($"<{columnName}>{GetMastCodeElement(row[columnName].ToString())}</{columnName}>");
                        }
                        else if (temp.Equals("MASTNAME"))
                        {
                            sb.AppendLine($"<{columnName}>{GetMastNameElement(row[columnName].ToString())}</{columnName}>");
                        }
                        else
                        {

                        }
                    }
                }

                sb.AppendLine($"</row>");

            }

            result = sb.ToString();
            return result;
        }

        public virtual string GetValue(string columnName, DataRow row)
        {
            string result = string.Empty;

            var value = row[columnName];
            //if (typeof(value) == string.gett)
            result = value.ToString();

            return result;
        }

        private object GetStatuteElement(string recnum)
        {
            StringBuilder sb = new StringBuilder();

            DataRow row = this.STATUTE_CODE.Select($"RECNUM = {recnum}").FirstOrDefault();

            if (row == null)
            {
                sb.AppendLine($"<RECNUM></RECNUM>");
                sb.AppendLine($"<NIB_CODE></NIB_CODE>");
                sb.AppendLine($"<NIB_CLASS></NIB_CLASS>");
                sb.AppendLine($"<CLASS></CLASS>");
                sb.AppendLine($"<TYPE></TYPE>");
                sb.AppendLine($"<CODE></CODE>");
                sb.AppendLine($"<NAME></NAME>");
                sb.AppendLine($"<AGENCY_REC></AGENCY_REC>");
            }
            else
            {
                sb.AppendLine($"<RECNUM>{row["RECNUM"].ToString()}</RECNUM>");
                sb.AppendLine($"<NIB_CODE>{row["NIB_CODE"].ToString()}</NIB_CODE>");
                sb.AppendLine($"<NIB_CLASS>{row["NIB_CLASS"].ToString()}</NIB_CLASS>");
                sb.AppendLine($"<CLASS>{row["CLASS"].ToString()}</CLASS>");
                sb.AppendLine($"<TYPE>{row["TYPE"].ToString()}</TYPE>");
                sb.AppendLine($"<CODE>{row["CODE"].ToString()}</CODE>");
                sb.AppendLine($"<NAME>{row["NAME"].ToString()}</NAME>");
                sb.AppendLine($"<AGENCY_REC>{row["AGENCY_REC"].ToString()}</AGENCY_REC>");
            }

            return sb.ToString();
        }

        public virtual string GetMastCodeElement(string recnum)
        {
            StringBuilder sb = new StringBuilder();

            DataRow row = null;

            if (!string.IsNullOrWhiteSpace(recnum))
                row = this.MASTCODE.Select($"RECNUM = {recnum}").FirstOrDefault();

            if (row == null)
            {
                sb.AppendLine($"<RECNUM></RECNUM>");
                sb.AppendLine($"<NIB_CODE></NIB_CODE>");
                sb.AppendLine($"<NIB_CLASS></NIB_CLASS>");
                sb.AppendLine($"<CLASS></CLASS>");
                sb.AppendLine($"<TYPE></TYPE>");
                sb.AppendLine($"<CODE></CODE>");
                sb.AppendLine($"<NAME></NAME>");
                sb.AppendLine($"<AGENCY_REC></AGENCY_REC>");
            }
            else
            {
                sb.AppendLine($"<RECNUM>{row["RECNUM"].ToString()}</RECNUM>");
                sb.AppendLine($"<NIB_CODE>{row["NIB_CODE"].ToString()}</NIB_CODE>");
                sb.AppendLine($"<NIB_CLASS>{row["NIB_CLASS"].ToString()}</NIB_CLASS>");
                sb.AppendLine($"<CLASS>{row["CLASS"].ToString()}</CLASS>");
                sb.AppendLine($"<TYPE>{row["TYPE"].ToString()}</TYPE>");
                sb.AppendLine($"<CODE>{row["CODE"].ToString()}</CODE>");
                sb.AppendLine($"<NAME>{row["NAME"].ToString()}</NAME>");
                sb.AppendLine($"<AGENCY_REC>{row["AGENCY_REC"].ToString()}</AGENCY_REC>");
            }

            return sb.ToString();
        }

        private object GetMastNameElement(string recnum)
        {
            throw new NotImplementedException();
        }


        public virtual string Beautify(string xmlString)
        {
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(xmlString);

                StringBuilder sb = new StringBuilder();
                XmlWriterSettings settings = new XmlWriterSettings
                {
                    Indent = true,
                    IndentChars = "  ",
                    NewLineChars = "\r\n",
                    NewLineHandling = NewLineHandling.Replace
                };

                using (XmlWriter writer = XmlWriter.Create(sb, settings))
                {
                    doc.Save(writer);
                }
                xmlString = sb.ToString();
            }
            catch (Exception)
            {
            }
            return xmlString;
        }


        //public virtual SourceData TransformSourceDataToCommonIbrData(SourceData source)
        //{
        //    this.Source = source;

        //    Init();

        //    StringBuilder sb = new StringBuilder();
        //    string temp = string.Empty;

        //    foreach (Record record in this.Source.Records)
        //    {
        //        this.CurrentRecord = record;

        //        sb = new StringBuilder();

        //        sb.AppendLine("<INCIDENT>");

        //        temp = GetIncident();
        //        sb.AppendLine(temp);

        //        temp = GetOffenses();
        //        sb.AppendLine(temp);

        //        temp = GetVictims();
        //        sb.AppendLine(temp);

        //        temp = GetOffenders();
        //        sb.AppendLine(temp);

        //        sb.AppendLine("</INCIDENT>");

        //        record.CommonIbrData = Beautify(sb.ToString());
        //    }

        //    return source;
        //}

        //public virtual void Init()
        //{
        //    if (this.Source != null)
        //        this.Records = this.Source.Records;

        //    if (this.Source != null && this.Source.SourceMastInfo != null && this.Source.SourceMastInfo.Tables.Count > 0)
        //    {
        //        if (this.Source.SourceMastInfo.Tables.Contains("MASTCODE"))
        //            this.MASTCODE = this.Source.SourceMastInfo.Tables["MASTCODE"];

        //        if (this.Source.SourceMastInfo.Tables.Contains("MASTNAME"))
        //            this.MASTNAME = this.Source.SourceMastInfo.Tables["MASTNAME"];

        //        if (this.Source.SourceMastInfo.Tables.Contains("STATUTE_CODE"))
        //            this.STATUTE_CODE = this.Source.SourceMastInfo.Tables["STATUTE_CODE"];
        //    }

        //    if (this.MASTCODE == null)
        //        this.MASTCODE = GetMastCodeData();

        //    if (this.MASTNAME == null)
        //        this.MASTNAME = GetMastNameData();

        //    if (this.STATUTE_CODE == null)
        //        this.STATUTE_CODE = GetStatuteCodeData();

        //}

        //public virtual DataTable GetMastCodeData()
        //{
        //    DataTable result = null;

        //    string sql = GetMastCodeDataSqlQuery();
        //    result = db.ExecuteSelectCommandMultipleResultDataSet(sql, "MASTCODE").Tables[0];
        //    return result;
        //}

        //public virtual string GetMastCodeDataSqlQuery()
        //{
        //    string sql = $"SELECT RECNUM, NIB_CODE, NIB_CLASS, CLASS, TYPE, CODE, NAME, AGENCY_REC FROM WEBRMS.DBO.MASTCODE ORDER BY TYPE, AGENCY_REC, NIB_CODE, CODE";
        //    return ReplaceLiterals(sql);
        //}

        //public virtual DataTable GetMastNameData()
        //{
        //    DataTable result = null;

        //    string sql = GetMastNameDataSqlQuery();
        //    result = db.ExecuteSelectCommandMultipleResultDataSet(sql, "MASTNAME").Tables[0];
        //    return result;
        //}

        //public virtual string GetMastNameDataSqlQuery()
        //{
        //    string sql = $"SELECT RECNUM, LNAME, FNAME, MIDDLE, NAME_TYPE, DOB, SEX, RACE, ETHNIC FROM WEBRMS.DBO.MASTNAME ORDER BY RECNUM";
        //    return ReplaceLiterals(sql);
        //}

        //public virtual DataTable GetStatuteCodeData()
        //{
        //    DataTable result = null;

        //    string sql = GetStatuteCodeDataSqlQuery();
        //    result = db.ExecuteSelectCommandMultipleResultDataSet(sql, "STATUTE_CODE").Tables[0];
        //    return result;
        //}

        //public virtual string GetStatuteCodeDataSqlQuery()
        //{
        //    string sql = $"SELECT RECNUM, NIB_CODE, CODE, NAME, AGENCY_REC FROM WEBRMS.DBO.STATUTE_CODE WHERE NIB_CODE IS NOT NULL ORDER BY NIB_CODE, AGENCY_REC, CODE";
        //    return ReplaceLiterals(sql);
        //}

        //public virtual string ReplaceLiterals(string sql)
        //{
        //    sql = sql.Replace("WEBRMS.DBO.", MainSchema);
        //    return sql;
        //}


        //public virtual string GetIncident()
        //{
        //    StringBuilder sb = new StringBuilder();
        //    DataTable incidentData = LoadIncidentData();
        //    //sb.AppendLine($"<INCIDENT></INCIDENT>");
        //    sb.AppendLine($"{GetIncidentColumns(incidentData)}");

        //    return sb.ToString();
        //}

        //public virtual string GetIncidentDataSqlQuery(string incidentRec)
        //{
        //    string sql = $"SELECT * FROM WEBRMS.DBO.INCIDENT WHERE RECNUM = {incidentRec} ";
        //    return ReplaceLiterals(sql);
        //}

        //public virtual DataTable LoadIncidentData()
        //{
        //    DataTable result = null;

        //    string sql = GetIncidentDataSqlQuery(CurrentRecord.RecordNumber.ToString());
        //    result = db.ExecuteSelectCommandMultipleResultDataSet(sql, "INCIDENT").Tables[0];
        //    return result;
        //}

        //public virtual string GetIncidentColumns(DataTable incidentData)
        //{
        //    StringBuilder sb = new StringBuilder();
        //    string temp = string.Empty;

        //    DataRow row = incidentData.Rows[0];

        //    sb.AppendLine($"<RECNUM>{row["RECNUM"].ToString()}</RECNUM>");
        //    sb.AppendLine($"<AGENCY_REC>{row["AGENCY_REC"].ToString()}</AGENCY_REC>");
        //    sb.AppendLine($"<CREATED_ON>{row["CREATED_ON"].ToString()}</CREATED_ON>");
        //    sb.AppendLine($"<CREATED_BY>{row["CREATED_BY"].ToString()}</CREATED_BY>");
        //    sb.AppendLine($"<UPDATED_ON>{row["UPDATED_ON"].ToString()}</UPDATED_ON>");
        //    sb.AppendLine($"<UPDATED_BY>{row["UPDATED_BY"].ToString()}</UPDATED_BY>");

        //    sb.AppendLine($"<INCIDENT_NUM>{row["INCIDENT_NUM"].ToString()}</INCIDENT_NUM>");
        //    sb.AppendLine($"<OCCURRED_ON>{row["OCCURRED_ON"].ToString()}</OCCURRED_ON>");
        //    sb.AppendLine($"<OCCURRED_TO>{row["OCCURRED_TO"].ToString()}</OCCURRED_TO>");
        //    sb.AppendLine($"<REPORTED_ON>{row["REPORTED_ON"].ToString()}</REPORTED_ON>");

        //    temp = GetMastCodeElement(row["DISP_CODE"].ToString());
        //    sb.AppendLine($"<DISP_CODE>{temp}</DISP_CODE>");

        //    sb.AppendLine($"<DISP_DATE>{row["DISP_DATE"].ToString()}</DISP_DATE>");

        //    temp = GetMastCodeElement(row["STATT"].ToString());
        //    sb.AppendLine($"<STATT>{temp}</STATT>");

        //    sb.AppendLine($"<STATUS_DATE>{row["STATUS_DATE"].ToString()}</STATUS_DATE>");


        //    return sb.ToString();
        //}


        //public virtual string GetOffenses()
        //{
        //    StringBuilder sb = new StringBuilder();
        //    DataTable data = LoadOffenses();

        //    sb.AppendLine($"<OFFENSES>");

        //    foreach (DataRow row in data.Rows)
        //        sb.AppendLine($"<row>{GetOffenseColumns(row)}</row>");

        //    sb.AppendLine($"</OFFENSES>");

        //    return sb.ToString();
        //}

        //public virtual string GetOffenseSqlQuery(string incidentRec)
        //{
        //    string sql = $"SELECT * FROM WEBRMS.DBO.INCIDENT_OFFENSE WHERE INCIDENT_REC = {incidentRec} ";
        //    return ReplaceLiterals(sql);
        //}

        //public virtual DataTable LoadOffenses()
        //{
        //    DataTable result = null;

        //    string sql = GetOffenseSqlQuery(CurrentRecord.RecordNumber.ToString());
        //    result = db.ExecuteSelectCommandMultipleResultDataSet(sql, "INCIDENT_OFFENSE").Tables[0];
        //    return result;
        //}

        //public virtual string GetOffenseColumns(DataRow row)
        //{
        //    if (row == null)
        //        return string.Empty;

        //    StringBuilder sb = new StringBuilder();
        //    string temp = string.Empty;

        //    sb.AppendLine($"<RECNUM>{row["RECNUM"].ToString()}</RECNUM>");
        //    sb.AppendLine($"<AGENCY_REC>{row["AGENCY_REC"].ToString()}</AGENCY_REC>");
        //    sb.AppendLine($"<CREATED_ON>{row["CREATED_ON"].ToString()}</CREATED_ON>");
        //    sb.AppendLine($"<CREATED_BY>{row["CREATED_BY"].ToString()}</CREATED_BY>");
        //    sb.AppendLine($"<UPDATED_ON>{row["UPDATED_ON"].ToString()}</UPDATED_ON>");
        //    sb.AppendLine($"<UPDATED_BY>{row["UPDATED_BY"].ToString()}</UPDATED_BY>");

        //    sb.AppendLine($"<NIBRS_CODE>{row["NIBRS_CODE"].ToString()}</NIBRS_CODE>");
        //    sb.AppendLine($"<NUM_PREMISIS_ENT>{row["NUM_PREMISIS_ENT"].ToString()}</NUM_PREMISIS_ENT>");
        //    sb.AppendLine($"<SUPPLEMENT_VOID_REC>{row["SUPPLEMENT_VOID_REC"].ToString()}</SUPPLEMENT_VOID_REC>");


        //    temp = GetMastCodeElement(row["ATTEMPT_COMPLETED"].ToString());
        //    sb.AppendLine($"<ATTEMPT_COMPLETED>{temp}</ATTEMPT_COMPLETED>");

        //    temp = GetMastCodeElement(row["OFFENDER_SUSPECTED_USE1"].ToString());
        //    sb.AppendLine($"<OFFENDER_SUSPECTED_USE1>{temp}</OFFENDER_SUSPECTED_USE1>");

        //    temp = GetMastCodeElement(row["OFFENDER_SUSPECTED_USE2"].ToString());
        //    sb.AppendLine($"<OFFENDER_SUSPECTED_USE2>{temp}</OFFENDER_SUSPECTED_USE2>");

        //    temp = GetMastCodeElement(row["OFFENDER_SUSPECTED_USE3"].ToString());
        //    sb.AppendLine($"<OFFENDER_SUSPECTED_USE3>{temp}</OFFENDER_SUSPECTED_USE3>");

        //    temp = GetMastCodeElement(row["BIAS_MOTIVATION1"].ToString());
        //    sb.AppendLine($"<BIAS_MOTIVATION1>{temp}</BIAS_MOTIVATION1>");

        //    temp = GetMastCodeElement(row["BIAS_MOTIVATION2"].ToString());
        //    sb.AppendLine($"<BIAS_MOTIVATION2>{temp}</BIAS_MOTIVATION2>");

        //    temp = GetMastCodeElement(row["BIAS_MOTIVATION3"].ToString());
        //    sb.AppendLine($"<BIAS_MOTIVATION3>{temp}</BIAS_MOTIVATION3>");

        //    temp = GetMastCodeElement(row["BIAS_MOTIVATION4"].ToString());
        //    sb.AppendLine($"<BIAS_MOTIVATION4>{temp}</BIAS_MOTIVATION4>");

        //    temp = GetMastCodeElement(row["BIAS_MOTIVATION5"].ToString());
        //    sb.AppendLine($"<BIAS_MOTIVATION5>{temp}</BIAS_MOTIVATION5>");

        //    temp = GetMastCodeElement(row["LOCATION_CODE"].ToString());
        //    sb.AppendLine($"<LOCATION_CODE>{temp}</LOCATION_CODE>");

        //    temp = GetMastCodeElement(row["METHOD_OF_ENTRY"].ToString());
        //    sb.AppendLine($"<METHOD_OF_ENTRY>{temp}</METHOD_OF_ENTRY>");

        //    temp = GetMastCodeElement(row["GROUP_ACTIVITY"].ToString());
        //    sb.AppendLine($"<GROUP_ACTIVITY1>{temp}</GROUP_ACTIVITY1>");

        //    temp = GetMastCodeElement(row["GROUP_ACTIVITY2"].ToString());
        //    sb.AppendLine($"<GROUP_ACTIVITY2>{temp}</GROUP_ACTIVITY2>");

        //    temp = GetMastCodeElement(row["GROUP_TYPE1"].ToString());
        //    sb.AppendLine($"<GROUP_TYPE1>{temp}</GROUP_TYPE1>");

        //    temp = GetMastCodeElement(row["GROUP_TYPE2"].ToString());
        //    sb.AppendLine($"<GROUP_TYPE2>{temp}</GROUP_TYPE2>");

        //    temp = GetMastCodeElement(row["GROUP_NAME1"].ToString());
        //    sb.AppendLine($"<GROUP_NAME1>{temp}</GROUP_NAME1>");

        //    temp = GetMastCodeElement(row["GROUP_NAME2"].ToString());
        //    sb.AppendLine($"<GROUP_NAME2>{temp}</GROUP_NAME2>");

        //    temp = GetMastCodeElement(row["ANIMAL_CRUELTY_TYPE"].ToString());
        //    sb.AppendLine($"<ANIMAL_CRUELTY_TYPE>{temp}</ANIMAL_CRUELTY_TYPE>");

        //    temp = GetMastCodeElement(row["TYPE_CRIMINAL_ACT1"].ToString());
        //    sb.AppendLine($"<TYPE_CRIMINAL_ACT1>{temp}</TYPE_CRIMINAL_ACT1>");

        //    temp = GetMastCodeElement(row["TYPE_CRIMINAL_ACT2"].ToString());
        //    sb.AppendLine($"<TYPE_CRIMINAL_ACT2>{temp}</TYPE_CRIMINAL_ACT2>");

        //    temp = GetMastCodeElement(row["TYPE_CRIMINAL_ACT3"].ToString());
        //    sb.AppendLine($"<TYPE_CRIMINAL_ACT3>{temp}</TYPE_CRIMINAL_ACT3>");

        //    temp = GetMastCodeElement(row["WEAPON1"].ToString());
        //    sb.AppendLine($"<WEAPON1>{temp}</WEAPON1>");

        //    temp = GetMastCodeElement(row["WEAPON1_AUTO"].ToString());
        //    sb.AppendLine($"<WEAPON1_AUTO>{temp}</WEAPON1_AUTO>");

        //    temp = GetMastCodeElement(row["WEAPON2"].ToString());
        //    sb.AppendLine($"<WEAPON2>{temp}</WEAPON2>");

        //    temp = GetMastCodeElement(row["WEAPON2_AUTO"].ToString());
        //    sb.AppendLine($"<WEAPON2_AUTO>{temp}</WEAPON2_AUTO>");

        //    temp = GetMastCodeElement(row["WEAPON3"].ToString());
        //    sb.AppendLine($"<WEAPON3>{temp}</WEAPON3>");

        //    temp = GetMastCodeElement(row["WEAPON3_AUTO"].ToString());
        //    sb.AppendLine($"<WEAPON3_AUTO>{temp}</WEAPON3_AUTO>");

        //    temp = GetMastCodeElement(row["CARGO_THEFT"].ToString());
        //    sb.AppendLine($"<CARGO_THEFT>{temp}</CARGO_THEFT>");


        //    return sb.ToString();
        //}


        //public virtual string GetVictims()
        //{
        //    StringBuilder sb = new StringBuilder();
        //    DataTable data = Loadvictims();

        //    sb.AppendLine($"<VICTIMS>");

        //    foreach (DataRow row in data.Rows)
        //    {
        //        var temp = GetVictimColumns(row);
        //        sb.AppendLine($"<row>{temp}</row>");
        //    }

        //    sb.AppendLine($"</VICTIMS>");

        //    return sb.ToString();
        //}

        //public virtual string GetVictimsSqlQuery(string incidentRec)
        //{
        //    string sql = $"SELECT *, (SELECT NAME_TYPE FROM WEBRMS.DBO.MASTNAME WHERE RECNUM = NAME_REC) NAME_TYPE FROM WEBRMS.DBO.LINK WHERE INCIDENT_REC = {incidentRec} AND INVOLVEMENT IN ({GetNibClassRecnums("INVL", "VICTIM")})";
        //    return ReplaceLiterals(sql);
        //}

        //public virtual DataTable Loadvictims()
        //{
        //    DataTable result = null;

        //    string sql = GetVictimsSqlQuery(CurrentRecord.RecordNumber.ToString());
        //    result = db.ExecuteSelectCommandMultipleResultDataSet(sql, "VICTIMS").Tables[0];
        //    return result;
        //}

        //public virtual string GetVictimColumns(DataRow row)
        //{
        //    if (row == null)
        //        return string.Empty;

        //    StringBuilder sb = new StringBuilder();
        //    string temp = string.Empty;

        //    sb.AppendLine($"<RECNUM>{row["RECNUM"].ToString()}</RECNUM>");
        //    sb.AppendLine($"<AGENCY_REC>{row["AGENCY_REC"].ToString()}</AGENCY_REC>");
        //    sb.AppendLine($"<CREATED_ON>{row["CREATED_ON"].ToString()}</CREATED_ON>");
        //    sb.AppendLine($"<CREATED_BY>{row["CREATED_BY"].ToString()}</CREATED_BY>");
        //    sb.AppendLine($"<UPDATED_ON>{row["UPDATED_ON"].ToString()}</UPDATED_ON>");
        //    sb.AppendLine($"<UPDATED_BY>{row["UPDATED_BY"].ToString()}</UPDATED_BY>");

        //    sb.AppendLine($"<SUPPLEMENT_VOID_REC>{row["SUPPLEMENT_VOID_REC"].ToString()}</SUPPLEMENT_VOID_REC>");
        //    sb.AppendLine($"<UNIQUEID>{row["UNIQUEID"].ToString()}</UNIQUEID>");
        //    sb.AppendLine($"<DOB>{row["DOB"].ToString()}</DOB>");
        //    sb.AppendLine($"<AGELOW>{row["AGELOW"].ToString()}</AGELOW>");
        //    sb.AppendLine($"<AGEHIGH>{row["AGEHIGH"].ToString()}</AGEHIGH>");
        //    //sb.AppendLine($"<LEOKA_OTHER_ORI>{row["LEOKA_OTHER_ORI"].ToString()}</LEOKA_OTHER_ORI>");
        //    //sb.AppendLine($"<LEOKA_OFFICER_ID>{row["LEOKA_OFFICER_ID"].ToString()}</LEOKA_OFFICER_ID>");
        //    sb.AppendLine($"<NAME_REC>{row["NAME_REC"].ToString()}</NAME_REC>");
        //    sb.AppendLine($"<LNAME>{row["LNAME"].ToString()}</LNAME>");
        //    sb.AppendLine($"<FNAME>{row["FNAME"].ToString()}</FNAME>");


        //    temp = GetMastCodeElement(row["INVOLVEMENT"].ToString());
        //    sb.AppendLine($"<INVOLVEMENT>{temp}</INVOLVEMENT>");

        //    temp = GetMastCodeElement(row["NAME_TYPE"].ToString());
        //    sb.AppendLine($"<NAME_TYPE>{temp}</NAME_TYPE>");

        //    temp = GetMastCodeElement(row["SEX"].ToString());
        //    sb.AppendLine($"<SEX>{temp}</SEX>");

        //    temp = GetMastCodeElement(row["RACE"].ToString());
        //    sb.AppendLine($"<RACE>{temp}</RACE>");

        //    temp = GetMastCodeElement(row["ETHNICITY"].ToString());
        //    sb.AppendLine($"<ETHNICITY>{temp}</ETHNICITY>");

        //    temp = GetMastCodeElement(row["RES_STAT"].ToString());
        //    sb.AppendLine($"<RES_STAT>{temp}</RES_STAT>");

        //    temp = GetMastCodeElement(row["CIRCUMSTANCE"].ToString());
        //    sb.AppendLine($"<CIRCUMSTANCE>{temp}</CIRCUMSTANCE>");

        //    temp = GetMastCodeElement(row["CIRCUMSTANCE_2"].ToString());
        //    sb.AppendLine($"<CIRCUMSTANCE_2>{temp}</CIRCUMSTANCE_2>");

        //    temp = GetMastCodeElement(row["JUSTIFIABLE_HOMICIDE"].ToString());
        //    sb.AppendLine($"<JUSTIFIABLE_HOMICIDE>{temp}</JUSTIFIABLE_HOMICIDE>");

        //    temp = GetMastCodeElement(row["VICTIM_INJURY"].ToString());
        //    sb.AppendLine($"<VICTIM_INJURY>{temp}</VICTIM_INJURY>");

        //    temp = GetMastCodeElement(row["VICTIM_INJURY_2"].ToString());
        //    sb.AppendLine($"<VICTIM_INJURY_2>{temp}</VICTIM_INJURY_2>");

        //    temp = GetMastCodeElement(row["VICTIM_INJURY_3"].ToString());
        //    sb.AppendLine($"<VICTIM_INJURY_3>{temp}</VICTIM_INJURY_3>");

        //    temp = GetMastCodeElement(row["VICTIM_INJURY_4"].ToString());
        //    sb.AppendLine($"<VICTIM_INJURY_4>{temp}</VICTIM_INJURY_4>");

        //    temp = GetMastCodeElement(row["VICTIM_INJURY_5"].ToString());
        //    sb.AppendLine($"<VICTIM_INJURY_5>{temp}</VICTIM_INJURY_5>");

        //    //temp = GetMastCodeElement(row["LEOKA_ACTIVITY"].ToString());
        //    //sb.AppendLine($"<LEOKA_ACTIVITY>{temp}</LEOKA_ACTIVITY>");

        //    //temp = GetMastCodeElement(row["LEOKA_ASSIGNMENT_TYPE"].ToString());
        //    //sb.AppendLine($"<LEOKA_ASSIGNMENT_TYPE>{temp}</LEOKA_ASSIGNMENT_TYPE>");

        //    //temp = GetMastCodeElement(row["LEOKA_ASSAULT_TYPE"].ToString());
        //    //sb.AppendLine($"<LEOKA_ASSAULT_TYPE>{temp}</LEOKA_ASSAULT_TYPE>");

        //    temp = GetConnectedOffenseRecs(row["RECNUM"].ToString());
        //    sb.AppendLine($"<CONNECTED_OFFENSES>{temp}</CONNECTED_OFFENSES>");

        //    temp = GetRelationships(row["RECNUM"].ToString());
        //    sb.AppendLine($"<VICTIM_SUSPECT_RELATIONS>{temp}</VICTIM_SUSPECT_RELATIONS>");

        //    return sb.ToString();
        //}


        //public virtual string GetConnectedOffenseRecs(string linkRec)
        //{
        //    StringBuilder sb = new StringBuilder();
        //    DataTable data = LoadConneectedoffenseRecs(linkRec);

        //    foreach (DataRow row in data.Rows)
        //        sb.AppendLine($"<row>{row["OFFENSE_REC"].ToString()}</row>");

        //    return sb.ToString();
        //}

        //private DataTable LoadConneectedoffenseRecs(string linkRec)
        //{
        //    DataTable result = null;

        //    string sql = GetConneectedOffenseRecsSqlQuery(string.Empty, linkRec, CurrentRecord.RecordNumber.ToString());
        //    result = db.ExecuteSelectCommandMultipleResultDataSet(sql, "INCIDENT_NAME_OFFENSE_LINK").Tables[0];
        //    return result;
        //}

        //public virtual string GetConneectedOffenseRecsSqlQuery(string sql, string linkRec, string incidentRec)
        //{
        //    if (string.IsNullOrWhiteSpace(sql))
        //        sql = $"SELECT OFFENSE_REC FROM WEBRMS.DBO.INCIDENT_NAME_OFFENSE_LINK WHERE INCIDENT_REC = {incidentRec} AND LINK_REC = {linkRec}";

        //    return ReplaceLiterals(sql);
        //}


        //public virtual string GetRelationships(string linkRec)
        //{
        //    StringBuilder sb = new StringBuilder();
        //    DataTable data = LoadRelationships(linkRec);

        //    foreach (DataRow row in data.Rows)
        //    {
        //        var temp = GetRelationshipsColumns(row);
        //        sb.AppendLine($"<row>{temp}</row>");
        //    }

        //    return sb.ToString();
        //}

        //public virtual string GetRelationshipsSqlQuery(string linkRec, string incidentRec)
        //{
        //    string sql = $"SELECT RECNUM, SUSPECT_LINK_REC, RELATIONSHIP_TYPE FROM WEBRMS.DBO.VICTIM_SUSPECT WHERE INCIDENT_REC = {incidentRec} AND VICTIM_LINK_REC = {linkRec}";
        //    return ReplaceLiterals(sql);
        //}

        //private DataTable LoadRelationships(string linkRec)
        //{
        //    DataTable result = null;

        //    string sql = GetRelationshipsSqlQuery(linkRec, CurrentRecord.RecordNumber.ToString());
        //    result = db.ExecuteSelectCommandMultipleResultDataSet(sql, "VICTIM_SUSPECT").Tables[0];
        //    return result;
        //}

        //public virtual string GetRelationshipsColumns(DataRow row)
        //{
        //    if (row == null)
        //        return string.Empty;

        //    StringBuilder sb = new StringBuilder();
        //    string temp = string.Empty;

        //    sb.AppendLine($"<RECNUM>{row["RECNUM"].ToString()}</RECNUM>");
        //    sb.AppendLine($"<SUSPECT_LINK_REC>{row["SUSPECT_LINK_REC"].ToString()}</SUSPECT_LINK_REC>");

        //    temp = GetMastCodeElement(row["RELATIONSHIP_TYPE"].ToString());
        //    sb.AppendLine($"<RELATIONSHIP_TYPE>{temp}</RELATIONSHIP_TYPE>");


        //    return sb.ToString();
        //}


        //public virtual string GetOffenders()
        //{
        //    StringBuilder sb = new StringBuilder();
        //    DataTable data = LoadOffenders();

        //    sb.AppendLine($"<OFFENDERS>");

        //    foreach (DataRow row in data.Rows)
        //    {
        //        var temp = GetOffendersColumns(row);
        //        sb.AppendLine($"<row>{temp}</row>");
        //    }

        //    sb.AppendLine($"</OFFENDERS>");

        //    return sb.ToString();
        //}

        //public virtual string GetOffendersSqlQuery(string incidentRec)
        //{
        //    string sql = $"SELECT * FROM WEBRMS.DBO.LINK WHERE INCIDENT_REC = {incidentRec} AND INVOLVEMENT IN ({GetNibClassRecnums("INVL", "OFFENDER")})";
        //    return ReplaceLiterals(sql);
        //}

        //public virtual DataTable LoadOffenders()
        //{
        //    DataTable result = null;

        //    string sql = GetOffendersSqlQuery(CurrentRecord.RecordNumber.ToString());
        //    result = db.ExecuteSelectCommandMultipleResultDataSet(sql, "OFFENDERS").Tables[0];
        //    return result;
        //}

        //public virtual string GetOffendersColumns(DataRow row)
        //{
        //    if (row == null)
        //        return string.Empty;

        //    StringBuilder sb = new StringBuilder();
        //    string temp = string.Empty;

        //    sb.AppendLine($"<RECNUM>{row["RECNUM"].ToString()}</RECNUM>");
        //    sb.AppendLine($"<AGENCY_REC>{row["AGENCY_REC"].ToString()}</AGENCY_REC>");
        //    sb.AppendLine($"<CREATED_ON>{row["CREATED_ON"].ToString()}</CREATED_ON>");
        //    sb.AppendLine($"<CREATED_BY>{row["CREATED_BY"].ToString()}</CREATED_BY>");
        //    sb.AppendLine($"<UPDATED_ON>{row["UPDATED_ON"].ToString()}</UPDATED_ON>");
        //    sb.AppendLine($"<UPDATED_BY>{row["UPDATED_BY"].ToString()}</UPDATED_BY>");

        //    sb.AppendLine($"<UNIQUEID>{row["UNIQUEID"].ToString()}</UNIQUEID>");
        //    sb.AppendLine($"<DOB>{row["DOB"].ToString()}</DOB>");
        //    sb.AppendLine($"<AGELOW>{row["AGELOW"].ToString()}</AGELOW>");
        //    sb.AppendLine($"<AGEHIGH>{row["AGEHIGH"].ToString()}</AGEHIGH>");
        //    sb.AppendLine($"<NAME_REC>{row["NAME_REC"].ToString()}</NAME_REC>");
        //    sb.AppendLine($"<LNAME>{row["LNAME"].ToString()}</LNAME>");
        //    sb.AppendLine($"<FNAME>{row["FNAME"].ToString()}</FNAME>");


        //    temp = GetMastCodeElement(row["INVOLVEMENT"].ToString());
        //    sb.AppendLine($"<INVOLVEMENT>{temp}</INVOLVEMENT>");

        //    temp = GetMastCodeElement(row["SEX"].ToString());
        //    sb.AppendLine($"<SEX>{temp}</SEX>");

        //    temp = GetMastCodeElement(row["RACE"].ToString());
        //    sb.AppendLine($"<RACE>{temp}</RACE>");

        //    temp = GetMastCodeElement(row["ETHNICITY"].ToString());
        //    sb.AppendLine($"<ETHNICITY>{temp}</ETHNICITY>");

        //    temp = GetMastCodeElement(row["RES_STAT"].ToString());
        //    sb.AppendLine($"<RES_STAT>{temp}</RES_STAT>");


        //    return sb.ToString();
        //}




        //public virtual string GetNibClassRecnums(string type, string nibClass)
        //{
        //    string result = "'-1'";
        //    var rows = this.MASTCODE.Select($"TYPE = '{type}' AND NIB_CLASS = '{nibClass}'").ToList();

        //    if (rows != null && rows.Count > 0)
        //    {
        //        result = string.Empty;
        //        foreach (var row in rows)
        //        {
        //            result = result + $"'{row["RECNUM"].ToString().Trim()}', ";
        //        }

        //        result = result.Trim().TrimEnd(',');
        //    }

        //    return result;
        //}



        //public virtual SourceData TransformSourceDataToCommonIbrData(SourceData sourceRmsData)
        //{
        //    this.SourceRmsData = sourceRmsData;
        //    List<Record> records = sourceRmsData.Records;

        //    if (records == null || records.Count == 0)
        //        return sourceRmsData;

        //    try
        //    {
        //        this.Records = sourceRmsData.Records;
        //        this.MasterData = sourceRmsData.SourceMastInfo;

        //        foreach (Record record in records)
        //        {
        //            if (record == null)
        //                continue;

        //            this.CurrentRecord = record;
        //            record.CommonIbrData = TransformDataToCommonIbrData(record);

        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        ExceptionDispatchInfo.Capture(ex).Throw();
        //    }

        //    return sourceRmsData;
        //}


        //public virtual string TransformDataToCommonIbrData(Record record)
        //{
        //    string result = string.Empty;
        //    StringBuilder sb = new StringBuilder();
        //    string tempStr = string.Empty;

        //    try
        //    {
        //        DataSet dsSource = LoadDataAsXml(record.Source);

        //        sb.AppendLine($"<RMS>");

        //        tempStr = TransformGeneralData(dsSource);
        //        sb.AppendLine($"{tempStr}");

        //        tempStr = TransformAdminData(dsSource);
        //        sb.AppendLine($"{tempStr}");

        //        tempStr = TransformOffenseData(dsSource);
        //        sb.AppendLine($"{tempStr}");

        //        tempStr = TransformPropertyData(dsSource);
        //        sb.AppendLine($"{tempStr}");

        //        tempStr = TransformVehiclesData(dsSource);
        //        sb.AppendLine($"{tempStr}");

        //        tempStr = TransformDrugsData(dsSource);
        //        sb.AppendLine($"{tempStr}");

        //        tempStr = TransformVictimsData(dsSource);
        //        sb.AppendLine($"{tempStr}");

        //        tempStr = TransformOffendersData(dsSource);
        //        sb.AppendLine($"{tempStr}");

        //        tempStr = TransformArrestsData(dsSource);
        //        sb.AppendLine($"{tempStr}");

        //        //tempStr = TransformArrestGroupBData(dsSource);
        //        //sb.AppendLine($"{tempStr}");

        //        sb.AppendLine($"</RMS>");
        //    }
        //    catch (Exception ex)
        //    {
        //        ExceptionDispatchInfo.Capture(ex).Throw();
        //    }

        //    result = sb.ToString();

        //    return result;
        //}


        //public virtual DataSet LoadDataAsXml(string sourceData)
        //{
        //    StringReader reader = new StringReader(sourceData);
        //    DataSet ds = new DataSet();
        //    ds.ReadXml(reader);

        //    return ds;
        //}


        //public virtual string TransformGeneralData(DataSet source)
        //{
        //    string result = string.Empty;



        //    return result;
        //}

        //public virtual string TransformAdminData(DataSet source)
        //{
        //    string xmlString = GetTableXmlData(source, "INCIDENT");
        //    xmlString = $"<INCIDENT>{xmlString}</INCIDENT>";
        //    return xmlString;
        //}


        //public virtual string TransformOffenseData(DataSet source)
        //{
        //    string xmlString = GetTableXmlData(source, "INCIDENT_OFFENSE");
        //    xmlString = $"<OFFENSES>{xmlString}</OFFENSES>";
        //    return xmlString;
        //}

        //public virtual string TransformPropertyData(DataSet source)
        //{
        //    string xmlString = GetTableXmlData(source, "INCIDENT_PROPERTY");
        //    xmlString = $"<PROPERTIES>{xmlString}</PROPERTIES>";
        //    return xmlString;
        //}

        //public virtual string TransformVehiclesData(DataSet source)
        //{
        //    string xmlString = GetTableXmlData(source, "LINKVC");
        //    xmlString = $"<VEHICLES>{xmlString}</VEHICLES>";
        //    return xmlString;
        //}

        //public virtual string TransformDrugsData(DataSet source)
        //{
        //    string xmlString = GetTableXmlData(source, "INCIDENT");
        //    xmlString = $"<DRUGS>{xmlString}</DRUGS>";
        //    return xmlString;
        //}

        //public virtual string TransformVictimsData(DataSet source)
        //{
        //    string xmlString = GetTableXmlData(source, "LINK");
        //    xmlString = $"<VICTIMS>{xmlString}</VICTIMS>";
        //    return xmlString;
        //}

        //public virtual string TransformOffendersData(DataSet source)
        //{
        //    string xmlString = GetTableXmlData(source, "INCIDENT");
        //    xmlString = $"<OFFENDERS>{xmlString}</OFFENDERS>";
        //    return xmlString;
        //}

        //public virtual string TransformArrestsData(DataSet source)
        //{
        //    string xmlString = GetTableXmlData(source, "INCIDENT");
        //    xmlString = $"<ARRESTS>{xmlString}</ARRESTS>";
        //    return xmlString;
        //}

        ////public virtual string TransformArrestGroupBData(DataSet source)
        ////{
        ////    string xmlString = GetTableXmlData(source, "INCIDENT");
        ////    xmlString = $"<ARRESTS>{xmlString}</ARRESTS>";
        ////    return xmlString;
        ////}


        //public virtual string GetTableXmlData(DataSet source, string tableName)
        //{
        //    if (source == null || source.Tables[tableName] == null || source.Tables[tableName].Rows.Count == 0)
        //        return string.Empty;

        //    StringBuilder sb = new StringBuilder();


        //    foreach (DataRow dr in source.Tables[tableName].Rows)
        //    {
        //        foreach (DataColumn dataColumn in source.Tables[tableName].Columns)
        //        {
        //            var column = dataColumn.Caption;
        //            var sourceConfigTableName = SourceColumnsInfo.Where(r => r.TableName.Equals(tableName) && r.ColumnName.Equals(column)).Select(r => r.ColumnSourceTable).FirstOrDefault();

        //            if (string.IsNullOrWhiteSpace(sourceConfigTableName))
        //            {
        //                sb.AppendLine($"<{column}>{dr[$"{column}"].ToString()}</{column}>");
        //            }
        //            else if (sourceConfigTableName.Equals("MASTCODE"))
        //            {
        //                var mastCodeStr = GetMastCodeElement(column.ToString(), dr[$"{column}"].ToString(), this.MasterData.Tables["MASTCODE"]);
        //                sb.AppendLine(mastCodeStr);
        //            }
        //            else if (sourceConfigTableName.Equals("STATUTE_CODE"))
        //            {
        //                var mastCodeStr = GetMastCodeElement(column.ToString(), dr[$"{column}"].ToString(), this.MasterData.Tables["STATUTE_CODE"]);
        //                sb.AppendLine(mastCodeStr);
        //            }
        //        }
        //    }

        //    return sb.ToString();
        //}

        //public virtual string GetMastCodeElement(string tag, string value, DataTable mastcode)
        //{
        //    string result = string.Empty;

        //    StringBuilder sb = new StringBuilder();
        //    DataRow dr = null;

        //    if (!string.IsNullOrWhiteSpace(value))
        //        dr = mastcode.Select($"RECNUM = '{value}'").FirstOrDefault();

        //    string columnValue = string.Empty;

        //    foreach (DataColumn dataColumn in mastcode.Columns)
        //    {
        //        var column = dataColumn.Caption;

        //        if (dr == null)
        //            columnValue = string.Empty;
        //        else
        //            columnValue = dr[column].ToString();

        //        sb.AppendLine($"<{column}>{columnValue}</{column}>");
        //    }

        //    result = $"<{tag}>{sb.ToString()}</{tag}>";

        //    return result;
        //}

    }

}