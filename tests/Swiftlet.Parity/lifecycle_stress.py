"""Combined native GH2 resource stress. Dedicated slot only; all traffic is loopback and browser launch is manual.
Example: python lifecycle_stress.py --port 10503 --pid 43000 --seconds 600 --cycles 25 --report out.json
"""
import argparse, base64, concurrent.futures, hashlib, json, os, socket, struct, subprocess, time, urllib.error, urllib.request
from pathlib import Path
from parity import RhinoMcp
from validate_server_input import free_port
NAMES = ['ServerInputComponent', 'McpServerComponent', 'HttpListenerComponent', 'WebSocketServerComponent', 'OAuthAuthorizeComponent']

class WebSocket:

    def __init__(self, port):
        self.s = socket.create_connection(('127.0.0.1', port), timeout=6)
        self.s.settimeout(6)
        self.buf = b''
        key = base64.b64encode(os.urandom(16)).decode()
        self.s.sendall(f'GET / HTTP/1.1\r\nHost: 127.0.0.1:{port}\r\nUpgrade: websocket\r\nConnection: Upgrade\r\nSec-WebSocket-Key: {key}\r\nSec-WebSocket-Version: 13\r\n\r\n'.encode())
        while b'\r\n\r\n' not in self.buf:
            data = self.s.recv(4096)
            if not data:
                raise ConnectionError('WebSocket closed during handshake')
            self.buf += data
        header, self.buf = self.buf.split(b'\r\n\r\n', 1)
        assert b' 101 ' in header, header
        accept = base64.b64encode(hashlib.sha1((key + '258EAFA5-E914-47DA-95CA-C5AB0DC85B11').encode()).digest())
        headers = {line.partition(b':')[0].lower(): line.partition(b':')[2].strip() for line in header.split(b'\r\n')[1:]}
        assert headers.get(b'sec-websocket-accept') == accept, header

    def read(self, n):
        while len(self.buf) < n:
            data = self.s.recv(4096)
            if not data:
                raise ConnectionError('WebSocket closed')
            self.buf += data
        result, self.buf = (self.buf[:n], self.buf[n:])
        return result

    def send(self, text, opcode=1):
        data = text.encode() if isinstance(text, str) else text
        mask = os.urandom(4)
        length = len(data)
        prefix = bytes([128 | opcode, 128 | length]) if length < 126 else bytes([128 | opcode, 128 | 126]) + struct.pack('!H', length)
        self.s.sendall(prefix + mask + bytes((v ^ mask[i % 4] for i, v in enumerate(data))))

    def receive(self):
        while True:
            first, second = self.read(2)
            length = second & 127
            if length == 126:
                length = struct.unpack('!H', self.read(2))[0]
            elif length == 127:
                length = struct.unpack('!Q', self.read(8))[0]
            mask = self.read(4) if second & 128 else None
            data = self.read(length)
            if mask:
                data = bytes((v ^ mask[i % 4] for i, v in enumerate(data)))
            if first & 15 == 9:
                self.send(data, 10)
                continue
            if first & 15 == 8:
                raise ConnectionError('WebSocket close frame')
            if first & 15 == 1:
                return data.decode()

    def close(self):
        try:
            self.send(b'', 8)
        except Exception:
            pass
        self.s.close()

class Stress:

    def __init__(self, a):
        self.a = a
        self.client = RhinoMcp(a.port, timeout=45)
        self.source = Path(__file__).with_suffix('.csx').read_text(encoding='utf-8-sig')
        self.ports = []
        self.all_ports = set()
        self.records = []
        self.requests = 0
        self.aborted = 0
        self.started = time.monotonic()
        self.report = {'status': 'running', 'pid': a.pid, 'cycles': self.records, 'traffic_completed': 0, 'traffic_interrupted': 0, 'interruption_kinds': {}, 'commit': subprocess.check_output(['git', 'rev-parse', 'HEAD'], text=True).strip()}

    def save(self):
        self.report.update(elapsed_seconds=round(time.monotonic() - self.started, 2), traffic_completed=self.requests, traffic_interrupted=self.aborted)
        self.a.report.parent.mkdir(parents=True, exist_ok=True)
        self.a.report.write_text(json.dumps(self.report, indent=2), encoding='utf-8')

    def command(self, action, **kw):
        script = self.source.replace('__COMMAND__', json.dumps({'action': action, **kw, 'pid': self.a.pid}).replace('"', '""'))
        result = self.client.call('run_csharp', {'script': script})
        if result.get('stderr'):
            raise RuntimeError(result['stderr'])
        line = next((line for line in result.get('stdout', '').splitlines() if line.startswith('STRESS:')), None)
        if not line:
            raise RuntimeError(result)
        value = json.loads(line[7:])
        assert value['pid'] == self.a.pid, value
        if not value.get('closed'):
            assert value['root'] is None, 'DOTNET_ROOT must be unset'
        self.last = value
        return value

    def children(self):
        code = f"Get-CimInstance Win32_Process -Filter 'ParentProcessId = {self.a.pid}' | Where-Object {{ $_.Name -like 'SwiftletBridge*' -or ($_.Name -eq 'dotnet.exe' -and $_.CommandLine -like '*SwiftletBridge*') }} | Select-Object ProcessId,ParentProcessId,Name | ConvertTo-Json -Compress"
        out = subprocess.check_output(['powershell', '-NoProfile', '-NonInteractive', '-Command', code], text=True, timeout=15).strip()
        value = json.loads(out) if out else []
        return value if isinstance(value, list) else [value]

    def listener_records(self):
        raw = subprocess.check_output(['netstat', '-ano', '-p', 'tcp'], text=True, timeout=15)
        ports = []
        for line in raw.splitlines():
            parts = line.split()
            if len(parts) >= 5 and parts[-2].upper() == 'LISTENING':
                try:
                    ports.append({'port': int(parts[1].rsplit(':', 1)[1]), 'pid': int(parts[-1])})
                except ValueError:
                    pass
        return ports

    def listeners(self):
        return {r['port'] for r in self.listener_records()}

    def listening(self, p):
        return p in self.listeners()

    def resources(self, open_indices, bridge_count, timeout=8):
        deadline = time.monotonic() + timeout
        while True:
            listeners = self.listeners()
            bad = [p for i, p in enumerate(self.ports) if (p in listeners) != (i in open_indices)]
            children = self.children()
            if not bad and len(children) == bridge_count:
                return {'ports': self.ports.copy(), 'bridges': children}
            if time.monotonic() > deadline:
                raise AssertionError({'unexpected_listening_state': bad, 'expected_open': [self.ports[i] for i in open_indices], 'bridges': children, 'expected_bridges': bridge_count})
            time.sleep(0.05)

    def new_ports(self):
        values = []
        while len(values) < 5:
            p = free_port()
            if p not in self.all_ports:
                values.append(p)
                self.all_ports.add(p)
        return values

    def setup(self):
        self.ports = self.new_ports()
        self.command('setup', ports=self.ports)
        return self.resources(set(range(5)), 4)

    def http(self, port, path, expected):
        req = urllib.request.Request(f'http://127.0.0.1:{port}{path}', data=b'stress', method='POST')
        with urllib.request.urlopen(req, timeout=6) as r:
            body = r.read().decode()
            assert r.status == 200 and body == expected, (r.status, body)
        return 1

    def rpc(self, method, params=None, sid=None, id=1):
        headers = {'Content-Type': 'application/json', 'Accept': 'application/json,text/event-stream'}
        if sid:
            headers['Mcp-Session-Id'] = sid
        req = urllib.request.Request(f'http://127.0.0.1:{self.ports[1]}/mcp/', json.dumps({'jsonrpc': '2.0', 'id': id, 'method': method, 'params': params or {}}).encode(), headers)
        with urllib.request.urlopen(req, timeout=7) as r:
            raw = r.read().decode()
            sid = r.headers.get('Mcp-Session-Id', sid)
        if raw.startswith(('event:', 'data:')):
            raw = next((x[5:].strip() for x in raw.splitlines() if x.startswith('data:')))
        return (json.loads(raw), sid)

    def mcp(self, sid, n):
        result, _ = self.rpc('tools/call', {'name': 'echo', 'arguments': {'cycle': n}}, sid, n)
        if 'error' in result or result.get('result', {}).get('isError'):
            raise ConnectionAbortedError(str(result))
        assert result.get('id') == n, result
        assert result['result']['content'][0]['text'] == 'mcp-ok', result
        return 1

    def websocket(self, n):
        client = WebSocket(self.ports[3])
        expected = {f'stress-{n}-{i}' for i in range(8)}
        received = set()
        try:
            for text in sorted(expected):
                client.send(text)
            received = set()
            deadline = time.monotonic() + 7
            while received != expected:
                text = client.receive()
                assert text in expected, text
                received.add(text)
                if time.monotonic() > deadline:
                    raise TimeoutError({'expected': expected, 'received': received})
            return len(received)
        except TimeoutError as error:
            raise TimeoutError(f'WebSocket burst {n}: received={sorted(received)}, missing={sorted(expected-received)}') from error
        finally:
            client.close()

    def callback(self, state):
        with urllib.request.urlopen(f'http://127.0.0.1:{self.ports[4]}/callback/?code=stress-code&state={state}', timeout=5) as r:
            assert r.status == 200
            return 1

    def burst(self, pool, n, complete_oauth=False):
        _, sid = self.rpc('initialize')
        futures = []
        for i in range(8):
            futures.append(pool.submit(self.http, self.ports[0], f'/stress/{n}/{i}', 'server-ok'))
            futures.append(pool.submit(self.http, self.ports[2], f'/stress/?cycle={n}&i={i}', 'listener-ok'))
            futures.append(pool.submit(self.mcp, sid, n * 100 + i))
        futures.append(pool.submit(self.websocket, n))
        if complete_oauth:
            state = next((c['state'] for c in self.command('snapshot')['components'] if c['name'] == NAMES[4]))
            assert state
            futures.append(pool.submit(self.callback, state))
        return futures

    def finish(self, futures, interrupted=False):
        failures = []
        for f in futures:
            try:
                self.requests += f.result(timeout=10)
            except Exception as e:
                if interrupted and isinstance(e, (urllib.error.URLError, ConnectionError, TimeoutError, OSError)):
                    self.aborted += 1
                    kind = type(e).__name__
                    self.report['interruption_kinds'][kind] = self.report['interruption_kinds'].get(kind, 0) + 1
                else:
                    failures.append(f'{type(e).__name__}: {e}')
        if failures:
            raise AssertionError({'normal_traffic_failed': failures})

    def run(self):
        self.command('snapshot')
        self.report['baseline_bridges'] = self.children()
        assert not self.report['baseline_bridges']
        self.report['initial'] = self.setup()
        self.save()
        cycle = 0
        with concurrent.futures.ThreadPoolExecutor(max_workers=40) as pool:
            while cycle < self.a.cycles or time.monotonic() - self.started < self.a.seconds:
                before = time.monotonic()
                self.finish(self.burst(pool, cycle, True))
                self.resources({0, 1, 2, 3}, 4)
                self.command('restart-oauth')
                self.resources(set(range(5)), 4)
                operation = ['disable-enable', 'ports', 'delete-undo-redo-undo', 'run-toggle', 'document-close'][cycle % 5]
                pending = self.burst(pool, 10000 + cycle)
                time.sleep(0.03)
                if operation == 'disable-enable':
                    self.command('disable')
                    stopped = self.resources(set(), 0)
                    self.finish(pending, True)
                    self.command('enable')
                elif operation == 'ports':
                    old = self.ports.copy()
                    self.ports = self.new_ports()
                    self.command('ports', ports=self.ports)
                    self.finish(pending, True)
                    assert not set(old) & self.listeners(), old
                    stopped = {'closed_old_ports': old}
                elif operation == 'delete-undo-redo-undo':
                    self.command('delete')
                    stopped = self.resources(set(), 0)
                    self.finish(pending, True)
                    self.command('undo')
                    self.resources(set(range(5)), 4)
                    self.command('redo')
                    self.resources(set(), 0)
                    self.command('undo')
                elif operation == 'run-toggle':
                    self.command('run', run=False)
                    stopped = self.resources({0, 1, 2, 4}, 3)
                    self.finish(pending, True)
                    self.command('run', run=True)
                else:
                    self.command('close')
                    stopped = self.resources(set(), 0)
                    self.finish(pending, True)
                    self.setup()
                active = self.resources(set(range(5)), 4)
                snapshot = self.command('snapshot')
                session_pids = {c['bridge'] for c in snapshot['components'] if c['bridge']}
                assert session_pids == {p['ProcessId'] for p in active['bridges']}, (snapshot, active)
                faults = [c for c in snapshot['components'] if c['fault']]
                assert not faults, faults
                assert len(snapshot['components']) == 5, snapshot
                assert next((c for c in snapshot['components'] if c['name'] == NAMES[0]))['outputs'] == ['/', '/stress']
                assert next((c for c in snapshot['components'] if c['name'] == NAMES[1]))['outputs'] == ['echo']
                self.records.append({'cycle': cycle, 'operation': operation, 'seconds': round(time.monotonic() - before, 2), 'stopped': stopped, 'active': active, 'snapshot': snapshot})
                cycle += 1
                self.save()
                print(f'PASS cycle {cycle}: {operation}; completed={self.requests}, interrupted={self.aborted}, elapsed={time.monotonic() - self.started:.1f}s', flush=True)
            self.command('close')
            self.report['final'] = self.resources(set(), 0)
            remaining = self.listener_records()
            leaked = [r for r in remaining if r['port'] in self.all_ports and r['pid'] == self.a.pid]
            assert not leaked, leaked
            self.report['historical_ports_reused_by_other_processes'] = [r for r in remaining if r['port'] in self.all_ports and r['pid'] != self.a.pid]
            live = self.client.call('run_csharp', {'script': 'Console.WriteLine(Environment.ProcessId);'})
            assert int(live['stdout'].strip()) == self.a.pid, live
            self.report.update(status='passed', leaked_ports=leaked, all_ports=sorted(self.all_ports), final_rhino_alive=True)
        return self.report

def main():
    p = argparse.ArgumentParser()
    p.add_argument('--port', type=int, required=True)
    p.add_argument('--pid', type=int, required=True)
    p.add_argument('--seconds', type=float, default=600)
    p.add_argument('--cycles', type=int, default=25)
    p.add_argument('--report', type=Path, required=True)
    a = p.parse_args()
    s = Stress(a)
    try:
        s.run()
    except Exception as e:
        s.report.update(status='failed', error=f'{type(e).__name__}: {e}')
        try:
            s.report['failure_snapshot'] = s.command('snapshot')
            s.command('close')
            s.report['cleanup'] = s.resources(set(), 0)
        except Exception as cleanup:
            s.report['cleanup_error'] = str(cleanup)
        raise
    finally:
        s.save()
        print(s.report['status'].upper(), s.report.get('error', ''), flush=True)
if __name__ == '__main__':
    main()
