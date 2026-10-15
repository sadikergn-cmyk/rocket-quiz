"""Serves the Unity WebGL build locally, with the headers Brotli/gzip builds need.

Usage: python tools/serve_webgl.py [port]   (default 8090)
"""
import http.server
import os
import sys

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "Game", "Builds", "WebGL")
TYPES = {".wasm": "application/wasm", ".js": "application/javascript", ".data": "application/octet-stream"}
ENCODINGS = {".br": "br", ".gz": "gzip"}


class Handler(http.server.SimpleHTTPRequestHandler):
    def __init__(self, *args, **kwargs):
        super().__init__(*args, directory=ROOT, **kwargs)

    def end_headers(self):
        path = self.path.split("?")[0]
        base, extension = os.path.splitext(path)
        if extension in ENCODINGS:
            self.send_header("Content-Encoding", ENCODINGS[extension])
        self.send_header("Cache-Control", "no-store")
        super().end_headers()

    def guess_type(self, path):
        base, extension = os.path.splitext(path)
        if extension in ENCODINGS:
            extension = os.path.splitext(base)[1]
        return TYPES.get(extension, super().guess_type(path))


if __name__ == "__main__":
    port = int(sys.argv[1]) if len(sys.argv) > 1 else 8090
    print("RocketQuiz WebGL: http://localhost:%d" % port)
    http.server.ThreadingHTTPServer(("127.0.0.1", port), Handler).serve_forever()
