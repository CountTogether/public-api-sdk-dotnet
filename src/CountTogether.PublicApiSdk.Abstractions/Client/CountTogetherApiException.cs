using System.Net;
using System.Net.Http;

namespace CountTogether.PublicApiSdk.Abstractions.Client;

/// <summary>
/// Thrown when the CountTogether Public API answers with an unsuccessful status code.
/// <see cref="HttpRequestException.StatusCode"/> contains the HTTP status code.
/// </summary>
public sealed class CountTogetherApiException : HttpRequestException
{
    /// <summary>
    /// Creates a new <see cref="CountTogetherApiException"/>.
    /// </summary>
    public CountTogetherApiException(string message, HttpStatusCode statusCode, int? errorCode, string? responseBody)
        : base(message, null, statusCode)
    {
        ErrorCode = errorCode;
        ResponseBody = responseBody;
    }

    /// <summary>
    /// The machine-readable CountTogether error code (for example <c>-1100</c> for not found), or <c>null</c> when
    /// the response did not contain one.
    /// </summary>
    public int? ErrorCode { get; }

    /// <summary>
    /// The raw response body, or <c>null</c> when it was empty.
    /// </summary>
    public string? ResponseBody { get; }
}
