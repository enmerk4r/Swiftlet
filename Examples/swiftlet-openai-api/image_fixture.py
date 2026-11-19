"""Local, deterministic Images API-shaped fixture. Does not generate images or call OpenAI."""
import argparse,base64,json,struct,zlib
from http.server import BaseHTTPRequestHandler,ThreadingHTTPServer

def fixture_png():
    def chunk(kind,data):
        return struct.pack('!I',len(data))+kind+data+struct.pack('!I',zlib.crc32(kind+data)&0xffffffff)
    # Top row red/green; bottom row blue/white. RGB, 2x2, no external image library.
    raw=b'\0\xff\0\0\0\xff\0\0\0\0\xff\xff\xff\xff'
    return b'\x89PNG\r\n\x1a\n'+chunk(b'IHDR',struct.pack('!IIBBBBB',2,2,8,2,0,0,0))+chunk(b'IDAT',zlib.compress(raw))+chunk(b'IEND',b'')

class Handler(BaseHTTPRequestHandler):
    requests=[]
    def do_POST(self):
        if self.path!='/v1/images/generations':self.send_error(404);return
        if self.headers.get('Authorization'):self.send_error(400,'Do not send credentials to this fixture');return
        try:
            size=int(self.headers.get('Content-Length','0'))
            if not 0<size<65536:raise ValueError('Request size')
            request=json.loads(self.rfile.read(size))
            if not isinstance(request.get('prompt'),str) or not request['prompt'].strip():raise ValueError('Prompt required')
            if not isinstance(request.get('model'),str) or request.get('n')!=1 or request.get('output_format')!='png':raise ValueError('Expected model, n=1, output_format=png')
        except (ValueError,TypeError):self.send_error(400,'Invalid fixture request');return
        self.requests.append(request)
        response=json.dumps({'created':0,'data':[{'b64_json':base64.b64encode(fixture_png()).decode()}],'output_format':'png'}).encode()
        self.send_response(200);self.send_header('Content-Type','application/json');self.send_header('Content-Length',str(len(response)));self.end_headers();self.wfile.write(response)
    def log_message(self,*args):pass

if __name__=='__main__':
    ap=argparse.ArgumentParser(description=__doc__);ap.add_argument('--port',type=int,default=18745);a=ap.parse_args()
    server=ThreadingHTTPServer(('127.0.0.1',a.port),Handler)
    print(f'Offline image fixture: http://127.0.0.1:{a.port}/v1/images/generations (Ctrl+C stops)',flush=True)
    try:server.serve_forever()
    except KeyboardInterrupt:pass
    finally:server.server_close()
