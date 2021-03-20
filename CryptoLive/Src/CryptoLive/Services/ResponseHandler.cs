#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using CryptoExchange.Net.Objects;

namespace Services
{
    public class ResponseHandler
    {
        internal static void AssertSuccessResponse<T>(WebCallResult<T> response, string requestDescription)
        {
            if (response.Success)
            {
                return;
            }

            if (!(response.Error is null))
            {
                throw new Exception(
                    $"{requestDescription}: Response returned with error, " +
                    $"Message: {response.Error}, " +
                    $"Code: {response.Error.Code}, " +
                    $"Response status code: {response.ResponseStatusCode}, " +
                    $"Data: {response.Error.Data}, " +
                    $"ResponseHeader: {string.Join(",", GetResponseHeaderDescriptions(response.ResponseHeaders))}");
            }

            throw new Exception(
                $"{requestDescription}: Response not success and has no Error, StatusCode: {response.ResponseStatusCode}");
        }

        private static List<string> GetResponseHeaderDescriptions(IEnumerable<KeyValuePair<string, IEnumerable<string>>>? responseResponseHeaders)
        {
            if (responseResponseHeaders is null)
            {
                return new List<string>();
            }

            return responseResponseHeaders.Select(m => $"[{m.Key}]={m.Value}").ToList();
        }
    }
}