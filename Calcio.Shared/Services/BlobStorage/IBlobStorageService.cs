namespace Calcio.Shared.Services.BlobStorage;

/// <summary>
/// Service for managing blob storage operations.
/// </summary>
public interface IBlobStorageService
{
    /// <summary>
    /// Uploads a blob to storage.
    /// </summary>
    /// <param name="containerName">The name of the container.</param>
    /// <param name="blobName">The name of the blob.</param>
    /// <param name="content">The content stream to upload.</param>
    /// <param name="contentType">The MIME content type.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The URI of the uploaded blob.</returns>
    Task<Uri> UploadAsync(
        string containerName,
        string blobName,
        Stream content,
        string contentType,
        CancellationToken cancellationToken);

    /// <summary>
    /// Deletes a blob from storage.
    /// </summary>
    /// <param name="containerName">The name of the container.</param>
    /// <param name="blobName">The name of the blob.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the blob was deleted, false if it didn't exist.</returns>
    Task<bool> DeleteAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken);

    /// <summary>
    /// Deletes all blobs with a given prefix.
    /// </summary>
    /// <param name="containerName">The name of the container.</param>
    /// <param name="prefix">The prefix to match (e.g., "players/123/").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of blobs deleted.</returns>
    Task<int> DeleteByPrefixAsync(
        string containerName,
        string prefix,
        CancellationToken cancellationToken);

    /// <summary>
    /// Generates a SAS URL for reading a blob.
    /// </summary>
    /// <param name="containerName">The name of the container.</param>
    /// <param name="blobName">The name of the blob.</param>
    /// <param name="expiresIn">How long the URL should be valid.</param>
    /// <returns>A time-limited URL for reading the blob.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the blob client is not authorized to generate SAS URIs.</exception>
    Uri GetSasUrl(
        string containerName,
        string blobName,
        TimeSpan expiresIn);

    /// <summary>
    /// Checks if a blob exists.
    /// </summary>
    /// <param name="containerName">The name of the container.</param>
    /// <param name="blobName">The name of the blob.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the blob exists.</returns>
    Task<bool> ExistsAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken);
}
