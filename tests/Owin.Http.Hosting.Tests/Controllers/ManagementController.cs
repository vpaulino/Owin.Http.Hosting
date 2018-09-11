using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace Owin.Http.Hosting.Tests.Controllers
{
    
    public class ManagementController : ApiController
    {
        [HttpGet]
        [Route("GetState", Name ="GetState")]
        public HttpResponseMessage GetState(string id)
        {
            if(string.IsNullOrEmpty(id))
                return new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest);

            return new HttpResponseMessage(System.Net.HttpStatusCode.OK);
        }

        [HttpGet]
        [Route("GetProperty", Name = "GetProperty")]
        public HttpResponseMessage GetProperty(string id)
        {
            if (string.IsNullOrEmpty(id))
                return new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest);

            return new HttpResponseMessage(System.Net.HttpStatusCode.OK);
        }
    }
}
