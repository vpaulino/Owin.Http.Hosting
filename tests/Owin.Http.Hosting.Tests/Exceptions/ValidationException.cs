using System;
using System.Collections.Generic;
using System.Linq;

namespace Owin.Http.Hosting.Tests.Exceptions
{
    public class ValidationException : Exception
    {
        #region Public methods
        public ValidationException(string message) : base(message)
        {
        }

        public override string ToString()
        {
            var flattenedErrors = Errors?.Select(e => $"{e.Domain}: {e.Message}")
                 .Aggregate((current, nextitem) => current + (string.IsNullOrWhiteSpace(current) ? string.Empty : "; ") + nextitem);
            return $"{flattenedErrors} {base.ToString()}";
        }

        #endregion

        #region Public properties

        public List<ErrorItem> Errors { get; set; }

        #endregion
    }
}