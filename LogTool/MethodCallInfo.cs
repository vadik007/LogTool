using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogTool
{
    public class MethodCallInfo
    {
        public MethodInfo From { get; set; }
        public MethodInfo To { get; set; }
    }

    public class MethodInfo
    {
        public string SourcePlace { get; set; }
        public string MethodName { get; set; }
        public string ClassName { get; set; }
        public string Message { get; set; }
    }
}
