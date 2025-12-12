using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using DbDesigner.Core.Schema;
using DbDesigner.Core.SchemaChanges;

namespace DbDesigner.AI;

public class ApiChangeProposalService : IChangeProposalService
{
    private readonly HttpClient _httpClient;
    private readonly Func<ApiSettings> _getSettings;

    public ApiChangeProposalService(HttpClient httpClient, Func<ApiSettings> getSettings)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _getSettings = getSettings ?? throw new ArgumentNullException(nameof(getSettings));
    }

    public async Task<IReadOnlyList<SchemaChange>> ProposeChangesAsync(string specification, DatabaseSchema? currentSchema)
    {
        string? requestJson = null;
        string? responseJson = null;
        string? error = null;
        IReadOnlyList<SchemaChange> mapped = Array.Empty<SchemaChange>();

        try
        {
            var settings = _getSettings() ?? new ApiSettings();
            if (!IsValidSettings(settings, out var uri))
            {
                error = "Invalid API settings";
                return Array.Empty<SchemaChange>();
            }

            var payload = BuildRequest(specification, currentSchema);
            requestJson = JsonSerializer.Serialize(payload, JsonOptions);

            using var request = new HttpRequestMessage(HttpMethod.Post, uri);
            request.Headers.TryAddWithoutValidation("X-goog-api-key", settings.ApiKey);
            request.Content = new StringContent(requestJson, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                responseJson = await response.Content.ReadAsStringAsync();
                error = $"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}";
                return Array.Empty<SchemaChange>();
            }

            responseJson = await response.Content.ReadAsStringAsync();
            var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(responseJson, JsonOptions);
            var responseText = geminiResponse?.Candidates?.FirstOrDefault()
                ?.Content?.Parts?.FirstOrDefault()?.Text;

            if (string.IsNullOrWhiteSpace(responseText))
            {
                error = "Empty Gemini response text";
                return Array.Empty<SchemaChange>();
            }

            ApiResponse? apiResponse;
            try
            {
                apiResponse = JsonSerializer.Deserialize<ApiResponse>(responseText, JsonOptions);
            }
            catch (Exception parseEx)
            {
                error = $"Parse error: {parseEx.Message}";
                return Array.Empty<SchemaChange>();
            }

            mapped = apiResponse?.Changes?
                .Select(MapChange)
                .Where(c => c != null)
                .Cast<SchemaChange>()
                .ToArray() ?? Array.Empty<SchemaChange>();

            if (mapped.Count == 0)
            {
                error = "Response contained no valid changes";
            }

            return mapped;
        }
        catch (Exception ex)
        {
            error = ex.ToString();
            return Array.Empty<SchemaChange>();
        }
        finally
        {
            await AiCallLogger.LogAsync(new AiCallLogEntry
            {
                Backend = "ExternalApi",
                Specification = specification,
                TableCount = currentSchema?.Tables.Count ?? 0,
                ViewCount = currentSchema?.Views.Count ?? 0,
                RequestBody = requestJson,
                ResponseBody = responseJson,
                Changes = mapped,
                Error = error
            });
        }
    }

    private static bool IsValidSettings(ApiSettings settings, [NotNullWhen(true)] out Uri? uri)
    {
        uri = null;
        if (string.IsNullOrWhiteSpace(settings.BaseUrl) || string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            return false;
        }

        if (!Uri.TryCreate(settings.BaseUrl, UriKind.Absolute, out var parsed))
        {
            return false;
        }

        uri = parsed;
        return true;
    }

    private static GeminiRequest BuildRequest(string specification, DatabaseSchema? currentSchema)
    {
        var promptBuilder = new StringBuilder();
        promptBuilder.AppendLine("You are an assistant that proposes database schema changes.");
        promptBuilder.AppendLine("Use the user's specification and the current schema to suggest changes.");
        promptBuilder.AppendLine("Return ONLY JSON with the structure { \"changes\": [ { \"type\": \"...\", \"description\": \"...\", \"tableName\": \"...\", \"viewName\": \"...\", \"sqlDefinition\": \"...\", \"columns\": [ ... ], \"column\": { ... } } ] } and no extra text.");
        promptBuilder.AppendLine("Allowed values for \"type\": \"CreateTable\", \"AddColumn\", \"CreateView\" (or lowercase without spaces).");
        promptBuilder.AppendLine("Specification:");
        promptBuilder.AppendLine(specification);
        promptBuilder.AppendLine("Current schema JSON:");
        promptBuilder.AppendLine(JsonSerializer.Serialize(ApiSchemaDto.From(currentSchema), JsonOptions));

        var prompt = promptBuilder.ToString();

        return new GeminiRequest
        {
            Contents = new List<GeminiContent>
            {
                new()
                {
                    Parts = new List<GeminiPart>
                    {
                        new() { Text = prompt }
                    }
                }
            },
            GenerationConfig = new GeminiGenerationConfig
            {
                ResponseMimeType = "application/json"
            }
        };
    }

    private static SchemaChange? MapChange(ApiChangeDto dto)
    {
        if (dto?.Type == null)
        {
            return null;
        }

        var normalizedType = dto.Type
            .Replace("_", string.Empty)
            .Replace("-", string.Empty)
            .Trim()
            .ToLowerInvariant();

        switch (normalizedType)
        {
            case "createtable":
                var ct = new CreateTableChange
                {
                    TableName = dto.TableName ?? string.Empty,
                    Description = dto.Description ?? string.Empty
                };
                if (dto.Columns != null)
                {
                    foreach (var col in dto.Columns)
                    {
                        ct.Columns.Add(MapColumn(col));
                    }
                }
                return ct;

            case "addcolumn":
                return new AddColumnChange
                {
                    TableName = dto.TableName ?? string.Empty,
                    Description = dto.Description ?? string.Empty,
                    Column = MapColumn(dto.Column)
                };

            case "createview":
                return new CreateViewChange
                {
                    ViewName = dto.ViewName ?? dto.TableName ?? string.Empty,
                    SqlDefinition = SqlFormatter.Format(dto.SqlDefinition ?? string.Empty),
                    Description = dto.Description ?? string.Empty
                };

            default:
                return null;
        }
    }

    private static ColumnDefinition MapColumn(ApiColumnDto? dto) =>
        new()
        {
            Name = dto?.Name ?? string.Empty,
            DataType = dto?.DataType ?? string.Empty,
            IsNullable = dto?.IsNullable ?? false,
            IsPrimaryKey = dto?.IsPrimaryKey ?? false
        };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private class GeminiRequest
    {
        public List<GeminiContent> Contents { get; set; } = new();
        public GeminiGenerationConfig GenerationConfig { get; set; } = new();
    }

    private class GeminiGenerationConfig
    {
        [JsonPropertyName("response_mime_type")]
        public string ResponseMimeType { get; set; } = "application/json";
    }

    private class GeminiContent
    {
        public List<GeminiPart> Parts { get; set; } = new();
    }

    private class GeminiPart
    {
        public string? Text { get; set; }
    }

    private class GeminiResponse
    {
        public List<GeminiCandidate>? Candidates { get; set; }
    }

    private class GeminiCandidate
    {
        public GeminiContent? Content { get; set; }
    }

    private class ApiResponse
    {
        public List<ApiChangeDto>? Changes { get; set; }
    }

    private class ApiChangeDto
    {
        public string? Type { get; set; }
        public string? Description { get; set; }
        public string? TableName { get; set; }
        public string? ViewName { get; set; }
        public string? SqlDefinition { get; set; }
        public List<ApiColumnDto>? Columns { get; set; }
        public ApiColumnDto? Column { get; set; }
    }

    private class ApiSchemaDto
    {
        public List<ApiTableDto>? Tables { get; set; }
        public List<ApiViewDto>? Views { get; set; }

        public static ApiSchemaDto? From(DatabaseSchema? schema)
        {
            if (schema == null) return null;

            return new ApiSchemaDto
            {
                Tables = schema.Tables.Select(t => new ApiTableDto
                {
                    Name = t.Name,
                    Columns = t.Columns.Select(c => new ApiColumnDto
                    {
                        Name = c.Name,
                        DataType = c.DataType,
                        IsNullable = c.IsNullable,
                        IsPrimaryKey = c.IsPrimaryKey
                    }).ToList()
                }).ToList(),
                Views = schema.Views.Select(v => new ApiViewDto
                {
                    Name = v.Name,
                    Definition = v.Definition
                }).ToList()
            };
        }
    }

    private class ApiTableDto
    {
        public string? Name { get; set; }
        public List<ApiColumnDto>? Columns { get; set; }
    }

    private class ApiViewDto
    {
        public string? Name { get; set; }
        public string? Definition { get; set; }
    }

    private class ApiColumnDto
    {
        public string? Name { get; set; }
        public string? DataType { get; set; }
        public bool? IsNullable { get; set; }
        public bool? IsPrimaryKey { get; set; }
    }
}

public class ApiSettings
{
    public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent";
    public string ApiKey { get; set; } = string.Empty;
}
