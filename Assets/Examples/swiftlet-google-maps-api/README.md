## 1. Create a Transitland API key
[Transitland](https://www.transit.land/) API has a free tier. You can register on their website and request your free API key by following [these instructions](https://www.transit.land/documentation#signing-up-for-an-api-key)

## 2. Create Google Maps API key
First of all, you'll have to create an account on [Google Cloud Platform](https://cloud.google.com/). Google Maps API is a paid service, however each API call is pretty cheap. Besides, your account is credited with $200 of free money each month, so chances are you won't ever get charged for your API calls. So, feel free to play around (though, if you accidentally write some insane For loop that bankrupts your account - that's on you, so don't hold me responsible hehe).

You can find instructions on how to enable the Google Maps API and generate your key [here](https://developers.google.com/maps/documentation/javascript/get-api-key). 

## 3. Enter your API keys
Assign your API keys to the corresponding text params in the top left corner of the definition. This will allow Swiftlet to make authenticated web requests to both API services.

## 4. Create Origin and Destination points
Create two points inside of your Rhino document. Make sure that the points lie within the meshes representing the boroughs (in other words - "on dry land"). Then assign each point to the corresponding param in the top left of the Grasshoper canvas. You should see a path being generated between the two points - this is a polyline representing Google Maps driving navigations

## 5. Change navigation mode
You can switch between Driving, Walking, Bicycling and Transit navigation in the dropdown located under "Navigation Settings" on the left hand side