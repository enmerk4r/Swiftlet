# Multipart form data in Grasshopper 2

Open `multipart-form-data.ghz` in Grasshopper 2 with Swiftlet 0.4.0 installed in Rhino 9. This is a new native example using the current multipart components. The older `.gh` file is kept for Grasshopper 1 and uses archived components.

1. In a terminal in this folder, run `python multipart_receiver.py` (Python 3, no extra packages).
2. Open the `.ghz` file. The POST Request component is saved disabled. Enable it after the receiver prints its listening address.
3. The status panel should show **200**. The response panel contains JSON describing both received fields. Double-click the panel to inspect its full contents if the line is clipped.
4. Disable POST Request when finished and press Ctrl+C in the receiver terminal.

| Field | Expected result |
|---|---|
| `title` | Text `Swiftlet origami`, 16 UTF-8 bytes, content type `text/plain` |
| `upload` | Filename `origami.txt`, text `Fold me.` followed by a newline, 9 UTF-8 bytes, content type `text/plain` |

The file contents are created on the canvas by Text To Byte Array; no file needs to be created on disk. Select a component to edit its persistent inputs. Text To Byte Array uses the encoding value `UTF8`.

The receiver listens only on `127.0.0.1:18741`. If that port is occupied, start it with `--port 18743` and change POST Request's URL to `http://127.0.0.1:18743/multipart`. Each recompute of the enabled POST Request sends another request.

**Verified:** saved and reopened in GH2; no GH1-hosted objects; actual HTTP request received with the exact names, filename, content types and bytes above; HTTP 200 and the same JSON returned to the canvas; no component errors.
