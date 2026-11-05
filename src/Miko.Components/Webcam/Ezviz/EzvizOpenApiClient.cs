using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Miko.Components.Webcam.Ezviz;

/// <summary>
/// The Ezviz Open Platform HTTP API (<c>https://open.ys7.com/api/lapp/…</c>): form-encoded POSTs
/// answering <c>{ "code": "200", "msg": …, "data": … }</c>. Handles the access token — a fixed one
/// from <see cref="EzvizOptions.AccessToken"/>, or one issued from the app key and secret and renewed
/// before it expires or when the API reports it expired (code 10002).
/// </summary>
public sealed class EzvizOpenApiClient
{
    private const string TokenExpiredCode = "10002";

    private readonly HttpClient _http;
    private readonly EzvizOptions _options;
    private readonly TimeProvider _time;
    private readonly SemaphoreSlim _tokenLock = new(1, 1);
    private string? _token;
    private DateTimeOffset _tokenExpires;

    public EzvizOpenApiClient(HttpClient http, EzvizOptions options, TimeProvider? timeProvider = null)
    {
        _http = http;
        _options = options;
        _time = timeProvider ?? TimeProvider.System;
        _token = string.IsNullOrWhiteSpace(options.AccessToken) ? null : options.AccessToken.Trim();
        _tokenExpires = DateTimeOffset.MaxValue;
    }

    /// <summary>Whether the client can call the API at all (a token, or a key and secret to get one).</summary>
    public bool HasCredentials =>
        _token != null || (!string.IsNullOrWhiteSpace(_options.AppKey) && !string.IsNullOrWhiteSpace(_options.AppSecret));

    /// <summary>
    /// Calls <paramref name="path"/> with the access token and <paramref name="parameters"/>; returns
    /// <c>data</c>. Throws <see cref="WebcamException"/> with the API's code when it is not 200.
    /// </summary>
    public async Task<JsonElement> PostAsync(string path, IEnumerable<KeyValuePair<string, string>> parameters, CancellationToken cancellationToken = default)
    {
        for (int attempt = 0; ; attempt++)
        {
            string token = await GetTokenAsync(forceRefresh: attempt > 0, cancellationToken).ConfigureAwait(false);
            var form = new List<KeyValuePair<string, string>> { new("accessToken", token) };
            form.AddRange(parameters);
            var response = await SendAsync(path, form, cancellationToken).ConfigureAwait(false);
            if (response.Code == TokenExpiredCode && attempt == 0 && CanIssueTokens) continue;
            return response.Data;
        }
    }

    private bool CanIssueTokens => !string.IsNullOrWhiteSpace(_options.AppKey) && !string.IsNullOrWhiteSpace(_options.AppSecret);

    private async Task<string> GetTokenAsync(bool forceRefresh, CancellationToken cancellationToken)
    {
        if (!forceRefresh && _token != null && _time.GetUtcNow() < _tokenExpires) return _token;
        if (!CanIssueTokens)
            return _token ?? throw new WebcamException("Ezviz needs an access token or an app key and secret (EzvizOptions).", "config");

        await _tokenLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!forceRefresh && _token != null && _time.GetUtcNow() < _tokenExpires) return _token;
            var response = await SendAsync("/api/lapp/token/get",
            [
                new("appKey", _options.AppKey!.Trim()),
                new("appSecret", _options.AppSecret!.Trim()),
            ], cancellationToken).ConfigureAwait(false);
            if (response.Code != "200")
                throw new WebcamException(Describe(response.Code, response.Message), response.Code);
            var token = response.Data.GetProperty("accessToken").GetString()
                ?? throw new WebcamException("Ezviz returned no access token.", "token");
            long expires = response.Data.TryGetProperty("expireTime", out var e) && e.TryGetInt64(out long ms) ? ms : 0;
            _token = token;
            // Renew an hour early: tokens last a week, a call must never race the expiry.
            _tokenExpires = expires > 0 ? DateTimeOffset.FromUnixTimeMilliseconds(expires).AddHours(-1) : _time.GetUtcNow().AddDays(1);
            return token;
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    private async Task<ApiResponse> SendAsync(string path, List<KeyValuePair<string, string>> form, CancellationToken cancellationToken)
    {
        var uri = new Uri(new Uri(_options.ApiBaseUrl.TrimEnd('/') + "/"), path.TrimStart('/'));
        using var content = new FormUrlEncodedContent(form);
        using var response = await _http.PostAsync(uri, content, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
            throw new WebcamException($"Ezviz API {path} answered HTTP {(int)response.StatusCode}.", ((int)response.StatusCode).ToString(CultureInfo.InvariantCulture));
        var body = await response.Content.ReadFromJsonAsync(EzvizJsonContext.Default.ApiResponse, cancellationToken).ConfigureAwait(false)
            ?? throw new WebcamException($"Ezviz API {path} returned no body.");
        if (body.Code != "200" && !(body.Code == TokenExpiredCode && CanIssueTokens))
            throw new WebcamException(Describe(body.Code, body.Message), body.Code);
        return body;
    }

    /// <summary>The API's message, made specific for the codes a camera UI runs into.</summary>
    internal static string Describe(string? code, string? message) => code switch
    {
        "10001" or "10002" => "The Ezviz access token is invalid or expired.",
        "10005" or "10017" => "The Ezviz app key is invalid.",
        "20002" or "20018" => "The device is not on this Ezviz account.",
        "20007" => "The device is offline.",
        "20008" => "The device did not respond in time.",
        "60000" => "The device does not support pan / tilt.",
        "60001" => "This account may not control the device.",
        "60002" => "The camera cannot turn further up.",
        "60003" => "The camera cannot turn further down.",
        "60004" => "The camera cannot turn further left.",
        "60005" => "The camera cannot turn further right.",
        "60006" => "Pan / tilt failed.",
        "60020" => "The device does not support this command.",
        "9048" or "399048" => "The account's limit of simultaneous viewers is reached.",
        "9049" or "399049" => "The free plan's stream limit is reached.",
        _ => string.IsNullOrWhiteSpace(message) ? $"Ezviz error {code}." : $"{message} ({code})",
    };

    internal sealed class ApiResponse
    {
        [JsonPropertyName("code")]
        public string? Code { get; set; }

        [JsonPropertyName("msg")]
        public string? Message { get; set; }

        [JsonPropertyName("data")]
        public JsonElement Data { get; set; }
    }
}

[JsonSerializable(typeof(EzvizOpenApiClient.ApiResponse))]
[JsonSourceGenerationOptions(NumberHandling = JsonNumberHandling.AllowReadingFromString)]
internal sealed partial class EzvizJsonContext : JsonSerializerContext;
