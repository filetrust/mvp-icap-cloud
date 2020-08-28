using Flurl.Http;
using Polly;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;

namespace DurableFileProcessing.Services
{
    public static class PollyPolicies
    {
        private static HttpStatusCode[] _httpStatusCodesWorthRetrying =
        {
            HttpStatusCode.RequestTimeout,
            HttpStatusCode.InternalServerError,
            HttpStatusCode.BadGateway,
            HttpStatusCode.ServiceUnavailable,
            HttpStatusCode.GatewayTimeout
        };

        public static AsyncPolicy<HttpResponseMessage> ApiRetryPolicy => Policy
                    .Handle<FlurlHttpException>()
                    .OrResult<HttpResponseMessage>(r => _httpStatusCodesWorthRetrying.Contains(r.StatusCode))
                    .RetryAsync(5);
    }
}
