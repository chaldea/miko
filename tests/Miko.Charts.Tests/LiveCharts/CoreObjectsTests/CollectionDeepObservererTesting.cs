// Derived from LiveCharts2 (MIT, Copyright (c) 2021 Alberto Rodriguez Orozco & LiveCharts Contributors).
// Ported from LiveCharts2 tests/CoreTests to Miko (xUnit, Miko render layer).

namespace Miko.Charts.Tests.LiveCharts.CoreObjectsTests;

using System.Collections.ObjectModel;
using Miko.Charts.Tests.LiveCharts.MockedObjects;
using Shouldly;

[Collection(ChartsCollection.Name)]
public class CollectionDeepObservererTesting
{
    public CollectionDeepObservererTesting() => CoreTestsSetup.Ensure();

    [Fact]
    public void ChangesCountTest()
    {
        var propertyChanges = 0;
        var collectionChanges = 0;

        // CASE 0, DUMMY CASE
        var dummyObserver = new TestObserver<int>();
        var dummyList = new List<int>();
        dummyObserver.MyCollection = dummyList;
        dummyList.Add(10);

        (dummyObserver.ChangesCount == 0).ShouldBeTrue();

        // CASE 1, ANOTHER DUMMY CASE
        var observer = new TestObserver<PropertyChangedObject>();

        var caseOneCollection = new List<PropertyChangedObject>();
        observer.MyCollection = caseOneCollection;

        var o1 = new PropertyChangedObject();

        caseOneCollection.Add(o1);
        o1.Value = 10;

        // it ignores both
        // because the collection does not implements INCC
        // the add occurs after we assigned the instance to propertyChangedObserver.MyCollection
        // sadly we can not detect this without consuming unnecessary resources.
        (observer.ChangesCount == 0).ShouldBeTrue();

        // CASE 2, MUST DETECT BOTH: INCC, IPC
        var caseTwoCollection = new ObservableCollection<PropertyChangedObject>();
        observer.MyCollection = caseTwoCollection;

        var o2 = new PropertyChangedObject();

        caseTwoCollection.Add(o2);
        collectionChanges++;

        o2.Value = 10;
        propertyChanges++;

        // o1 change must be ignored
        o1.Value = 30;

        (observer.ChangesCount == collectionChanges + propertyChanges).ShouldBeTrue();

        // CASE 3, INSTANCE CHANGED.
        var caseThreeCollection = new ObservableCollection<PropertyChangedObject>();
        observer.MyCollection = caseThreeCollection;

        var o3 = new PropertyChangedObject();
        caseThreeCollection.Add(o3);
        collectionChanges++;

        var o4 = new PropertyChangedObject();
        caseThreeCollection.Add(o4);
        collectionChanges++;

        o3.Value = 5;
        propertyChanges++;
        o3.Value = 8;
        propertyChanges++;
        o4.Value = 3;
        propertyChanges++;

        // should ignore the next changes
        caseTwoCollection.Add(new PropertyChangedObject());
        o1.Value = 100;
        o2.Value = 100;

        (observer.ChangesCount == collectionChanges + propertyChanges).ShouldBeTrue();

        // CASE 4, IT MUST STOP LISTENING WHEN WE REMOVE THE OBJECT FROM THE COLLECTION
        _ = caseThreeCollection.Remove(o4);
        collectionChanges++;
        o4.Value = 60;
        o4.Value = 20;
        o4.Value = 40;

        (observer.ChangesCount == collectionChanges + propertyChanges).ShouldBeTrue();

        // CASE 5, BUT IT MUST BE LISTENING FOR THE OBJECTS THAT ARE INCLUDED IN THE COLLECTION
        o3.Value = 30;
        propertyChanges++;
        o3.Value = 40;
        propertyChanges++;
        o3.Value = 50;
        propertyChanges++;

        (observer.ChangesCount == collectionChanges + propertyChanges).ShouldBeTrue();

        // CASE 5, REPLACEMENT
        var o5 = new PropertyChangedObject();
        caseThreeCollection[0] = o5;
        collectionChanges++;

        // it must stop listening for changes in o3 (index 0)
        o3.Value = 1;
        o3.Value = 2;
        o3.Value = 3;
        o3.Value = 4;

        // and now it should listen for changes in o5;
        o5.Value = 1;
        propertyChanges++;
        o5.Value = 2;
        propertyChanges++;

        (observer.ChangesCount == collectionChanges + propertyChanges).ShouldBeTrue();

        // CASE 6, COLLECTION CLEARED.
        caseThreeCollection.Clear();
        collectionChanges++;

        // must not count the following changes..
        o5.Value = 2;
        o5.Value = 2;
        o5.Value = 2;

        (observer.ChangesCount == collectionChanges + propertyChanges).ShouldBeTrue();
    }
}
