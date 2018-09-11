using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Owin;
using AppFunc = System.Func<System.Collections.Generic.IDictionary<string, object>, System.Threading.Tasks.Task>;

namespace Owin.Http.Hosting.Tests
{
    public class TestOwinMiddleware 
    {
        ManualResetEventSlim resetEvent;

        private AppFunc next;

        public TestOwinMiddleware(AppFunc next)
        {
             
            this.next = next;

        }

        public TestOwinMiddleware(AppFunc next, ManualResetEventSlim resetEvent)
        {
            this.resetEvent = resetEvent;
            this.next = next;
        }
         
        public Task Invoke(IDictionary<string, object> env)
        {

            next(env);
            env["owin.ResponseStatusCode"] = (HttpStatusCode)env["owin.ResponseStatusCode"] == HttpStatusCode.NotFound ? HttpStatusCode.Redirect : env["owin.ResponseStatusCode"];
            resetEvent.Set();

            return Task.CompletedTask;
        }
    }
}
