using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;

using Calcio.Shared.Services.BlobStorage;

namespace Calcio.Services.BlobStorage;

/// <summary>
/// Provides Blob Storage Service operations.
/// </summary>
/// <param name="blobServiceClient">The blob Service Client.</param>
/// <param name="logger">The logger.</param>
public partial class BlobStorageService(
    BlobServiceClient blobServiceClient,
    ILogger<BlobStorageService> logger) : IBlobStorageService
{
    /// <summary>
    /// Executes the Upload Async operation.
    /// </summary>
    /// <param name="containerName">The container Name.</param>
    /// <param name="blobName">The blob Name.</param>
    /// <param name="content">The content.</param>
    /// <param name="contentType">The content Type.</param>
    /// <param name="cancellationToken">The cancellation Token.</param>
    /// <returns>The operation result.</returns>
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

    /// <summary>
    /// Executes the Delete Async operation.
    /// </summary>
    /// <param name="containerName">The container Name.</param>
    /// <param name="blobName">The blob Name.</param>
    /// <param name="cancellationToken">The cancellation Token.</param>
    /// <returns>The operation result.</returns>
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

    /// <summary>
    /// Executes the Delete By Prefix Async operation.
    /// </summary>
    /// <param name="containerName">The container Name.</param>
    /// <param name="prefix">The prefix.</param>
    /// <param name="cancellationToken">The cancellation Token.</param>
    /// <returns>The operation result.</returns>
    public async Task<int> DeleteByPrefixAsync(
        string containerName,
        string prefix,
        CancellationToken cancellationToken)
    {
        var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        var deletedCount = 0;

        await foreach (var blobItem in containerClient.GetBlobsAsync(traits: BlobTraits.None, states: BlobStates.All, prefix: prefix, cancellationToken: cancellationToken))
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

    /// <summary>
    /// Executes the Get Sas Url operation.
    /// </summary>
    /// <param name="containerName">The container Name.</param>
    /// <param name="blobName">The blob Name.</param>
    /// <param name="expiresIn">The expires In.</param>
    /// <returns>The operation result.</returns>
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

    /// <summary>
    /// Executes the Exists Async operation.
    /// </summary>
    /// <param name="containerName">The container Name.</param>
    /// <param name="blobName">The blob Name.</param>
    /// <param name="cancellationToken">The cancellation Token.</param>
    /// <returns>The operation result.</returns>
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

    /// <summary>
    /// Executes the Log Blob Uploaded operation.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="blobName">The blob Name.</param>
    /// <param name="containerName">The container Name.</param>
    [LoggerMessage(Level = LogLevel.Information, Message = "Uploaded blob {BlobName} to container {ContainerName}")]
    /// <summary>
    /// Executes the log blob uploaded operation.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="blobName">The blob name.</param>
    /// <param name="containerName">The container name.</param>
    private static partial void LogBlobUploaded(ILogger logger, string blobName, string containerName);

    /// <summary>
    /// Executes the Log Blob Deleted operation.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="blobName">The blob Name.</param>
    /// <param name="containerName">The container Name.</param>
    [LoggerMessage(Level = LogLevel.Information, Message = "Deleted blob {BlobName} from container {ContainerName}")]
    /// <summary>
    /// Executes the log blob deleted operation.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="blobName">The blob name.</param>
    /// <param name="containerName">The container name.</param>
    private static partial void LogBlobDeleted(ILogger logger, string blobName, string containerName);

    /// <summary>
    /// Executes the Log Blobs Deleted By Prefix operation.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="count">The count.</param>
    /// <param name="prefix">The prefix.</param>
    /// <param name="containerName">The container Name.</param>
    [LoggerMessage(Level = LogLevel.Information, Message = "Deleted {Count} blobs with prefix {Prefix} from container {ContainerName}")]
    /// <summary>
    /// Executes the log blobs deleted by prefix operation.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="count">The count.</param>
    /// <param name="prefix">The prefix.</param>
    /// <param name="containerName">The container name.</param>
    private static partial void LogBlobsDeletedByPrefix(ILogger logger, int count, string prefix, string containerName);
}
