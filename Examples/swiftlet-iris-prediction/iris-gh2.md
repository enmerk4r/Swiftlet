# Iris classification in Grasshopper 2

Open `iris-3d.ghz` in Rhino 9 / Grasshopper 2 with Swiftlet 0.4.0 installed.
The GH1 original, `iris-3d.gh`, is unchanged.

## Run locally

From this directory, create a Python environment and install the dependencies:

```sh
python -m venv .venv
```

Activate it (`.venv\Scripts\activate` on Windows), then:

```sh
python -m pip install -r requirements.txt
python -c "import server; server.app.run(host='127.0.0.1', port=8000)"
```

This uses the bundled `model.pt`; retraining is optional. Keep the terminal open.
In GH2 enable **Predict Iris**. Its URL is `http://127.0.0.1:8000/predict-iris`.
The component is saved disabled, so opening the file makes no network request.
After a successful request the status is 200 and the Predictions panel contains
150 class names. Disable Predict Iris when finished, then stop Python with Ctrl+C.

The native 3D scatter plot uses sepal length, sepal width and petal length as
X, Y and Z, in centimetres. Petal width is also sent to the model. Blue points are
Iris Setosa, orange points Iris Versicolour, and purple points Iris Virginica.
Use Zoom Extents in Rhino to see the plot; coincident measurements overlap.
The **150 Iris samples** component holds the editable JSON input, copied from
the original GH1 example and also stored in `iris-samples.json`. Editing that file
alone does not change the saved graph; paste the updated JSON into the component.

## Verification

**Output verified against the bundled local Flask/PyTorch service on Windows.**
The saved graph reopens with 30 objects, 29 wires and no GH1-hosted components.
All 150 response labels matched a separate request to the same model. Every
point coordinate and its class assignment were checked, with 49 Setosa,
48 Versicolour and 53 Virginica predictions for the bundled batch/model.
These are model predictions, not a classification-accuracy score.
The server fits its scaler to each submitted batch, so predictions can change
when the batch changes. No external service, account or API key is used.

The check is reproducible with the local model running and an exclusively owned
Rhino 9 MCP endpoint:

```sh
python tests/Swiftlet.Cases/checks/native_iris_example.py --port 10500
```

Run that command from the repository root. It regenerates the example and writes
its verification report and canvas render under `artifacts/native-examples`.

## Difference from the original

The GH1 example's flower construction is replaced by the simpler native scatter
plot. Its request inputs, model and prediction labels are retained. The first
rebuild attempt introduced a connection cycle by selecting the wrong replacement
node; this was a generator mistake and is not evidence of a Rhino defect. After
correcting it, the converted flower graph completed but had text/integer division
faults and, after explicit numeric conversion, native Explode Surface path-ordering
exceptions followed by empty loft inputs. That converted geometry is not shipped.
No claim of equivalent flower geometry or of a minimal McNeel reproducer is made.
