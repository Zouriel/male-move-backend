using System.Text.Json;
using Anthropic;
using Anthropic.Exceptions;
using Anthropic.Models.Messages;
using MaleMove.Application.Common;
using MaleMove.Application.Permits;
using MaleMove.Infrastructure.Persistence;
using MaleMove.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MaleMove.Infrastructure.Services;

/// <summary>
/// Reads an uploaded permit (PDF or photo) with Claude and returns its fields as suggestions.
/// Only documents that passed malware scanning are sent. Nothing is saved here: the officer reviews the
/// suggestions in the form and saves them through the normal update path.
/// </summary>
public sealed class PermitExtractionService(
    AppDbContext db,
    IPermitDocumentStorage storage,
    IConfiguration configuration,
    ILogger<PermitExtractionService> logger) : IPermitExtractionService
{
    private const string Instructions = """
        You read road works permits issued in Greater Malé, Maldives (Malé, Hulhulé, Hulhumalé, Villimalé).
        Extract the fields below from the attached permit. Use only what the document states; use null for
        anything it does not state. Never invent a permit number, date or street.
        - startDate/endDate: YYYY-MM-DD. startTime/endTime/dailyStart/dailyEnd: 24-hour HH:MM in Maldives time.
        - dailyStart/dailyEnd: only when the work is limited to recurring hours each day (for example nights only).
        - impactType: FullClosure (road closed to traffic), PartialRestriction (part of the road usable),
          LaneRestriction (one lane affected), or Unknown when the document does not say.
        - locationDescription: the street(s) and island as written, for example "Majeedhee Magu, Malé".
        - streetNames: each street or road named as affected, as written (for example "Majeedhee Magu").
        - workDescription: one short sentence describing the work.
        """;

    public async Task<PermitExtractionDto> ExtractAsync(Guid permitId, CancellationToken cancellationToken)
    {
        var apiKey = configuration["Anthropic:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new AppException("Document reading is not configured.", 503, "ocr_unavailable");

        var permit = await db.Permits.AsNoTracking().Include(x => x.PermitDocument)
            .SingleOrDefaultAsync(x => x.Id == permitId, cancellationToken)
            ?? throw new AppException("Permit was not found.", 404, "permit_not_found");
        var document = permit.PermitDocument
            ?? throw new AppException("This permit has no uploaded document to read.", 409, "permit_document_missing");
        if (document.ScanStatus != "Clean")
            throw new AppException("The document has not passed malware scanning yet.", 409, "document_scan_required");

        string data;
        await using (var stream = await storage.OpenReadAsync(document.StorageKey, cancellationToken))
        using (var buffer = new MemoryStream())
        {
            await stream.CopyToAsync(buffer, cancellationToken);
            data = Convert.ToBase64String(buffer.ToArray());
        }

        ContentBlockParam file = document.MediaType switch
        {
            "application/pdf" => new DocumentBlockParam { Source = new Base64PdfSource { Data = data } },
            "image/png" => new ImageBlockParam { Source = new Base64ImageSource { Data = data, MediaType = MediaType.ImagePng } },
            "image/jpeg" => new ImageBlockParam { Source = new Base64ImageSource { Data = data, MediaType = MediaType.ImageJpeg } },
            _ => throw new AppException("Only PDF, PNG and JPEG permits can be read.", 415, "ocr_unsupported_type"),
        };

        var parameters = new MessageCreateParams
        {
            Model = configuration["Anthropic:Model"] is { Length: > 0 } model ? model : "claude-opus-5-5",
            MaxTokens = 16000,
            Messages = [new() { Role = Role.User, Content = new List<ContentBlockParam> { file, new TextBlockParam { Text = Instructions } } }],
            OutputConfig = new OutputConfig { Effort = Effort.Low, Format = new JsonOutputFormat { Schema = Schema() } },
        };

        Message response;
        try
        {
            var client = new AnthropicClient { ApiKey = apiKey, Timeout = TimeSpan.FromSeconds(90), MaxRetries = 1 };
            response = await client.Messages.Create(parameters, cancellationToken: cancellationToken);
        }
        catch (AnthropicRateLimitException ex) { throw Unavailable("Document reading is busy right now. Try again in a minute.", ex); }
        catch (AnthropicApiException ex) { throw Unavailable("The document could not be read. Enter the details by hand.", ex); }
        catch (AnthropicIOException ex) { throw Unavailable("Document reading is unreachable right now. Try again in a minute.", ex); }

        if (response.StopReason == "refusal")
            throw new AppException("The document could not be read. Enter the details by hand.", 422, "ocr_refused");
        var json = response.Content.Select(b => b.Value).OfType<TextBlock>().Select(t => t.Text).FirstOrDefault();
        if (string.IsNullOrWhiteSpace(json))
            throw new AppException("The document could not be read. Enter the details by hand.", 502, "ocr_empty");

        var extracted = JsonSerializer.Deserialize<PermitExtractionDto>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web))
            ?? throw new AppException("The document could not be read. Enter the details by hand.", 502, "ocr_invalid");
        return extracted with { StreetNames = extracted.StreetNames ?? [] };
    }

    private AppException Unavailable(string message, Exception ex)
    {
        logger.LogWarning(ex, "Permit document reading failed.");
        return new AppException(message, 503, "ocr_unavailable");
    }

    private static Dictionary<string, JsonElement> Schema()
    {
        var nullableString = new { anyOf = new object[] { new { type = "string" }, new { type = "null" } } };
        var fields = new[] { "permitNumber", "locationDescription", "workDescription", "contractor", "impactType",
            "startDate", "startTime", "endDate", "endTime", "dailyStart", "dailyEnd" };
        var properties = fields.ToDictionary(f => f, f => (object)nullableString);
        properties["streetNames"] = new { type = "array", items = new { type = "string" } };
        return new Dictionary<string, JsonElement>
        {
            ["type"] = JsonSerializer.SerializeToElement("object"),
            ["properties"] = JsonSerializer.SerializeToElement(properties),
            ["required"] = JsonSerializer.SerializeToElement(fields.Append("streetNames").ToArray()),
            ["additionalProperties"] = JsonSerializer.SerializeToElement(false),
        };
    }
}
