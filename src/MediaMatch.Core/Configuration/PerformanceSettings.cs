namespace MediaMatch.Core.Configuration;

/// <summary>
/// Performance tuning settings for file scanning and metadata resolution.
/// </summary>
public sealed class PerformanceSettings
{
    /// <summary>Maximum parallel threads for file scanning. Defaults to processor count.</summary>
    public int MaxScanThreads { get; set; } = Environment.ProcessorCount;

    /// <summary>Concurrent I/O threads when scanning network paths (UNC/mapped drives).</summary>
    public int NetworkConcurrency { get; set; } = 2;

    /// <summary>Maximum recursive directory depth for file scanning.</summary>
    public int MaxDirectoryDepth { get; set; } = 20;

    /// <summary>When true, metadata is only fetched on-demand (preview click or batch start).</summary>
    public bool EnableLazyMetadata { get; set; } = true;
}
