// Derived from LiveCharts2 (MIT, Copyright (c) 2021 Alberto Rodriguez Orozco & LiveCharts Contributors).
// Ported from LiveCharts2 tests/CoreTests to Miko (xUnit, Miko render layer).

namespace Miko.Charts.Tests.LiveCharts.MockedObjects;

using LiveChartsCore.Kernel.Observers;

public class TestObserver<T> : IDisposable
{
    private readonly CollectionDeepObserver _observerer;

    public TestObserver()
    {
        _observerer = new CollectionDeepObserver(() => ChangesCount++);
    }

    public IEnumerable<T>? MyCollection
    {
        get;
        set
        {
            _observerer.Dispose();
            _observerer.Initialize(value);
            field = value;
        }
    }

    public int ChangesCount { get; private set; }

    public void Dispose() => _observerer.Dispose();
}
