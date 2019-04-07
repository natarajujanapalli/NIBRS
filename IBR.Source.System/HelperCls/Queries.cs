using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using System.Configuration;
using System.IO;

namespace IBR.Source.System
{
    public class Queries
    {
        private readonly List<SqlQuery> items = new List<SqlQuery>();
        [XmlElement("SqlQuery")]
        public List<SqlQuery> Items
        {
            get { return items; }
        }


        public static List<SqlQuery> GetQueryInfo()
        {
            string RmsType = ConfigurationManager.AppSettings["RMSType"].ToString().ToUpper().Trim();

            List<SqlQuery> elements = new List<SqlQuery>();

            var ser = new XmlSerializer(typeof(Queries));

            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $@"IBR.Source.Queries\{RmsType}.xml");

            using (var reader = XmlReader.Create(path))
            {
                var wrapper = (Queries)ser.Deserialize(reader);
                if (wrapper != null && wrapper.Items.Count > 0)
                {
                    //elements = new ObservableCollection<Script>(wrapper.Items);
                    elements = wrapper.Items.ToList();
                }
            }

            return elements;
        }
    }


    //public class RootElement
    //{
    //    [XmlElement("SqlServer")]
    //    public DatabaseQueries SqlServer { get; set; }
    //
    //    [XmlElement("Oracle")]
    //    public DatabaseQueries Oracle { get; set; }
    //}
    //
    //
    //public class DatabaseQueries
    //{
    //    [XmlElement("Incident")]
    //    public ModuleQueries Incident { get; set; }
    //
    //    [XmlElement("Arrest")]
    //    public ModuleQueries Arrest { get; set; }
    //
    //    [XmlElement("Juvenile")]
    //    public ModuleQueries Juvenile { get; set; }
    //
    //    [XmlElement("MAST_INFO")]
    //    public ModuleQueries MasterInfo { get; set; }
    //}
    //
    //public class ModuleQueries
    //{
    //    [XmlElement("SqlQuery")]
    //    public List<QueryInfo> SqlQuery { get; set; }
    //}


    public class SqlQuery
    {
        [XmlElement("DATABASE_TYPE")]
        public string DATABASE_TYPE { get; set; }

        [XmlElement("RECORD_TYPE")]
        public string RECORD_TYPE { get; set; }

        [XmlElement("TABLE_NAME")]
        public string TABLE_NAME { get; set; }

        [XmlElement("QUERY")]
        public string QUERY { get; set; }
    }
}