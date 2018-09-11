using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Owin.Http.Hosting.Tests
{
    public interface IVBTraceSource
    {
        bool ShouldTrace(TraceEventType eventType);
        void TraceEvent(TraceEventType eventType, string id, string message);
    }
}
