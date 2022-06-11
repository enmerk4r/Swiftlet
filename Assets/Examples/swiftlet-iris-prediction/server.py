from flask import Flask, jsonify, request
import torch
from pprint import pprint
import numpy as np
import json

from sklearn.preprocessing import StandardScaler

# Create Flask app
app = Flask(__name__)

# Load the model
model = torch.jit.load('model.pt')

# Specify hardware device
device = torch.device('cpu')

# Specify human-readable Classes
classes = [
    "Iris Setosa",
    "Iris Versicolour",
    "Iris Virginica"
]

@app.route("/predict-iris", methods=["POST"])
def predict():

    # Load JSON data
    json_data = request.get_json()

    X = []

    for obj in json_data:

        sepal_length = obj["sepal_length_cm"]
        sepal_width = obj["sepal_width_cm"]
        petal_length = obj["petal_length_cm"]
        petal_width = obj["petal_width_cm"]

        # Convert to PyTorch tensor
        X.append([sepal_length, sepal_width, petal_length, petal_width])

    # Scale input data to have mean 0 and variance 1 
    scaler = StandardScaler()
    X_scaled = scaler.fit_transform(X)

    data = torch.tensor(X_scaled, dtype=torch.float32, device=device)

    # Run inference
    with torch.no_grad():
        prediction = model(data).numpy()

    # Convert to human-readable class name
    response_array = []
    for a in prediction:
        response_array.append(classes[np.argmax(a)])

    return jsonify(response_array)

if __name__ == '__main__':
    app.run(host='0.0.0.0', port=8000)