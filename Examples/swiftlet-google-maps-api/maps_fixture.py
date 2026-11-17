"""Synthetic local fixtures for the GH2 Google Routes + Transitland example. No external calls."""
import argparse,json
from http.server import BaseHTTPRequestHandler,ThreadingHTTPServer
from urllib.parse import urlsplit,parse_qs
ROUTE=[[-73.9900,40.7500],[-73.9880,40.7520],[-73.9850,40.7540],[-73.9820,40.7550],[-73.9800,40.7580]]
STOPS=[[-73.9890,40.7510],[-73.9850,40.7540],[-73.9810,40.7560]]
MASK='routes.duration,routes.distanceMeters,routes.polyline.geoJsonLinestring'
class Handler(BaseHTTPRequestHandler):
    requests=[]
    def credentials(self):
        return any(self.headers.get(k) for k in ('Authorization','X-Goog-Api-Key','apikey')) or 'apikey' in parse_qs(urlsplit(self.path).query)
    def send_json(self,obj):
        raw=json.dumps(obj).encode();self.send_response(200);self.send_header('Content-Type','application/json');self.send_header('Content-Length',str(len(raw)));self.end_headers();self.wfile.write(raw)
    def do_POST(self):
        if self.credentials():self.send_error(400,'No credentials belong in this fixture');return
        if urlsplit(self.path).path!='/directions/v2:computeRoutes':self.send_error(404);return
        try:
            length=int(self.headers.get('Content-Length','0'))
            if not 0<length<65536:raise ValueError()
            body=json.loads(self.rfile.read(length))
            if body.get('polylineEncoding')!='GEO_JSON_LINESTRING' or body.get('travelMode') not in ('DRIVE','WALK','BICYCLE','TRANSIT'):raise ValueError()
            if self.headers.get('X-Goog-FieldMask')!=MASK:raise ValueError()
            for key in ('origin','destination'):
                ll=body[key]['location']['latLng'];float(ll['latitude']);float(ll['longitude'])
        except (ValueError,KeyError,TypeError):self.send_error(400,'Invalid route request');return
        self.requests.append({'method':'POST','path':self.path,'body':body})
        self.send_json({'routes':[{'distanceMeters':1200,'duration':'240s','polyline':{'geoJsonLinestring':{'type':'LineString','coordinates':ROUTE}}}]})
    def do_GET(self):
        if self.credentials():self.send_error(400,'No credentials belong in this fixture');return
        url=urlsplit(self.path);query=parse_qs(url.query)
        if url.path!='/api/v2/rest/stops':self.send_error(404);return
        if any(k not in query for k in ('lat','lon','radius')):self.send_error(400,'lat/lon/radius required');return
        self.requests.append({'method':'GET','path':url.path,'query':query})
        self.send_json({'stops':[{'stop_name':'Fixture stop '+str(i+1),'geometry':{'type':'Point','coordinates':p}} for i,p in enumerate(STOPS)]})
    def log_message(self,*args):pass
if __name__=='__main__':
    ap=argparse.ArgumentParser(description=__doc__);ap.add_argument('--port',type=int,default=18746);a=ap.parse_args()
    server=ThreadingHTTPServer(('127.0.0.1',a.port),Handler);print(f'Maps fixtures on http://127.0.0.1:{a.port}; Ctrl+C stops',flush=True)
    try:server.serve_forever()
    except KeyboardInterrupt:pass
    finally:server.server_close()
