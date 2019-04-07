using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IBR.Utility
{
    public class Record
    {
        public string RecordID { get { return $"{RecordType}{RecordNumber}"; } }
        public string RecordType { get; set; }
        public long RecordNumber { get; set; }
        public long AgencyNumber { get; set; }
        public string State { get; set; }

        public string Source { get; set; }
        public string CommonIbrData { get; set; }
        public string Validations { get; set; }
        public string NIBRSData { get; set; }
        public string OutPut { get; set; }
    }
}
