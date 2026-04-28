using System.IO.Hashing;
using System.Security.Cryptography;
using MediaMatch.Core.Services;

namespace MediaMatch.Application.Services;

/// <summary>
/// Computes file checksums using CRC32, MD5, SHA1, SHA256, or SHA512.
/// Reports progress for large file hashing.
/// </summary>
public sealed class ChecksumService : IChecksumService
{
    private const int BufferSize = 81920; // 80 KB chunks

    public async Task<string> ComputeAsync(
        string filePath,
        ChecksumAlgorithm algorithm,
        IProgress<double>? progress = null,
        CancellationToken ct = default)
    {
        var fileInfo = new FileInfo(filePath);
        if (!fileInfo.Exists)
            throw new FileNotFoundException("File not found.", filePath);

        var totalBytes = fileInfo.Length;
        long bytesRead = 0;

        await using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, useAsync: true);
        var buffer = new byte[BufferSize];

        if (algorithm == ChecksumAlgorithm.Crc32)
        {
            return await ComputeCrc32Async(stream, totalBytes, buffer, progress, ct);
        }

        using var hasher = CreateHashAlgorithm(algorithm);
        int read;
        while ((read = await stream.ReadAsync(buffer, ct)) > 0)
        {
            hasher.TransformBlock(buffer, 0, read, null, 0);
            bytesRead += read;
            progress?.Report(totalBytes > 0 ? (double)bytesRead / totalBytes : 0);
        }

        hasher.TransformFinalBlock([], 0, 0);
        return Convert.ToHexStringLower(hasher.Hash!);
    }

    public async Task<bool> VerifyAsync(
        string filePath,
        string expectedHash,
        ChecksumAlgorithm algorithm,
        CancellationToken ct = default)
    {
        var computed = await ComputeAsync(filePath, algorithm, ct: ct);
        return string.Equals(computed, expectedHash, StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<string> ComputeCrc32Async(
        Stream stream,
        long totalBytes,
        byte[] buffer,
        IProgress<double>? progress,
        CancellationToken ct)
    {
        var crc = new Crc32();
        long bytesRead = 0;
        int read;

        while ((read = await stream.ReadAsync(buffer, ct)) > 0)
        {
            crc.Append(buffer.AsSpan(0, read));
            bytesRead += read;
            progress?.Report(totalBytes > 0 ? (double)bytesRead / totalBytes : 0);
        }

        var hash = new byte[4];
        crc.GetCurrentHash(hash);
        return Convert.ToHexStringLower(hash);
    }

    private static HashAlgorithm CreateHashAlgorithm(ChecksumAlgorithm algorithm) => algorithm switch
    {
        ChecksumAlgorithm.Md5 => MD5.Create(),
        ChecksumAlgorithm.Sha1 => SHA1.Create(),
        ChecksumAlgorithm.Sha256 => SHA256.Create(),
        ChecksumAlgorithm.Sha512 => SHA512.Create(),
        _ => throw new ArgumentOutOfRangeException(nameof(algorithm))
    };
}
