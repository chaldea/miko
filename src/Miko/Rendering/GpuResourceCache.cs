using SkiaSharp;

namespace Miko.Rendering;

/// <summary>
/// Applies the resource-cache policy used by Miko-owned GPU contexts.
/// </summary>
public static class GpuResourceCache
{
    /// <summary>Default maximum number of bytes retained by Skia's GPU resource cache.</summary>
    public const long DefaultLimitBytes = 64L * 1024 * 1024;

    /// <summary>Applies an explicit byte limit to a GPU resource cache.</summary>
    public static void Configure(GRContext context, long limitBytes = DefaultLimitBytes)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(limitBytes);
        context.SetResourceCacheLimit(limitBytes);
    }

    /// <summary>Returns the resources currently retained by the context cache.</summary>
    public static GpuResourceCacheUsage GetUsage(GRContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.GetResourceCacheUsage(out int resourceCount, out long resourceBytes);
        return new GpuResourceCacheUsage(resourceCount, resourceBytes);
    }

    /// <summary>Purges resources which are no longer referenced by a surface or other GPU object.</summary>
    public static void PurgeUnlockedResources(GRContext? context)
    {
        context?.PurgeUnlockedResources(scratchResourcesOnly: false);
    }

    /// <summary>Purges all cached GPU resources before a context is destroyed.</summary>
    public static void PurgeAllResources(GRContext? context)
    {
        context?.PurgeResources();
    }
}

/// <summary>A snapshot of Skia GPU resource-cache usage.</summary>
public readonly record struct GpuResourceCacheUsage(int ResourceCount, long ResourceBytes);
