#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Bridge client for the open Unity editor (MCP-for-Unity), auto-detects the bridge port."""

import json
import socket
import struct
import sys

HOST = "127.0.0.1"


class Bridge:
    def __init__(self, port, timeout=600.0):
        self.sock = socket.create_connection((HOST, port), timeout=timeout)
        line = b""
        while not line.endswith(b"\n"):
            ch = self.sock.recv(1)
            if not ch:
                raise RuntimeError("bridge closed during handshake")
            line += ch
        if not line.startswith(b"WELCOME UNITY-MCP"):
            raise RuntimeError("unexpected handshake: %r" % line)

    def call(self, command_type, params=None):
        payload = json.dumps({"type": command_type, "params": params or {}}).encode("utf-8")
        self.sock.sendall(struct.pack(">Q", len(payload)) + payload)
        header = b""
        while len(header) < 8:
            chunk = self.sock.recv(8 - len(header))
            if not chunk:
                raise RuntimeError("bridge closed")
            header += chunk
        size = struct.unpack(">Q", header)[0]
        body = b""
        while len(body) < size:
            chunk = self.sock.recv(size - len(body))
            if not chunk:
                raise RuntimeError("bridge closed mid-frame")
            body += chunk
        return json.loads(body.decode("utf-8", "replace"))

    def code(self, source, action="execute"):
        return self.call("execute_code", {"action": action, "code": source})

    def close(self):
        try:
            self.sock.close()
        except Exception:
            pass


def main():
    if len(sys.argv) < 2:
        sys.exit("usage: refsheet_mcp.py --code <file> [port] | <tool> [json-params] [port]")
    port = int(sys.argv[-1]) if sys.argv[-1].isdigit() else 6401
    bridge = Bridge(port)
    try:
        if sys.argv[1] == "--code":
            result = bridge.code(open(sys.argv[2], encoding="utf-8").read())
        else:
            params = json.loads(sys.argv[2]) if len(sys.argv) > 2 and not sys.argv[2].isdigit() else {}
            result = bridge.call(sys.argv[1], params)
        print(json.dumps(result, ensure_ascii=False, indent=2))
    finally:
        bridge.close()


if __name__ == "__main__":
    main()
