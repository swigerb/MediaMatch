namespace MediaMatch.Core.Services;

/// <summary>
/// Computes and verifies file checksums (CRC32, MD5, SHA1, SHA256, SHA384/512).
/// </summary>
public interface IChecksumService
{
    /// <summary>
    /// Computes the checksum of a file using the specified algorithm.
    /// Reports progress (0.0–1.0) during computation.
    /// </summary>
    /// <param name="filePath">The path to the file.</param>
    /// <param name="algorithm">The checksum algorithm to use.</param>
    /// <param name="progress">An optional progress reporter receiving values from 0.0 to 1.0.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The computed checksum as a hexadecimal string.</returns>
    Task<string> ComputeAsync(
        string filePath,
        ChecksumAlgorithm algorithm,
        IProgress<double>? progress = null,
        CancellationToken ct = default);

    /// <summary>
    /// Verifies a file's checksum against an expected value.
    /// </summary>
    /// <param name="filePath">The path to the file.</param>
    /// <param name="expectedHash">The expected checksum value.</param>
    /// <param name="algorithm">The checksum algorithm to use.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A value indicating whether the computed checksum matches the expected value.</returns>
    Task<bool> VerifyAsync(
        string filePath,
        string expectedHash,
        ChecksumAlgorithm algorithm,
        CancellationToken ct = default);
}

/// <summary>
/// Supported checksum algorithms for SFV verification.
/// </summary>
public enum ChecksumAlgorithm
{
    Crc32,
    Md5,
    Sha1,
    Sha256,
    Sha512
}
