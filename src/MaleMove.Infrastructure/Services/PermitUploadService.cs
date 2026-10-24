using MaleMove.Application.Common;
using MaleMove.Application.Permits;
using MaleMove.Domain.Entities;
using MaleMove.Infrastructure.Persistence;
using MaleMove.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;

namespace MaleMove.Infrastructure.Services;

public sealed class PermitUploadService(AppDbContext db, IPermitDocumentStorage storage, IFileMalwareScanner scanner) : IPermitUploadService
{
    private const long MaximumBytes = 25 * 1024 * 1024;

    public async Task<PermitSummaryDto> UploadAsync(PermitUploadRequest request, CancellationToken cancellationToken)
    {
        if (request.Length is <= 0 or > MaximumBytes)
            throw new AppException("Permit files must be between 1 byte and 25 MiB.", 413, "upload_size_invalid");

        var extension = Path.GetExtension(request.FileName).ToLowerInvariant();
        var mediaType = extension switch
        {
            ".pdf" when request.ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase) => "application/pdf",
            ".jpg" or ".jpeg" when request.ContentType.Equals("image/jpeg", StringComparison.OrdinalIgnoreCase) => "image/jpeg",
            ".png" when request.ContentType.Equals("image/png", StringComparison.OrdinalIgnoreCase) => "image/png",
            _ => throw new AppException("Only PDF, JPEG, and PNG permit files are accepted.", 422, "unsupported_file_type")
        };

        var prefix = new byte[8];
        var prefixLength = await request.Content.ReadAsync(prefix, cancellationToken);
        request.Content.Position = 0;
        var signatureValid = mediaType switch
        {
            "application/pdf" => prefixLength >= 5 && prefix.AsSpan(0, 5).SequenceEqual("%PDF-"u8),
            "image/jpeg" => prefixLength >= 3 && prefix[0] == 0xFF && prefix[1] == 0xD8 && prefix[2] == 0xFF,
            "image/png" => prefixLength >= 8 && prefix.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }),
            _ => false
        };
        if (!signatureValid) throw new AppException("The file content does not match its declared type.", 422, "file_signature_invalid");

        var scan = await scanner.ScanAsync(request.Content, cancellationToken);
        request.Content.Position = 0;
        if (scan == MalwareScanResult.Malicious)
            throw new AppException("The uploaded file failed malware scanning and was discarded.", 422, "malware_detected");
        if (scan != MalwareScanResult.Clean)
            throw new AppException("Malware scanning is unavailable. The file was not accepted.", 503, "malware_scanner_unavailable");

        var stored = await storage.SaveAsync(request.Content, cancellationToken);
        try
        {
            var document = new PermitDocument
            {
                StorageKey = stored.StorageKey,
                OriginalFileName = Path.GetFileName(request.FileName)[..Math.Min(Path.GetFileName(request.FileName).Length, 255)],
                MediaType = mediaType,
                SizeBytes = request.Length,
                Sha256 = stored.Sha256,
                UploadedBy = request.UploadedBy,
                OcrStatus = "NotConfigured",
                ScanStatus = "Clean"
            };
            var permit = new Permit
            {
                PermitNumber = "UNASSIGNED",
                LocationDescription = "Needs review",
                WorkDescription = "OCR extraction is not configured; enter permit details during review.",
                ImpactType = PermitImpactType.Unknown,
                ApprovalState = PermitApprovalState.NeedsReview,
                PermitDocument = document
            };
            db.Permits.Add(permit);
            db.PermitAuditEvents.Add(new PermitAuditEvent
            {
                PermitId = permit.Id,
                ActorId = request.UploadedBy,
                Action = "permit.document_uploaded",
                NewState = permit.ApprovalState.ToString(),
                OccurredAt = DateTimeOffset.UtcNow
            });
            await db.SaveChangesAsync(cancellationToken);
            return new PermitSummaryDto(permit.Id, permit.PermitNumber, permit.LocationDescription, permit.WorkDescription,
                permit.ImpactType, permit.ApprovalState, permit.SiteVerificationState, permit.StartsAt, permit.EndsAt, permit.Revision, permit.UpdatedAt);
        }
        catch
        {
            await storage.DeleteAsync(stored.StorageKey, CancellationToken.None);
            throw;
        }
    }
}

