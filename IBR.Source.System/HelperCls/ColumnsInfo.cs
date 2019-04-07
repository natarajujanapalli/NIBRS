using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace IBR.Source.System
{
    public class ColumnsInfo
    {
        private readonly List<ColumnInfo> items = new List<ColumnInfo>();
        [XmlElement("ColumnInfo")]
        public List<ColumnInfo> Items
        {
            get { return items; }
        }


        public static List<ColumnInfo> GetColumnsInfo()
        {
            List<ColumnInfo> elements = new List<ColumnInfo>();

            var ser = new XmlSerializer(typeof(ColumnsInfo));

            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $@"IBR.Source.Queries\ColumnSource.xml");
            using (var reader = XmlReader.Create(path))
            {
                var wrapper = (ColumnsInfo)ser.Deserialize(reader);
                if (wrapper != null && wrapper.Items.Count > 0)
                {
                    //elements = new ObservableCollection<Script>(wrapper.Items);
                    elements = wrapper.Items.ToList();
                }
            }

            return elements;
        }

    }


    public class ColumnInfo
    {
        [XmlElement("TableName")]
        public string TableName { get; set; }

        [XmlElement("ColumnName")]
        public string ColumnName { get; set; }

        [XmlElement("ColumnSourceTable")]
        public string ColumnSourceTable { get; set; }
    }

}
