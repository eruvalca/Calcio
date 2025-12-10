using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;

using Calcio.Shared.Services.BlobStorage;

namespace Calcio.Services.BlobStorage;

public partial class BlobStorageService(
    BlobServiceClient blobServiceClient,
    ILogger<BlobStorageService> logger) : IBlobStorageService
{
    public async Task<Uri> UploadAsync(
        string containerName,
        string blobName,
        Stream content,
        string contentType,
        CancellationToken cancellationToken)
    {
        var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        await containerClient.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: cancellationToken);

        var blobClient = containerClient.GetBlobClient(blobName);

        var options = new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
        };

        await blobClient.UploadAsync(content, options, cancellationToken);
        LogBlobUploaded(logger, blobName, containerName);

        return blobClient.Uri;
    }

    public async Task<bool> DeleteAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken)
    {
        var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(blobName);

        var response = await blobClient.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots, cancellationToken: cancellationToken);

        if (response.Value)
        {
            LogBlobDeleted(logger, blobName, containerName);
        }

        return response.Value;
    }

    public async Task<int> DeleteByPrefixAsync(
        string containerName,
        string prefix,
        CancellationToken cancellationToken)
    {
        var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        var deletedCount = 0;

        await foreach (var blobItem in containerClient.GetBlobsAsync(prefix: prefix, cancellationToken: cancellationToken))
        {
            var blobClient = containerClient.GetBlobClient(blobItem.Name);
            var deleted = await blobClient.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots, cancellationToken: cancellationToken);

            if (deleted.Value)
            {
                deletedCount++;
            }
        }

        if (deletedCount > 0)
        {
            LogBlobsDeletedByPrefix(logger, deletedCount, prefix, containerName);
        }

        return deletedCount;
    }

    public Uri GetSasUrl(
        string containerName,
        string blobName,
        TimeSpan expiresIn)
    {
        var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(blobName);

        if (!blobClient.CanGenerateSasUri)
        {
            throw new InvalidOperationException("BlobClient is not authorized to generate SAS URIs. Ensure the connection uses account credentials.");
        }

        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = containerName,
            BlobName = blobName,
            Resource = "b", // blob
            ExpiresOn = DateTimeOffset.UtcNow.Add(expiresIn)
        };

        sasBuilder.SetPermissions(BlobSasPermissions.Read);

        return blobClient.GenerateSasUri(sasBuilder);
    }

    public async Task<bool> ExistsAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken)
    {
        var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(blobName);

        var response = await blobClient.ExistsAsync(cancellationToken);
        return response.Value;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Uploaded blob {BlobName} to container {ContainerName}")]
    private static partial void LogBlobUploaded(ILogger logger, string blobName, string containerName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Deleted blob {BlobName} from container {ContainerName}")]
    private static partial void LogBlobDeleted(ILogger logger, string blobName, string containerName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Deleted {Count} blobs with prefix {Prefix} from container {ContainerName}")]
    private static partial void LogBlobsDeletedByPrefix(ILogger logger, int count, string prefix, string containerName);
}
