# HTTP geometry server in Grasshopper 2

Open `server_demo.ghz` in Grasshopper 2 with Swiftlet 0.4.0 installed in Rhino 9. The original `server_demo.gh` remains available for Grasshopper 1.

1. Enable the component labelled **Start server**. It listens on port **18744**. It is saved disabled so opening the example does not start a server.
2. From a terminal in this folder, run `python server_request.py volume`, then `python server_request.py area` (Python 3, no extra packages).
3. Each command should return HTTP 200 and a JSON object containing a positive `volume` or `area`. The corresponding geometry branch displays its Voronoi cells in Rhino.
4. Disable **Start server** when finished. If port 18744 is occupied, change the port panel and pass the matching `--port` to the helper.

The two endpoints are `POST /voronoi-volume` and `POST /voronoi-area`. Both take a JSON body such as `{"num_points":8,"box_size":10}`. Try `python server_request.py volume --points 12 --size 15` to change the inputs. Use small positive sizes and point counts while exploring; this is a teaching example, not a public production endpoint with input limits.

The graph keeps the original request parsing, geometry calculation and response wiring. Four GH1-hosted geometry components were replaced with native GH2 components:

- **Populate Box** generates the points using its GH2 default sampling algorithm. The distribution is not the same as GH1's seeded Populate 3D.
- **Voronoi 3D** produces mesh polyhedra instead of GH1's Brep cells. The graph computes area and volume from those meshes.

Numeric results are therefore not expected to match the GH1 file. The two routes run independently: after an area request, the inactive volume branch may show no data, and vice versa.

**Verified:** the `.ghz` saves/reopens with its objects and wires intact and no GH1-hosted objects; both real local HTTP requests return 200 with geometry-derived JSON; an eight-point request produces eight native Voronoi mesh cells on the active route; no component errors; server disabled and listener released afterward.
