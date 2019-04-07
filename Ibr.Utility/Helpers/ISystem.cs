using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IBR.Utility
{
    public interface ISystem
    {
        void LoadRecordsData(List<Record> records);
    }
}
