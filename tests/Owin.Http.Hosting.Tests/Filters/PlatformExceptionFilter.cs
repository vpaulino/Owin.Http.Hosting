using System;
using System.Net;
using System.Net.Http;
using System.Web.Http.Filters;
using Owin.Http.Hosting.Tests.Exceptions;

namespace Owin.Http.Hosting.Tests
{
    public class PlatformExceptionFilter : ExceptionFilterAttribute
    {
        #region Private methods

        public PlatformExceptionFilter() { }

        private IVBTraceSource traceSource;
        public PlatformExceptionFilter(IVBTraceSource traceSource)
        {
            this.traceSource = traceSource;
        }

        private static HttpResponseMessage GetErrorResponse(HttpRequestMessage request, Exception exception)
        {
            var aggregateException = exception as AggregateException;
            if (aggregateException != null)
            {
                return GetErrorResponse(request, aggregateException.InnerException);
            }

            var platformException = exception as ValidationException;
            if (platformException != null)
            {
                return request.CreateResponse(HttpStatusCode.BadRequest, new ResponseStatus
                {
                    Message = platformException.Message,
                    Code = (int)HttpStatusCode.BadRequest,
                    Errors = platformException.Errors?.ToArray()
                });
            }
            var businessException = exception as BusinessException; //TODO: check usage of this
            if (businessException != null)
            {
                return request.CreateResponse(HttpStatusCode.Conflict, new ResponseStatus
                {
                    Message = businessException.Message,
                    Code = (int)HttpStatusCode.Conflict,
                });
            }
            return request.CreateResponse(HttpStatusCode.InternalServerError, new ResponseStatus
            {
                Message = "Internal server error",
                Code = (int)HttpStatusCode.InternalServerError,
            });
        }

        #endregion

        #region Public methods

        public override void OnException(HttpActionExecutedContext actionExecutedContext)
        {
            actionExecutedContext.Response = GetErrorResponse(actionExecutedContext.Request, actionExecutedContext.Exception);
        }

        #endregion
        public bool AllowMultiple => throw new System.NotImplementedException();
    }
    
}