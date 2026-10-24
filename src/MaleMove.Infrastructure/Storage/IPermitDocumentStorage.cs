namespace MaleMove.Infrastructure.Storage;

public sealed record StoredDocument(string StorageKey, string Sha256);

public interface IPermitDocumentStorage
{
    Task<StoredDocument> SaveAsync(Stream content, CancellationToken cancellationToken);
    Task DeleteAsync(string storageKey, CancellationToken cancellationToken);
    Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken);
}

