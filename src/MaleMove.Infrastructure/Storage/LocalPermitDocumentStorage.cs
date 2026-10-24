using System.Security.Cryptography;

namespace MaleMove.Infrastructure.Storage;

public sealed class LocalPermitDocumentStorage(IConfiguration configuration) : IPermitDocumentStorage
{
    private readonly string _root = Path.GetFullPath(configuration["PermitStorage:Path"]
        ?? throw new InvalidOperationException("PermitStorage:Path must be configured."));

    public async Task<StoredDocument> SaveAsync(Stream content, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(_root);
        var key = $"{Guid.NewGuid():N}.upload";
        var path = ResolvePath(key);
        await using var destination = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var buffer = new byte[81920];
        int read;
        while ((read = await content.ReadAsync(buffer, cancellationToken)) > 0)
        {
            hash.AppendData(buffer, 0, read);
            await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }

        await destination.FlushAsync(cancellationToken);
        return new StoredDocument(key, Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant());
    }

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = ResolvePath(storageKey);
        if (File.Exists(path)) File.Delete(path);
        return Task.CompletedTask;
    }

    private string ResolvePath(string key)
    {
        if (Path.GetFileName(key) != key || key.Contains("..", StringComparison.Ordinal))
            throw new InvalidOperationException("Invalid storage key.");
        var path = Path.GetFullPath(Path.Combine(_root, key));
        if (!path.StartsWith(_root + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            throw new InvalidOperationException("Storage path escaped its configured root.");
        return path;
    }
}

