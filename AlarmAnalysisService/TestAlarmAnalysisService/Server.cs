using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlarmAnalysisService
{
    public enum ServerType { ORACLE, MYSQL, TCPIP, SCADA, OPC, SQLITE };
    public abstract class Server
    {
        public string name;
        public ServerType serverType;
        public abstract void Init();
        public abstract List<RawTable> GetRawData(string name, bool exactMatch = false);     //get RawData by name
        public abstract DataTable GetQueryData(string sqlstr);     //get RawData by name
        public abstract bool SendData(string command);
        public abstract void Close();           //close connection
        public static readonly log4net.ILog log = DebugUtil.Instance.LOG;
    }
}
