#if NET462
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Owin;
using Microsoft.Owin.Hosting;
using Microsoft.Owin.Cors;
using System.Web.Cors;
using System.Web.Http;
using System.Net.Http.Formatting;
using System.Net.Http;
using System.Web.Http.Filters;
using System.Web.Http.Dependencies;
using Microsoft.Owin;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Web.Http.Controllers;
using System.Linq.Expressions;
using System.Web.Http.ModelBinding;

namespace Owin.Http.Hosting
{
    

    public class OwinHttpServer : IDisposable 
    {
        IDisposable webApp;
        UriBuilder apiUri;

      

        public OwinHttpServer(UriBuilder apiUri)
        {
            this.apiUri = apiUri;
            this.HttpConfiguration = new HttpConfiguration();

        }

        
        public HttpConfiguration HttpConfiguration { get; private set; }

        public OwinHttpServer AddCors(IAppBuilder builder, IList<string> allowedCorsOrigins)
        {

            if (allowedCorsOrigins != null)
            {
                var corsPolicy = new CorsPolicy
                {
                    AllowAnyMethod = true,
                    AllowAnyHeader = true
                };
                foreach (var origin in allowedCorsOrigins)
                {
                    corsPolicy.Origins.Add(origin);
                }
                var corsOptions = new CorsOptions
                {
                    PolicyProvider = new CorsPolicyProvider
                    {
                        PolicyResolver = context => Task.FromResult(corsPolicy)
                    }
                };

                builder.UseCors(corsOptions);
            }
            return this;
        }


        public virtual OwinHttpServer AddModelBinding<T>(Func<IDependencyResolver, IModelBinder> modelBindingFactoryHandler)
        {
            this.HttpConfiguration.BindParameter((typeof(T)), modelBindingFactoryHandler(this.HttpConfiguration.DependencyResolver));
            return this;
        }

        public virtual OwinHttpServer AddModelBindingWithRule<T>(Func<IDependencyResolver, IModelBinder> modelBinderFactoryHandler, Func<IDependencyResolver, HttpParameterDescriptor, HttpParameterBinding> parameterBindingFactoryHandler )
        {
            this.HttpConfiguration.BindParameter((typeof(T)), modelBinderFactoryHandler(this.HttpConfiguration.DependencyResolver));
            this.HttpConfiguration.ParameterBindingRules.Add(typeof(T), (descriptor) => parameterBindingFactoryHandler(this.HttpConfiguration.DependencyResolver, descriptor));
            return this;
        }

        public virtual OwinHttpServer SetErrorPolicy(IncludeErrorDetailPolicy errorDetailPolicy)
        {
            this.HttpConfiguration.IncludeErrorDetailPolicy = errorDetailPolicy;
            
            return this;
        }

        public virtual OwinHttpServer AddHttpConfiguration(HttpConfiguration configuration)
        {
            this.HttpConfiguration = configuration;
            return this;
        }

        public virtual OwinHttpServer SetHttpRouteAttributes()
        {
            this.HttpConfiguration.MapHttpAttributeRoutes();
            return this;
        }

        public virtual OwinHttpServer ReplaceJsonFormatter(JsonMediaTypeFormatter mediaTypeFormatter)
        {
            
            this.HttpConfiguration.Formatters.Remove(this.HttpConfiguration.Formatters.JsonFormatter);
            if (mediaTypeFormatter != null)
                this.HttpConfiguration.Formatters.Add(mediaTypeFormatter);

            return this;
        }

        public virtual OwinHttpServer ReplaceXmlFormatter(XmlMediaTypeFormatter mediaTypeFormatter)
        {
            this.HttpConfiguration.Formatters.Remove(this.HttpConfiguration.Formatters.XmlFormatter);

            if(mediaTypeFormatter != null)
                this.HttpConfiguration.Formatters.Add(mediaTypeFormatter);

            return this;

        }
        public virtual OwinHttpServer ReplaceService<T>(Func<IDependencyResolver, T> handlerFactory)
        {
            var service = handlerFactory(this.HttpConfiguration.DependencyResolver);
            this.HttpConfiguration.Services.Replace(typeof(T), service);
            return this;
        }

        public virtual OwinHttpServer AddService<T>(Func<IDependencyResolver, T> handlerFactory)
        {
            var service = handlerFactory(this.HttpConfiguration.DependencyResolver);
            this.HttpConfiguration.Services.Add(typeof(T), service);
            return this;
        }

        public virtual OwinHttpServer AddMessageHandlers(Action<Collection<DelegatingHandler>, IDependencyResolver> handlerFactory)
        {
            
            handlerFactory(this.HttpConfiguration.MessageHandlers, this.HttpConfiguration.DependencyResolver);

            return this;
        }

        public virtual OwinHttpServer AddFilter(Action<HttpFilterCollection, IDependencyResolver> filterFactory)
        {
            filterFactory(this.HttpConfiguration.Filters, this.HttpConfiguration.DependencyResolver);
            return this;
        }

        public virtual OwinHttpServer AddDependencyResolver(Func<IDependencyResolver> resolverFactory)
        {
            
            this.HttpConfiguration.DependencyResolver = resolverFactory();
            return this;
        }
        
        List<OwinMiddlewareRegistration> middlewaresRegistrations = new List<OwinMiddlewareRegistration>();
        List<Func<IDictionary<string, object>, Task>> middlewares = new List<Func<IDictionary<string, object>, Task>>();
        public virtual OwinHttpServer AddMiddleware(Func<IDependencyResolver, OwinMiddlewareRegistration> handler)  
        {
            middlewaresRegistrations.Add(handler(this.HttpConfiguration.DependencyResolver));

            return this;

        }

        public virtual OwinHttpServer AddMiddleware(Func<IDependencyResolver, Func<IDictionary<string, object>,Task>> handler)
        {
            middlewares.Add(handler(this.HttpConfiguration.DependencyResolver));

            return this;

        }

        public virtual OwinHttpServer MapRoutes( string routeName, string routeTemplate, object defaults, object constraints = null)
        {
           

            //this.MapPropertyRoutes(configuration, controllerName, basePath);
            this.HttpConfiguration.Routes.MapHttpRoute(
                //name: $"{basePath}{this.Resource}{controllerName}",
                name: routeName,
                routeTemplate: routeTemplate,
                defaults: defaults,
                constraints: constraints);
            return this;

        }

        public virtual OwinHttpServer MapRoutes( string routeName, string routeTemplate, object defaults, object constraints, HttpMessageHandler httpMessageHandler)
        {

            this.HttpConfiguration.Routes.MapHttpRoute(
                name: routeName,
                routeTemplate: routeTemplate,
                defaults: defaults,
                constraints: constraints,
                handler : httpMessageHandler);
            return this;

        }

        private OwinHttpServer UseMiddlewares(IAppBuilder builder)
        {
            void UseMiddleware(IAppBuilder _builder, OwinMiddlewareRegistration item)
            {
                if (item.Arguments == null || (item.Arguments != null && item.Arguments.Count() == 0))
                    _builder.Use(item.Middleware);
                else
                    _builder.Use(item.Middleware, item.Arguments);
            }
            
            foreach (var item in middlewaresRegistrations)                                                                   
            {
                if (!string.IsNullOrWhiteSpace(item.Route))
                {
                    builder.Map(item.Route, a => UseMiddleware(a, item));
                }
                else
                {
                    UseMiddleware(builder, item);
                }
                
            }

            return this;
        }

        public void Build(IAppBuilder appBuilder)
        {
            UseMiddlewares(appBuilder);

            appBuilder.UseWebApi(this.HttpConfiguration);

            HttpConfiguration.EnsureInitialized();
        }

        public virtual void TryStart(StartOptions options = null)
        {
            this.webApp = WebApp.Start(options ?? new StartOptions(apiUri.ToString()), builder =>
            {
                Build(builder);
            });
        }

        public virtual void TryStart<T>(StartOptions options = null)
        {
            try
            {
                this.webApp = WebApp.Start<T>(options ?? new StartOptions(apiUri.ToString()));
            }
            catch (Exception)
            {

                throw;
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    this.webApp.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~HttpServerHost() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion


    }
}
#endif