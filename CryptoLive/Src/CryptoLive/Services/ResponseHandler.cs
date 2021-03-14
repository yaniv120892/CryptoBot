using System;
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
                    $"{requestDescription}: Response returned with error, Message: {response.Error.Message}, Code: {response.Error.Code}, Data: {response.Error.Data}");
            }

            throw new Exception(
                $"{requestDescription}: Response not success and has no Error, StatusCode: {response.ResponseStatusCode}");
        }
    }
}