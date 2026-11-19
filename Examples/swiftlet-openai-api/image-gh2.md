# Image responses in Grasshopper 2

`swiftlet-dalle-example.ghz` replaces the old GH1 DALL-E example with a native
GH2 JSON/base64/bitmap pipeline. Its default mode is entirely local and needs
Python, Rhino 9 and Swiftlet 0.4.0. The GH1 original is unchanged.

## Run the offline example

In this directory run:

```sh
python image_fixture.py
```

Open the `.ghz` file and enable **Image request**. The URL is
`http://127.0.0.1:18745/v1/images/generations`. Expect status 200, Width = 2 and
Height = 2. The Rhino viewport shows a four-colour mesh: red/green on the top row,
blue/white on the bottom. Use Zoom Extents to find it. This is a deterministic
test pattern, not an AI-generated image. The fixture ignores the requested image
size and returns 2x2 pixels deliberately. Disable the request afterwards; stop
the fixture with Ctrl+C. The saved request is disabled, so opening the graph
alone does not send anything.

The graph follows `data[] -> b64_json -> Base64 To Byte Array -> Byte Array To
Bitmap -> Bitmap to Mesh`. It uses Swiftlet and native GH2 components only;
there is no Python component or image-import plug-in.

## Verification and limits

**Output verified against a local fixture; not verified against the live service.**
No OpenAI request was made and no API key was configured. Save/reopen preserves
18 objects and 13 wires with no GH1-hosted objects. The check verifies one local
POST, status 200, exact PNG bytes, all four RGBA pixels, bitmap dimensions and a
mesh with four vertices and one face. Reproduce from the repo root:

```sh
python tests/Swiftlet.Cases/checks/native_image_example.py --port 10501
```

That check starts and stops its own fixture and needs an exclusively owned Rhino
MCP endpoint. It writes evidence under `artifacts/native-examples`.

## Adapting it to the live API later

The [Images API reference](https://developers.openai.com/api/reference/resources/images/methods/generate)
now lists DALL-E 2 and 3 as retired. The sample request instead names
`gpt-image-2.5-sunburst`, requests one PNG, and handles the base64 response used by
GPT image models. Model availability and account access still need live checking.

For a separately authorised live run, change the URL to
`https://api.openai.com/v1/images/generations`, supply an Authorization header with
an appropriate bearer credential, and edit **Request JSON** for the prompt and
model. Do not send credentials to the local fixture: it rejects Authorization
headers. Set an appropriate request timeout in GH2's component settings before
enabling the real request. Real generation can incur API charges.

A full-size image produces a large mesh (one vertex per pixel). For display,
resize the decoded image before meshing, or connect another consumer to the
Bitmap/Byte Array output. This example does not save an image to disk or claim
that large-image display performance has been tested.
