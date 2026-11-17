# Routes and transit stops in Grasshopper 2

Open `swiftlet-google-maps-api.ghz` in Rhino 9 / GH2 with Swiftlet 0.4.0.
The GH1 original is unchanged. This GH2 rebuild uses native JSON, point and
polyline components; it does not need the old Python script or another plug-in.

## Run locally, without accounts

From this directory:

```sh
python maps_fixture.py
```

Enable **Route request** and **Stop request** in GH2. Both are saved disabled,
so opening the graph sends no requests. Expect two status values of 200, a blue
polyline with five vertices, three orange stop points, three names beginning
`Fixture stop`, distance 1200 and duration `240s`. These are synthetic fixtures,
not actual directions or transit data. The server always returns the same
geometry; changing request options does not recompute a route locally.

Coordinates are X = longitude, Y = latitude and Z = 0, in degrees. Zoom to the
route/stop outputs near (-73.985, 40.754) to see them. This schematic has no map
projection or borough meshes; its geometry cannot be measured in metres. Use the
returned `distanceMeters` field for route distance. Disable both requests when
finished and stop the fixture with Ctrl+C.

## Inputs and response paths

**Route options** holds editable JSON for origin, destination and travel mode.
The default mode is `DRIVE`. The request asks for GeoJSON LineString geometry
and a single route. **Response field mask** asks only for duration, distance and
that geometry. The graph selects the first route and reads:

`routes[0].polyline.geoJsonLinestring.coordinates -> [longitude, latitude] -> points -> polyline`

**Stop search parameters** contains matching lists of keys `lat`, `lon`, `radius`
and values `40.754`, `-73.985`, `1000`. The radius is in metres. The query parameters
go through the Params input, not in the URL. Stop geometry comes from
`stops[].geometry.coordinates`, with names from `stops[].stop_name`.

## Verification

**Outputs verified against local fixtures; not verified against the live services.**
No Google or Transitland request was made, and no credentials were configured.
The saved graph reopens with 48 objects, 46 wires and no GH1-hosted components.
The test checks one route POST and one stops GET, both statuses, every route
vertex in order, all three stop coordinates and names, distance and duration.
Both branches solve without faults/errors.

From the repository root, with an exclusively owned Rhino MCP endpoint:

```sh
python tests/Swiftlet.Cases/checks/native_maps_example.py --port 10501
```

The check starts/stops its own fixture and writes evidence and a canvas render
under `artifacts/native-examples`.

## Live services, for a later verified run

The [Google Routes computeRoutes reference](https://developers.google.com/maps/documentation/routes/reference/rest/v2/TopLevel/computeRoutes)
documents POST `https://routes.googleapis.com/directions/v2:computeRoutes`,
`GEO_JSON_LINESTRING` encoding and the response fields used here. A live request
needs access to Routes API and an appropriate `X-Goog-Api-Key` header. Keep the
existing `X-Goog-FieldMask` header. Editing the mode alone may require other
options for the desired route; this example has not verified those combinations.

The [Transitland stops API](https://www.transit.land/documentation/rest-api/stops)
uses GET `https://transit.land/api/v2/rest/stops`. Keep the lat/lon/radius Params
and add an `apikey` header as described in the
[access documentation](https://www.transit.land/documentation/rest-api).
Only the first page of stops is handled here. Live results may be empty, have
missing names/geometry, or require pagination; those conditions were not covered
by the local fixture. Provider quotas, charges, attribution requirements and
account permissions need checking before publishing results.

For a live run, replace each local URL and add the appropriate provider header
before enabling the request. Do not send credentials to the fixture: it rejects
credential headers. The GH1 README's old pricing statements are not used as a
basis for this example.

## Differences from the GH1 example

This rebuild demonstrates route/stop HTTP responses and native geometry. It
replaces the borough mesh and Rhino-point selection workflow with editable
latitude/longitude JSON, and replaces the mode dropdown with the `travelMode`
field. It does not reproduce the original map styling, geographic projection,
route accuracy or live service behaviour. Those need a separate live review.
