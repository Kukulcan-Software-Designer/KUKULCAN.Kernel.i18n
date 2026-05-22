using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ATLAS.Kernel.i18n.Client.Models;

namespace ATLAS.Kernel.i18n.Client.ApiClient;

/// <summary>
/// Typed HTTP client for the ATLAS.Kernel.i18n REST API.
/// Covers all endpoints exposed by Languages, Locales, Currencies, and Translations controllers.
/// </summary>
public sealed class I18NApiClient(HttpClient http)
{
    private static readonly JsonSerializerOptions _jsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<ApiResult<T>> GetAsync<T>(string url, CancellationToken ct = default)
    {
        HttpResponseMessage response = await http.GetAsync(url, ct);
        return await ParseAsync<T>(response);
    }

    private async Task<ApiResult<T>> PostAsync<T>(string url, object body, CancellationToken ct = default)
    {
        HttpResponseMessage response = await http.PostAsJsonAsync(url, body, _jsonOpts, ct);
        return await ParseAsync<T>(response);
    }

    private async Task<ApiResult<T>> PutAsync<T>(string url, object body, CancellationToken ct = default)
    {
        HttpResponseMessage response = await http.PutAsJsonAsync(url, body, _jsonOpts, ct);
        return await ParseAsync<T>(response);
    }

    private async Task<ApiResult<Unit>> PatchAsync(string url, object body, CancellationToken ct = default)
    {
        var content  = JsonContent.Create(body, options: _jsonOpts);
        var request  = new HttpRequestMessage(HttpMethod.Patch, url) { Content = content };
        HttpResponseMessage response = await http.SendAsync(request, ct);
        return response.IsSuccessStatusCode
            ? ApiResult<Unit>.Ok(Unit.Value)
            : ApiResult<Unit>.Fail(await ReadError(response));
    }

    private async Task<ApiResult<Unit>> DeleteAsync(string url, CancellationToken ct = default)
    {
        HttpResponseMessage response = await http.DeleteAsync(url, ct);
        return response.IsSuccessStatusCode
            ? ApiResult<Unit>.Ok(Unit.Value)
            : ApiResult<Unit>.Fail(await ReadError(response));
    }

    private static async Task<ApiResult<T>> ParseAsync<T>(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<T>(_jsonOpts);
            return ApiResult<T>.Ok(result!);
        }
        return ApiResult<T>.Fail(await ReadError(response));
    }

    private static async Task<ApiError> ReadError(HttpResponseMessage response)
    {
        try
        {
            var err = await response.Content.ReadFromJsonAsync<ApiError>(_jsonOpts);
            return err ?? new ApiError(response.ReasonPhrase, (int)response.StatusCode, null);
        }
        catch
        {
            return new ApiError(response.ReasonPhrase, (int)response.StatusCode, null);
        }
    }

    // ── Languages ─────────────────────────────────────────────────────────────

    public Task<ApiResult<IReadOnlyList<LanguageDto>>> GetAllLanguagesAsync(bool activeOnly = true, CancellationToken ct = default)
        => GetAsync<IReadOnlyList<LanguageDto>>($"api/v1/languages?activeOnly={activeOnly}", ct);

    public Task<ApiResult<LanguageDto>> GetLanguageAsync(string code, CancellationToken ct = default)
        => GetAsync<LanguageDto>($"api/v1/languages/{code}", ct);

    public Task<ApiResult<LanguageDto>> CreateLanguageAsync(CreateLanguageRequest body, CancellationToken ct = default)
        => PostAsync<LanguageDto>("api/v1/languages", body, ct);

    public Task<ApiResult<LanguageDto>> UpdateLanguageAsync(string code, UpdateLanguageRequest body, CancellationToken ct = default)
        => PutAsync<LanguageDto>($"api/v1/languages/{code}", body, ct);

    public Task<ApiResult<Unit>> SetLanguageActiveAsync(string code, bool isActive, CancellationToken ct = default)
        => PatchAsync($"api/v1/languages/{code}/active", new SetActiveRequest(isActive), ct);

    public Task<ApiResult<Unit>> SetDefaultLanguageAsync(string code, CancellationToken ct = default)
        => PatchAsync($"api/v1/languages/{code}/default", new { }, ct);

    // ── Locales ───────────────────────────────────────────────────────────────

    public Task<ApiResult<IReadOnlyList<LocaleConfigurationDto>>> GetAllLocalesAsync(CancellationToken ct = default)
        => GetAsync<IReadOnlyList<LocaleConfigurationDto>>("api/v1/locales", ct);

    public Task<ApiResult<LocaleConfigurationDto>> GetLocaleAsync(string languageCode, CancellationToken ct = default)
        => GetAsync<LocaleConfigurationDto>($"api/v1/locales/{languageCode}", ct);

    public Task<ApiResult<LocaleConfigurationDto>> UpsertLocaleAsync(string languageCode, UpsertLocaleRequest body, CancellationToken ct = default)
        => PutAsync<LocaleConfigurationDto>($"api/v1/locales/{languageCode}", body, ct);

    // ── Currencies ────────────────────────────────────────────────────────────

    public Task<ApiResult<IReadOnlyList<CurrencyFormatDto>>> GetCurrenciesAsync(string languageCode, CancellationToken ct = default)
        => GetAsync<IReadOnlyList<CurrencyFormatDto>>($"api/v1/currencies/{languageCode}", ct);

    public Task<ApiResult<CurrencyFormatDto>> UpsertCurrencyAsync(string languageCode, string currencyCode, UpsertCurrencyRequest body, CancellationToken ct = default)
        => PutAsync<CurrencyFormatDto>($"api/v1/currencies/{languageCode}/{currencyCode}", body, ct);

    public Task<ApiResult<Unit>> DeleteCurrencyAsync(string languageCode, string currencyCode, CancellationToken ct = default)
        => DeleteAsync($"api/v1/currencies/{languageCode}/{currencyCode}", ct);

    // ── Translations ──────────────────────────────────────────────────────────

    public Task<ApiResult<TranslationLookupDto>> GetTranslationAsync(string code, string languageCode, CancellationToken ct = default)
        => GetAsync<TranslationLookupDto>($"api/v1/translations/{code}/{languageCode}", ct);

    public Task<ApiResult<IReadOnlyList<TranslationDto>>> GetTranslationVariantsAsync(string code, CancellationToken ct = default)
        => GetAsync<IReadOnlyList<TranslationDto>>($"api/v1/translations/{code}/variants", ct);

    public Task<ApiResult<TranslationMapDto>> GetModuleTranslationsAsync(string module, string languageCode, CancellationToken ct = default)
        => GetAsync<TranslationMapDto>($"api/v1/translations/module/{module}/{languageCode}", ct);

    public Task<ApiResult<PagedResult<TranslationDto>>> GetTranslationsPagedAsync(
        int page = 1, int pageSize = 50, string? module = null, string? languageCode = null, string? sortBy = null,
        CancellationToken ct = default)
    {
        var url = $"api/v1/translations?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(module))       url += $"&module={module}";
        if (!string.IsNullOrWhiteSpace(languageCode)) url += $"&languageCode={languageCode}";
        if (!string.IsNullOrWhiteSpace(sortBy))       url += $"&sortBy={sortBy}";
        return GetAsync<PagedResult<TranslationDto>>(url, ct);
    }

    public Task<ApiResult<TranslationDto>> CreateTranslationAsync(CreateTranslationRequest body, CancellationToken ct = default)
        => PostAsync<TranslationDto>("api/v1/translations", body, ct);

    public Task<ApiResult<TranslationDto>> UpdateTranslationAsync(string code, string languageCode, UpdateTranslationRequest body, CancellationToken ct = default)
        => PutAsync<TranslationDto>($"api/v1/translations/{code}/{languageCode}", body, ct);

    public Task<ApiResult<Unit>> SetTranslationReviewedAsync(string code, string languageCode, bool isReviewed, CancellationToken ct = default)
        => PatchAsync($"api/v1/translations/{code}/{languageCode}/review", new SetReviewedRequest(isReviewed), ct);

    public Task<ApiResult<Unit>> DeleteTranslationAsync(string code, string languageCode, CancellationToken ct = default)
        => DeleteAsync($"api/v1/translations/{code}/{languageCode}", ct);

    public Task<ApiResult<BulkUpsertResultDto>> BulkUpsertTranslationsAsync(BulkUpsertRequest body, CancellationToken ct = default)
        => PostAsync<BulkUpsertResultDto>("api/v1/translations/bulk", body, ct);
}

// ── Result wrapper ────────────────────────────────────────────────────────────
public sealed class ApiResult<T>
{
    public bool     IsSuccess { get; private init; }
    public T?       Value     { get; private init; }
    public ApiError? Error    { get; private init; }

    public static ApiResult<T> Ok(T value)    => new() { IsSuccess = true,  Value = value };
    public static ApiResult<T> Fail(ApiError error) => new() { IsSuccess = false, Error = error };
}

public readonly struct Unit
{
    public static readonly Unit Value = default;
}
