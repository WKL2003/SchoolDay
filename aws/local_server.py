#!/usr/bin/env python3
"""Same routes as the Lambda. File store until TABLE_NAME is set."""

import json
import os
import sys
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer

HERE = os.path.dirname(os.path.abspath(__file__))
SRC = os.path.join(HERE, "src")
DASHBOARD = os.path.join(os.path.dirname(HERE), "dashboard")
sys.path.insert(0, SRC)

import handler  # noqa: E402


class App(BaseHTTPRequestHandler):
    def do_OPTIONS(self):
        self._send(204, b"", "text/plain")

    def do_GET(self):
        path = self.path.split("?", 1)[0]
        if path in ("/events", "/summary", "/health"):
            self._api("GET", path, None)
            return
        self._static(path)

    def do_POST(self):
        path = self.path.split("?", 1)[0]
        length = int(self.headers.get("Content-Length") or "0")
        body = self.rfile.read(length).decode("utf-8") if length else "{}"
        self._api("POST", path, body)

    def _api(self, method, path, body):
        event = {
            "requestContext": {"http": {"method": method}},
            "rawPath": path,
            "body": body,
        }
        result = handler.lambda_handler(event, None)
        status = int(result.get("statusCode") or 500)
        payload = (result.get("body") or "").encode("utf-8")
        self._send(status, payload, "application/json")

    def _static(self, path):
        if path in ("", "/"):
            path = "/index.html"
        rel = path.lstrip("/").replace("..", "")
        full = os.path.join(DASHBOARD, rel)
        if not os.path.isfile(full):
            self._send(404, b'{"error":"not found"}', "application/json")
            return
        ext = os.path.splitext(full)[1]
        types = {
            ".html": "text/html; charset=utf-8",
            ".js": "application/javascript; charset=utf-8",
            ".css": "text/css; charset=utf-8",
        }
        with open(full, "rb") as fh:
            self._send(200, fh.read(), types.get(ext, "application/octet-stream"))

    def _send(self, status, payload, content_type):
        self.send_response(status)
        self.send_header("Content-Type", content_type)
        self.send_header("Access-Control-Allow-Origin", "*")
        self.send_header("Access-Control-Allow-Headers", "content-type")
        self.send_header("Access-Control-Allow-Methods", "GET,POST,OPTIONS")
        self.send_header("Content-Length", str(len(payload)))
        self.end_headers()
        if payload and self.command != "HEAD":
            self.wfile.write(payload)

    def log_message(self, fmt, *args):
        sys.stderr.write("%s - %s\n" % (self.address_string(), fmt % args))


def main():
    host = os.environ.get("HOST", "127.0.0.1")
    port = int(os.environ.get("PORT", "8787"))
    server = ThreadingHTTPServer((host, port), App)
    print("SchoolDay metrics  http://%s:%s" % (host, port))
    print("Dashboard          http://%s:%s/" % (host, port))
    print("POST /events   GET /summary   GET /health")
    server.serve_forever()


if __name__ == "__main__":
    main()
