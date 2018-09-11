#if NET462

using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Owin;

using System;
using System.Web.Http.Dependencies;

namespace Owin.WebSocket
{
    public class WebSocketConnectionMiddleware<T> : OwinMiddleware where T : WebSocketConnection
    {
        private readonly Regex mMatchPattern;
        private readonly IServiceProvider mServiceLocator;
        private static readonly Task completedTask = Task.FromResult(false);

        public WebSocketConnectionMiddleware(OwinMiddleware next, IServiceProvider locator)
            : base(next)
        {
            mServiceLocator = locator;
        }

        public WebSocketConnectionMiddleware(OwinMiddleware next, IServiceProvider locator, Regex matchPattern)
            : this(next, locator)
        {
            mMatchPattern = matchPattern;
        }

        public override Task Invoke(IOwinContext context)
        {
            var matches = new Dictionary<string, string>();

            if (mMatchPattern != null)
            {
                var match = mMatchPattern.Match(context.Request.Path.Value);
                if(!match.Success)
                    return Next?.Invoke(context) ?? completedTask;

                for (var i = 1; i <= match.Groups.Count; i++)
                {
                    var name = mMatchPattern.GroupNameFromNumber(i);
                    var value = match.Groups[i];
                    matches.Add(name, value.Value);
                }
            }
            
            T socketConnection;
            if(mServiceLocator == null)
                socketConnection = Activator.CreateInstance<T>();
            else
                socketConnection = mServiceLocator.GetService((typeof(T))) as T;
            
            return socketConnection.AcceptSocketAsync(context, matches);
          
        }
    }
}
#endif