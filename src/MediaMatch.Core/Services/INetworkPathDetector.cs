namespace MediaMatch.Core.Services;

/// <summary>
/// Detects whether a path is on a network share (UNC or mapped drive).
/// </summary>
public interface INetworkPathDetector
{
    /// <summary>Returns true if the path is a UNC path or a mapped network drive.</summary>
    /// <param name="path">The file system path to check.</param>
    /// <returns>A value indicating whether the path is on a network share.</returns>
    bool IsNetworkPath(string path);
}
