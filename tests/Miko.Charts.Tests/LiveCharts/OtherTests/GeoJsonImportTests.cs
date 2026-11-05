// Derived from LiveCharts2 (MIT, Copyright (c) 2021 Alberto Rodriguez Orozco & LiveCharts Contributors).
// Ported from LiveCharts2 tests/CoreTests to Miko (xUnit, Miko render layer).

namespace Miko.Charts.Tests.LiveCharts.OtherTests;

using LiveChartsCore.Geo;
using Shouldly;

[Collection(ChartsCollection.Name)]
public class GeoJsonImportTests
{
    public GeoJsonImportTests() => CoreTestsSetup.Ensure();

    // Standard GeoJson allowed by RFC 7946: mixed Polygon / MultiPolygon geometries,
    // numeric and string property values, and no LiveCharts-specific keys
    // (no "shortName", no "setOf"). Regression for issue #1426.
    private const string CustomFeatureCollection = """
        {
          "type": "FeatureCollection",
          "features": [
            {
              "type": "Feature",
              "properties": { "name": "Piemonte", "code_num": 1 },
              "geometry": {
                "type": "Polygon",
                "coordinates": [[[0,0],[1,0],[1,1],[0,1],[0,0]]]
              }
            },
            {
              "type": "Feature",
              "properties": { "name": "Sicilia", "code_num": 19 },
              "geometry": {
                "type": "MultiPolygon",
                "coordinates": [[[[10,10],[11,10],[11,11],[10,11],[10,10]]]]
              }
            },
            {
              "type": "Feature",
              "properties": { "label": "no-name-key" },
              "geometry": {
                "type": "Polygon",
                "coordinates": [[[5,5],[6,5],[6,6],[5,5]]]
              }
            },
            {
              "type": "Feature",
              "properties": { "name": "Capital" },
              "geometry": { "type": "Point", "coordinates": [0,0] }
            },
            {
              "type": "Feature",
              "properties": { "name": "Highway" },
              "geometry": {
                "type": "MultiLineString",
                "coordinates": [[[20,20],[21,21]],[[22,22],[23,23]]]
              }
            }
          ]
        }
        """;

    [Fact]
    public void Import_CustomGeoJson_ParsesMixedPolygonShapes()
    {
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(CustomFeatureCollection));
        using var sr = new StreamReader(stream);

        var map = Maps.GetMapFromStreamReader(sr);

        map.Layers.TryGetValue("default", out var layer).ShouldBeTrue("default layer should exist");
        // Polygon + MultiPolygon land, plus the missing-name fallback. Point and
        // MultiLineString are non-area types and must be skipped (MultiLineString
        // shares 3-deep coordinate shape with Polygon, so dispatching on type matters).
        layer!.Lands.Count.ShouldBe(3, "polygon-shaped features should be loaded; non-area types skipped");

        map.FindLand("piemonte").ShouldNotBeNull("Polygon feature should be found by lower-cased name");
        map.FindLand("sicilia").ShouldNotBeNull("MultiPolygon feature should be found by lower-cased name");
        // The unnamed feature falls back to a synthetic feature_<index> identifier.
        layer.Lands.Keys.Any(k => k.StartsWith("feature_")).ShouldBeTrue("missing-name feature should fall back to feature_<i>");
    }

    [Fact]
    public void Import_BuiltInWorldMap_StillLoads()
    {
        // Sanity check that the LiveCharts-shipped world.geojson keeps loading after
        // the parser changes (its features carry "shortName" + "setOf" properties).
        var world = Maps.GetWorldMap();
        (world.Layers["default"].Lands.Count > 100).ShouldBeTrue("world map lands should still load");
        world.FindLand("bra").ShouldNotBeNull("Brazil should be found by shortName");
    }

    // RFC 7946 doesn't constrain property casing, and arbitrary GeoJson can produce
    // duplicate names that resolve to the same shortName. Both must import cleanly.
    private const string MixedCaseAndDuplicateNamesCollection = """
        {
          "type": "FeatureCollection",
          "features": [
            {
              "type": "Feature",
              "properties": { "Name": "Alpha", "SHORTNAME": "AL" },
              "geometry": {
                "type": "Polygon",
                "coordinates": [[[0,0],[1,0],[1,1],[0,1],[0,0]]]
              }
            },
            {
              "type": "Feature",
              "properties": { "name": "Duplicate" },
              "geometry": {
                "type": "Polygon",
                "coordinates": [[[2,2],[3,2],[3,3],[2,3],[2,2]]]
              }
            },
            {
              "type": "Feature",
              "properties": { "name": "Duplicate" },
              "geometry": {
                "type": "Polygon",
                "coordinates": [[[4,4],[5,4],[5,5],[4,5],[4,4]]]
              }
            }
          ]
        }
        """;

    [Fact]
    public void Import_MixedCasePropertyKeys_ResolveCaseInsensitively()
    {
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(MixedCaseAndDuplicateNamesCollection));
        using var sr = new StreamReader(stream);

        var map = Maps.GetMapFromStreamReader(sr);

        // "Name"/"SHORTNAME" must resolve through case-insensitive lookup; if the
        // resolver reverted to exact-match it would fall back to feature_0 / the name.
        map.FindLand("al").ShouldNotBeNull("SHORTNAME should resolve via case-insensitive lookup");
    }

    [Fact]
    public void Import_DuplicateShortNames_DoNotAbortImport()
    {
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(MixedCaseAndDuplicateNamesCollection));
        using var sr = new StreamReader(stream);

        var map = Maps.GetMapFromStreamReader(sr);

        map.Layers.TryGetValue("default", out var layer).ShouldBeTrue("default layer should exist");
        // All three features must land. Without collision handling, the second
        // "Duplicate" feature would throw ArgumentException from Lands.Add and
        // GetMapFromStreamReader would never return.
        layer!.Lands.Count.ShouldBe(3, "duplicate shortName features should be suffixed, not dropped or thrown");
        map.FindLand("duplicate").ShouldNotBeNull("first duplicate should keep the bare key");
    }
}
