using System;
using Microsoft.AspNetCore.Mvc.ActionConstraints;

namespace Library.API.Helpers
{
    [AttributeUsage(AttributeTargets.All, Inherited = true, AllowMultiple = true)]
    public class RequestHeaderMatchesMediaTypeAttribute : Attribute, IActionConstraint
    {
        private readonly string[] _mediaTypes;
        private readonly string _requestHeaderToMatch;

        public RequestHeaderMatchesMediaTypeAttribute(string requestHeaderToMatch, string[] mediaTypes)
        {
            _mediaTypes = mediaTypes;
            _requestHeaderToMatch = requestHeaderToMatch;
        }

        public bool Accept(ActionConstraintContext context)
        {
            var requestHeaders = context.RouteContext.HttpContext.Request.Headers;

            if (!requestHeaders.ContainsKey(_requestHeaderToMatch))
            {
                return false;
            }

            foreach (var mediaType in _mediaTypes)
            {
                var mediaTypesMathes = string.Equals(requestHeaders[_requestHeaderToMatch].ToString(),
                    mediaType, StringComparison.OrdinalIgnoreCase);

                if (mediaTypesMathes)
                {
                    return true;
                }
            }

            return false;
        }

        public int Order
        {
            get { return 0; }
        }
    }
}
