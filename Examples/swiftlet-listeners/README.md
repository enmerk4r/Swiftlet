# Listeners
Up until the 0.1.6 release, Swiftlet was only able to work as a one-way web client: it could send web requests and then receive web responses. But with the introduction of the HTTP Listener and the Socket Listener, you can now have other services stream data to your Grasshopper definition! For example, you can create definitions that respond to [WebHooks](https://www.redhat.com/en/topics/automation/what-is-a-webhook), or stream real-time data from a [WebSocket](https://developer.mozilla.org/en-US/docs/Web/API/WebSockets_API)!

**Disclaimer:** Both of these components are essentially early prototypes of this new functionality. Please, let me know if you run into any issues and use them at your own risk!

## HTTP Listener
The HTTP Listener component attaches to a specified port and waits for incoming requests. Once a request arrives, it outputs the request type and the contents of the body (if there is one). 

### Ports
Remember, that only one component can be attached to one port at one time! So if you are using several of these listeners in your definition, make sure that they all listen on different ports. You can pick any port from 0 to 65,536, just remember that some ports can be used by other services. Ports 0 to 1024 are considered "priveleged", so I would recommend picking a number above 1024.

### Routes
You can also specify an optional route for your HTTP Listener. By default, the component will listen to requests addressed to `http://localhost:[PORT-NUMBER]/`, but you can also extend that URL as `http://localhost:[PORT-NUMBER]/my-route-name/`. This is helpful when you have several of these listeners running in onde definition and you want to differentiate their function by assigning each of them a specific route.

### Responses
By default, HTTP Listener will return a response with an empty body to whatever is calling into it. However, you can pass it a "pre-canned" response to be fired back at the client in the form of an HTTP body. Essentially, this means that you can use these components to serve static content.

### Getting requests from the Web
The HTTP Listener runs on your `localhost`, which means that while local services can talk to it over HTTP, it will not be discoverable by resources outside of your machine. In order to use this component for things like WebHooks, or to be able to receive requests from outside of your system in general, you'll need to do a bit of additional setup.

One of the easiest ways to expose a port on your machine to the outside world is [ngrok](https://ngrok.com/). It is cross-platform and very easy to setup:

- Download ngrok from the [official website](https://ngrok.com/download)
- Sign up in order to generate a token (there is a free tier)
- Add your token to ngrok by running `ngrok config add-authtoken <token>`
- Expose a specific port on your machine by running `ngrok http <port> --host-header="localhost:<port>"`, where `<port>` is an integer representing the port you picked for the HTTP LIstener
- Once ngrok starts running, it will display a Forwarding URL. Use that URL to reach your HTTP Listener from outside of your machine

## Socket Listener
A Socket Listener is used to attach to a real-time data stream (such as a stock market API for example). It is essentially a fire hose of information that gets streamed to your socket client.

### URL and Query Params
The Socket Listener component takes a URL for the secket service, as well as query params that can be used for authentication. It is not uncommon for even free socket APIs to require an auth token.

### On Open
The On Open input lets you queue a set of messages to be sent to the server once the socket connection is established. This is often a necessary step that lets the server know what type of information you would like to subscribe to, or what format you prefer it in.

### Stopping the component
The Socket Listener runs asynchronously, but it will expire solution every time a new socket message comes in. Depending on the rate of data and your system specs, you may or may not lose control of the canvas. An easy workaround is to temporarily turn off your internet connection and wait for a few seconds. The component will make 5 attempts to re-establish the connection with the socket server and will then stop completely. Not a great UX, but again... this is just a prototype.