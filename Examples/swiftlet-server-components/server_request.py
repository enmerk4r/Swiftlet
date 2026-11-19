"""Call the local GH2 HTTP geometry server example."""
import argparse
import json
import urllib.request

if __name__ == '__main__':
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('route', choices=['volume', 'area'])
    p.add_argument('--points', type=int, default=8)
    p.add_argument('--size', type=float, default=10)
    p.add_argument('--port', type=int, default=18744)
    a = p.parse_args()
    if not 4 <= a.points <= 100 or not 0 < a.size <= 1000:
        p.error('For this demo use 4-100 points and a size greater than 0 and at most 1000.')
    request = urllib.request.Request('http://localhost:%d/voronoi-%s' % (a.port, a.route),
        data=json.dumps({'num_points': a.points, 'box_size': a.size}).encode(),
        headers={'Content-Type': 'application/json'})
    with urllib.request.urlopen(request, timeout=30) as response:
        print('HTTP', response.status)
        print(json.dumps(json.load(response), indent=2))
