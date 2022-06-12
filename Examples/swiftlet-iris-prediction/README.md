This example demonstrates how to train a PyTorch model on the [Iris dataset](https://archive.ics.uci.edu/ml/datasets/iris), serve it locally with a simple Python Flask server and then call into it from Grasshopper using Swiftlet

## 1. Install Python Dependencies
```pip install -r requirements.txt```

## 2. Train the model
```python train.py```

## 3. Run the server
```python server.py```

## 4. Call into the server
Use either one of the Grasshopper files to send requests to the server with Swiftlet
