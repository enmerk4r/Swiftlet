![](https://github.com/enmerk4r/Swiftlet/blob/main/Assets/Logo/Logo_Readme.png)

# Swiftlet
Swiftlet is a free, open source Grasshopper plugin that allows users to construct and send web requests, as well as parse JSON responses from API endpoints.

Swiftlet ships for both Grasshopper 1 and Grasshopper 2 from this one repository. The two versions offer the same components and share their logic; the main development entry point is `Swiftlet.sln`.

| | Grasshopper 1 | Grasshopper 2 |
|---|---|---|
| Rhino | 8 and 9 | 9 |
| Plug-in | `Swiftlet.Gh.Rhino8.gha` (`src/Swiftlet.Gh1`) | `Swiftlet.Gh2.rhp` (`src/Swiftlet.Gh2`) |

Grasshopper 1 definitions that use Swiftlet open in Grasshopper 2 with their Swiftlet components converted to the native Grasshopper 2 ones.

## Prerequisites
* Rhino 8 or up (Rhino 9 for Grasshopper 2)
* Grasshopper

## Examples
* You can find example files [here](https://github.com/enmerk4r/Swiftlet/tree/main/Examples)
* Also check out this [McNeel Webinar](https://www.youtube.com/watch?v=j1Y7mkPRmXE)

## Questions?
Check out the [Wiki](https://github.com/enmerk4r/Swiftlet/wiki)!

## Get Swiftlet
Swiftlet is available on [food4rhino](https://www.food4rhino.com/en/app/swiftlet) and via the [Yak](https://developer.rhino3d.com/guides/yak/what-is-yak/) package manager.

One `swiftlet` Yak package serves both: the Rhino 8 distribution contains the Grasshopper 1 plug-in, and the Rhino 9 distribution contains the Grasshopper 1 and Grasshopper 2 plug-ins together.

## Repository layout
* `src/Swiftlet.Core`, `Swiftlet.Imaging`, `Swiftlet.Runtime`, `Swiftlet.Hosts.*`: shared logic, no Grasshopper dependency
* `src/Swiftlet.Gh1`, `src/Swiftlet.Gh2`: the two Grasshopper shells
* `src/SwiftletBridge`: helper executable used by the server, WebSocket and MCP components
* `tests/Swiftlet.Cases`: runs every component in Grasshopper 1 and Grasshopper 2 and compares the results
* `docs/gh2-porting.md`, `docs/parity.json`: how the Grasshopper 2 port is built and which component maps to which

## Support Swiftlet
Do you find Swiftlet useful? [Buy me a coffe! :)](https://www.buymeacoffee.com/enmerk4r)