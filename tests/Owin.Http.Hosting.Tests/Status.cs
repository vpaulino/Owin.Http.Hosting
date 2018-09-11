
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Owin.Http.Hosting.Tests
{
    public class ErrorItem
    {
        public string Message { get; set; }
        public string Domain { get; set; }

    }
    public class ResponseStatus
    {
        public int Code { get; set; } = (int)HttpStatusCode.OK;
        public string Message { get; set; }
        public ErrorItem[] Errors { get; set; }
    }
}
