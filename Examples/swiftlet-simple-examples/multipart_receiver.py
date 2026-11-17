"""Local multipart receiver for the Swiftlet GH2 example (Python standard library only)."""
import argparse
import json
from email import policy
from email.parser import BytesParser
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer


class Receiver(BaseHTTPRequestHandler):
    def do_POST(self):
        if self.path != '/multipart':
            self.send_error(404)
            return
        length = int(self.headers.get('Content-Length', '0'))
        if not 0 < length <= 1024 * 1024:
            self.send_error(413, 'Expected a body of at most 1 MiB')
            return
        data = self.rfile.read(length)
        mime = BytesParser(policy=policy.default).parsebytes(
            ('Content-Type: ' + self.headers.get('Content-Type', '') + '\r\nMIME-Version: 1.0\r\n\r\n').encode('ascii') + data)
        if not mime.is_multipart():
            self.send_error(400, 'Expected multipart/form-data')
            return
        fields = []
        for part in mime.iter_parts():
            value = part.get_payload(decode=True)
            fields.append({'name': part.get_param('name', header='content-disposition'),
                           'filename': part.get_filename(), 'content_type': part.get_content_type(),
                           'text': value.decode('utf-8'), 'bytes': len(value)})
        result = {'fields': fields}
        self.server.received.append(result)
        body = json.dumps(result, ensure_ascii=False).encode('utf-8')
        self.send_response(200)
        self.send_header('Content-Type', 'application/json; charset=utf-8')
        self.send_header('Content-Length', str(len(body)))
        self.end_headers()
        self.wfile.write(body)
        print(json.dumps(result, ensure_ascii=False), flush=True)


def make_server(port=18741):
    server = ThreadingHTTPServer(('127.0.0.1', port), Receiver)
    server.received = []
    return server


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--port', type=int, default=18741)
    args = parser.parse_args()
    server = make_server(args.port)
    print('Listening on http://127.0.0.1:%d/multipart; Ctrl+C to stop.' % server.server_port, flush=True)
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        pass
    finally:
        server.server_close()
