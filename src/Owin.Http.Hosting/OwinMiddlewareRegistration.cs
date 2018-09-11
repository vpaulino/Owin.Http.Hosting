using System;

namespace Owin.Http.Hosting
{
    public class OwinMiddlewareRegistration
    {

        public OwinMiddlewareRegistration(Type middleware)
        {
            this.Middleware = middleware;
        }


        public OwinMiddlewareRegistration(Type middleware, object[] arguments) : this(middleware)
        {

            this.Arguments = arguments;
        }

        public OwinMiddlewareRegistration(Type middleware, object[] arguments, string route) : this(middleware, arguments)
        {
            this.Route = route;
        }

        public Type Middleware { get; set; }

        public object[] Arguments { get; set; }

        public string Route { get; set; }

    }
}
