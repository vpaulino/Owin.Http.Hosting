using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Owin.Http.Hosting.Tests.Exceptions;

namespace Owin.Http.Hosting.Tests.Controllers
{
    [RoutePrefix("Device")]
    public class DeviceController : ApiController
    {

        [HttpGet]
        [Route("state/{id}")]
        public HttpResponseMessage SetState(string id, bool? active)
        {
            if (string.IsNullOrEmpty(id))
                return new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest);

            if (active == false)
                throw new BusinessException($"device {id} Device cannot be deactivated");

            return new HttpResponseMessage(System.Net.HttpStatusCode.OK);
        }
        
    }
}
