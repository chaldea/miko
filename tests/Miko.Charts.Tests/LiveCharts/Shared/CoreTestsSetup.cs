namespace Miko.Charts.Tests.LiveCharts;

// The usings sit inside the namespace on purpose: from Miko.Charts.Tests.LiveCharts the simple names `LiveCharts`
// and `Animation` would otherwise bind to the namespaces Miko.Charts.Tests.LiveCharts and Miko.Animation before
// any file-level using is considered.
using LiveChartsCore;
using LiveChartsCore.Motion;
using LiveChartsCore.MikoView;
using Miko.Graphics;

/// <summary>
/// What LiveCharts' <c>TestsInitializer</c> did once per run with <c>[AssemblyInitialize]</c>: registers the defaults
/// and lifts the "measure only after the canvas drew a frame" guard. xUnit has no assembly initializer, so every
/// ported test class calls <see cref="Ensure"/> from its constructor.
/// </summary>
internal static class CoreTestsSetup
{
    private static readonly object s_lock = new();
    private static Miko.Core.MikoEngine? s_engine;

    /// <summary>The graphics factory the labels are laid out with and the test surfaces are created from.</summary>
    public static IGraphicsFactory Graphics
    {
        get
        {
            Ensure();
            return s_engine!.Graphics;
        }
    }

    /// <summary>Configures LiveCharts for the tests; cheap to call before every test.</summary>
    public static void Ensure()
    {
        lock (s_lock)
        {
            if (s_engine is null)
            {
                LiveCharts.Configure(config => config.UseDefaults());
                CoreMotionCanvas.IsTesting = true;
                s_engine = new Miko.Hosting.MikoEngineBuilder().Build();
            }

            // the chart components of the other tests in this assembly set their own engine's factory, set ours back.
            LiveChartsMiko.UseGraphics(s_engine.Graphics);
        }
    }
}
