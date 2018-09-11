using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.ExceptionHandling;
using Microsoft.Owin;
using Microsoft.Owin.Logging;
using Microsoft.Owin.Testing;
using Moq;
using SimpleInjector;
using SimpleInjector.Integration.WebApi;

using Owin.Http.Hosting.Tests.Controllers;
using Xunit;

namespace Owin.Http.Hosting.Tests
{
    public class HttpServerHostTests
    {
        TestServer testServer;
        
        public HttpServerHostTests()
        {
          
           
        }

        private void RegisterManualResetEvent(ref SimpleInjector.Container container, ManualResetEventSlim resetEvent)
        {
            try
            {
                container.Register<ManualResetEventSlim>(() => resetEvent);
            }
            catch (Exception ex)
            {

                throw;
            }
        }

        private void RegisterExceptionLogger(ref SimpleInjector.Container container, ManualResetEventSlim resetEvent)
        {
            try
            {
                var mock = new Mock<IExceptionLogger>();

                mock.Setup((exceptionLogger) => exceptionLogger.LogAsync(It.IsAny<ExceptionLoggerContext>(), It.IsAny<CancellationToken>())).Callback(() => resetEvent.Set());

                container.Register<IExceptionLogger>(() => mock.Object);
            }
            catch (Exception ex)
            {

                throw;
            }
        }

        private static void RegisterVBTraceSource(ref SimpleInjector.Container container)
        {
            try
            {
                var mock = new Mock<IVBTraceSource>();

                mock.Setup((traceSource) => traceSource.TraceEvent(It.IsAny<TraceEventType>(), It.IsAny<string>(), It.IsAny<string>()));


                container.Register<IVBTraceSource>(() => mock.Object);
            }
            catch (Exception ex)
            {

                throw;
            }

        }


        TestServer CreateTestServer(OwinHttpServer httpServer)
        {
            return  TestServer.Create(builder =>
            {
                httpServer.Build(builder);
            });

        }

        HttpClient CreateHttpClient(TestServer httpServer)
        {
            return new HttpClient(httpServer.Handler);
        }

        [Fact]
        public async Task CreateSimpleHttpServer_RoutingWithExplicitRoutes_RequestDefaultRoute()
        {
            
                var httpServer = new OwinHttpServer(new UriBuilder("http://localhost:5000/"));

                httpServer.AddDependencyResolver(() => new SimpleInjectorWebApiDependencyResolver(new SimpleInjector.Container()))
                      .MapRoutes("GetState", "{controller}/{action}/{id}", new { controller = nameof(ManagementController), action = "GetState", id = RouteParameter.Optional });


                HttpClient client = CreateHttpClient(CreateTestServer(httpServer));
                client.BaseAddress = new Uri("http://localhost");
                var result = await client.GetAsync($"/Management/GetState/{1}");

                Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            
           

        }

        [Fact]
        public async Task CreateSimpleHttpServer_RoutingWithExplicitRoutes_RequestDiferentThenDefault()
        {

            var httpServer = new OwinHttpServer(new UriBuilder("http://localhost:5000/"));

            httpServer.AddDependencyResolver(() => new SimpleInjectorWebApiDependencyResolver(new SimpleInjector.Container()))
                      .MapRoutes("GetState", "{controller}/{action}/{id}", new { controller = nameof(ManagementController), action = "GetState", id = RouteParameter.Optional });
 
                      
            HttpClient client = CreateHttpClient(CreateTestServer(httpServer));
            client.BaseAddress = new Uri("http://localhost");
             var result = await client.GetAsync($"/Management/GetProperty/{1}");

            Assert.Equal(HttpStatusCode.OK, result.StatusCode);



        }


        [Fact]
        public async Task CreateSimpleHttpServer_RoutingWithHttpAttributes()
        {
            var httpServer = new OwinHttpServer(new UriBuilder("http://localhost:5000/"));

            httpServer.AddDependencyResolver(() => new SimpleInjectorWebApiDependencyResolver(new SimpleInjector.Container()))
                      .SetHttpRouteAttributes();

            HttpClient client = CreateHttpClient(CreateTestServer(httpServer));
            client.BaseAddress = new Uri("http://localhost");

            var result = await client.GetAsync($"/Device/state/{1}?active=");

            Assert.Equal(HttpStatusCode.OK, result.StatusCode);

        }

        [Fact]
        public async Task CreateSimpleHttpServer_RoutingWithHttpAttributes_FilterExceptions()
        {
            var httpServer = new OwinHttpServer(new UriBuilder("http://localhost:5000/"));

            httpServer.AddDependencyResolver(() => new SimpleInjectorWebApiDependencyResolver(new SimpleInjector.Container()))
                      .SetHttpRouteAttributes()
                      .AddFilter((filterCollection, serviceProvider) =>
                      {
                          filterCollection.Add(new PlatformExceptionFilter());
                      });


            HttpClient client = CreateHttpClient(CreateTestServer(httpServer));
            client.BaseAddress = new Uri("http://localhost");

            var result = await client.GetAsync($"/Device/state/{1}?active=false");

            Assert.Equal(HttpStatusCode.Conflict, result.StatusCode);

        }

        [Fact]
        public async Task CreateSimpleHttpServer_RoutingWithHttpAttributes_FilterExceptionsWithDependencies()
        {
            var httpServer = new OwinHttpServer(new UriBuilder("http://localhost:5000/"));

            var container = new SimpleInjector.Container();

            RegisterVBTraceSource(ref container);

            httpServer.AddDependencyResolver(() => new SimpleInjectorWebApiDependencyResolver(container))
                      .SetHttpRouteAttributes()
                      .AddFilter((filterCollection, serviceResolver) =>
                      {

                          IVBTraceSource traceSource = serviceResolver.GetService(typeof(IVBTraceSource)) as IVBTraceSource;
                          filterCollection.Add(new PlatformExceptionFilter(traceSource));

                      });

            HttpClient client = CreateHttpClient(CreateTestServer(httpServer));
            client.BaseAddress = new Uri("http://localhost");

            var result = await client.GetAsync($"/Device/state/{1}?active=false");

            Assert.Equal(HttpStatusCode.Conflict, result.StatusCode);

        }


        [Fact]
        public async Task CreateSimpleHttpServer_RoutingWithHttpAttributes_AddService()
        {
            using (var httpServer = new OwinHttpServer(new UriBuilder("http://localhost:5000/")))
            {
                var container = new SimpleInjector.Container();

                var resetEvent = new ManualResetEventSlim();

                RegisterExceptionLogger(ref container, resetEvent);

                httpServer.AddDependencyResolver(() => new SimpleInjectorWebApiDependencyResolver(container))
                          .SetHttpRouteAttributes()
                          .AddService<IExceptionLogger>((serviceResolver) =>
                          {
                              IExceptionLogger traceSource = serviceResolver.GetService(typeof(IExceptionLogger)) as IExceptionLogger;
                              return traceSource;

                          }).TryStart();

                HttpClient client = new HttpClient();
                client.BaseAddress = new Uri("http://localhost:5000/");
                var result = await client.GetAsync($"/Device/state/{1}?active=false");

                resetEvent.Wait();

                Assert.Equal(HttpStatusCode.InternalServerError, result.StatusCode);

            }


        }

        [Fact]
        public async Task CreateSimpleHttpServer_RoutingWithHttpAttributes_ReplaceService()
        {
            using (var httpServer = new OwinHttpServer(new UriBuilder("http://localhost:5001/")))
            {

                var container = new SimpleInjector.Container();

                var resetEvent = new ManualResetEventSlim();

                RegisterExceptionLogger(ref container, resetEvent);

                httpServer.AddDependencyResolver(() => new SimpleInjectorWebApiDependencyResolver(container))
                          .SetHttpRouteAttributes()
                          .ReplaceService<IExceptionLogger>((serviceResolver) =>
                          {
                              IExceptionLogger exceptionLogger = serviceResolver.GetService(typeof(IExceptionLogger)) as IExceptionLogger;
                              return exceptionLogger;

                          }).TryStart();

                HttpClient client = new HttpClient();
                client.BaseAddress = new Uri("http://localhost:5001/");

                var result = await client.GetAsync($"/Device/state/{1}?active=false");

                resetEvent.Wait();

                Assert.Equal(HttpStatusCode.InternalServerError, result.StatusCode);
            }

          

        }

        [Fact]
        public async Task RegisterGlobalMiddleware_RoutingWithHttpAttributes__WithNoArguments_AndNotRoutes()
        {
            var httpServer = new OwinHttpServer(new UriBuilder("http://localhost:5000/"));

            var container = new SimpleInjector.Container();
            var resetEvent = new ManualResetEventSlim();

            RegisterManualResetEvent(ref container, resetEvent);

            httpServer.AddDependencyResolver(() => new SimpleInjectorWebApiDependencyResolver(container))
                      .SetHttpRouteAttributes()
                      .AddMiddleware((iocResolver) =>
                      {
                          var _resetEvent = iocResolver.GetService(typeof(ManualResetEventSlim));
                          return new OwinMiddlewareRegistration(typeof(TestOwinMiddleware), new object[] { _resetEvent });
                      });
                      

            HttpClient client = CreateHttpClient(CreateTestServer(httpServer));

            client.BaseAddress = new Uri("http://localhost");

            var result = await client.GetAsync($"/Device/state/{1}?active=");

            resetEvent.Wait();

            Assert.Equal(HttpStatusCode.OK, result.StatusCode);

        }

        [Fact]
        public async Task CreateSimpleHttpServer_RoutingWithHttpAttributes_RoutedMiddleware()
        {
            var httpServer = new OwinHttpServer(new UriBuilder("http://localhost:5000/"));

            var container = new SimpleInjector.Container();
            var resetEvent = new ManualResetEventSlim();

            RegisterManualResetEvent(ref container, resetEvent);

            httpServer.AddDependencyResolver(() => new SimpleInjectorWebApiDependencyResolver(container))
                      .SetHttpRouteAttributes()
                      .AddMiddleware((iocResolver) => 
                      {
                          var _resetEvent = iocResolver.GetService(typeof(ManualResetEventSlim));

                          return new OwinMiddlewareRegistration(typeof(TestOwinMiddleware), new object[] { _resetEvent }, "/WebSockets");
                      });

            HttpClient client = CreateHttpClient(CreateTestServer(httpServer));

            client.BaseAddress = new Uri("http://localhost");

            var result = await client.GetAsync($"/WebSockets/");

            resetEvent.Wait();

            Assert.Equal(HttpStatusCode.Redirect, result.StatusCode);

        }


      


    }
}
