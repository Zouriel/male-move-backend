namespace MaleMove.Application.Permits;

public sealed record PermitUploadRequest(
    Stream Content,
    string FileName,
    string ContentType,
    long Length,
    Guid UploadedBy);

public interface IPermitUploadService
{
    Task<PermitSummaryDto> UploadAsync(PermitUploadRequest request, CancellationToken cancellationToken);
}

