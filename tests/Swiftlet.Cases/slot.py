"""Start a private, fresh Rhino 9 for testing and keep it alive until asked to stop.

    python tests/Swiftlet.Cases/slot.py --stop-file C:\\path\\to\\my.stop [--package-dir DIR]

Starts the Rhino AI router that ships with Rhino 9, asks it for a new Rhino 9 process and prints

    SLOT {"slotId": "...", "port": 10504, "pid": 12345, ...}

Use that port with cases.py / the HTTP helper. The script then waits; create the stop file to shut the
Rhino down (the router closes the Rhino it spawned when it exits). Run it as a background task.

--package-dir sets RHINO_PACKAGE_DIRS so Rhino loads a Yak package layout from that folder at startup
(point it at the folder that holds the plug-in files, not its parent). Without it you get a clean Rhino
with no Swiftlet loaded, ready for `cases.py --load`.

Each agent owns the slots it starts. Never close or reuse another agent's port.
"""
import argparse, json, os, queue, subprocess, sys, threading, time

ROUTER = r'C:\Program Files\Rhino 9\Plug-ins\RhinoAI\router\win-x64\rhino-mcp-router.exe'

ap = argparse.ArgumentParser()
ap.add_argument('--stop-file', required=True)
ap.add_argument('--package-dir')
args = ap.parse_args()

env = dict(os.environ)
if args.package_dir:
    env['RHINO_PACKAGE_DIRS'] = args.package_dir
if os.path.exists(args.stop_file):
    os.remove(args.stop_file)

proc = subprocess.Popen([ROUTER, '--default-version', '9'], stdin=subprocess.PIPE, stdout=subprocess.PIPE,
                        stderr=subprocess.DEVNULL, text=True, bufsize=1, env=env,
                        creationflags=getattr(subprocess, 'CREATE_NO_WINDOW', 0))
replies = queue.Queue()


def pump():
    for line in proc.stdout:
        try:
            replies.put(json.loads(line))
        except ValueError:
            pass
    replies.put({'process_exit': proc.poll()})


threading.Thread(target=pump, daemon=True).start()
counter = 0


def request(method, params, timeout=240):
    global counter
    counter += 1
    proc.stdin.write(json.dumps({'jsonrpc': '2.0', 'id': counter, 'method': method, 'params': params}) + '\n')
    proc.stdin.flush()
    end = time.monotonic() + timeout
    while time.monotonic() < end:
        reply = replies.get(timeout=max(0.1, end - time.monotonic()))
        if 'process_exit' in reply:
            raise RuntimeError('router exited: %r' % reply)
        if reply.get('id') == counter:
            return reply
    raise TimeoutError(method)


request('initialize', {'protocolVersion': '2024-11-05', 'capabilities': {}, 'clientInfo': {'name': 'swiftlet-slot', 'version': '1'}})
proc.stdin.write(json.dumps({'jsonrpc': '2.0', 'method': 'notifications/initialized'}) + '\n')
proc.stdin.flush()
spawn = request('tools/call', {'name': 'spawn_slot', 'arguments': {'version': '9'}})
text = ''.join(c.get('text', '') for c in spawn.get('result', {}).get('content', []))
try:
    payload = json.loads(text).get('payload', {})
except ValueError:
    payload = {'raw': text}
print('SLOT ' + json.dumps(payload), flush=True)

while not os.path.exists(args.stop_file) and proc.poll() is None:
    time.sleep(2)
print('STOPPING', flush=True)
try:
    request('tools/call', {'name': 'close_slot', 'arguments': {'slot': payload.get('slotId', '')}}, timeout=60)
except Exception:
    pass
proc.terminate()
