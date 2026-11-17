# WebSocket loopback in Grasshopper 2

Open `swiftlet-websocket-loopback.ghz` in Grasshopper 2 with Swiftlet 0.4.0 installed in Rhino 9. It contains both a local WebSocket server and a client, so no external service or Python installation is needed.

This replaces the teaching example based on the archived Socket Listener. The original `swiftlet-socket-listener.gh` remains a Grasshopper 1 example. The GH2 file uses the current WebSocket Client, WebSocket Server and WebSocket Send components.

All Run and Send inputs are saved **false**. Select the named component to edit its input values:

1. On **Local server**, set Run to true. Its status should say `Listening on ws://localhost:18742/`.
2. On **Client**, set Run to true. Its status should become `Connected`.
3. On **Send to server**, set Send to true, then back to false. The server's received-message panel should show `hello from Swiftlet`.
4. On **Reply to client**, set Send to true, then back to false. The client's received-message panel should show `subscribed: origami`.
5. Stop the client by setting its Run to false, then stop the server the same way. The server status should become `Stopped`.

Send remains true until you change it. Leaving it true can send the message again on a later recompute. For a real subscription service, replace the message with that service's subscription command and send it after the client connects; this example does not automatically send on every connection.

Port 18742 must be free. To use a different port, change both Local server's Port and Client's URL. There is no need to expose this example to the internet.

**Verified:** saved and reopened in GH2 with no GH1-hosted objects; real local WebSocket connection; exact message delivery in both directions; no component errors; server stopped afterward. This tests the local transport, not a third-party subscription API.
