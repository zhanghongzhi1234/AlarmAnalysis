using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlarmAnalysisService
{
    class Program
    {
        static void Main(string[] args)
        {
            AlarmAnalysisService service = new AlarmAnalysisService();
            while (true)
            {
                System.Threading.Thread.Sleep(60000);
            }
        }
    }
}
