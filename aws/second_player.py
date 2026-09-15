#!/usr/bin/env python3
"""Post one extra Monday so Class gets a second row (Girl).

Uses a stable player_id of its own. Re-run updates the same Girl row.
Unity Play stays a different row.
"""

import json
import os
import sys
import time
import uuid
from datetime import datetime, timezone

try:
    from urllib.request import Request, urlopen
except ImportError:
    raise SystemExit("python3 required")

HERE = os.path.dirname(os.path.abspath(__file__))
PLAYER_FILE = os.path.join(HERE, "data", "second_player_id.txt")


def api_url():
    env = os.environ.get("API_URL", "").strip()
    if env:
        return env.rstrip("/")
    path = os.path.join(HERE, "outputs.json")
    with open(path, encoding="utf-8") as fh:
        return json.load(fh)["api_url"].rstrip("/")


def girl_player_id():
    if os.path.isfile(PLAYER_FILE):
        pid = open(PLAYER_FILE, encoding="utf-8").read().strip()
        if pid:
            return pid
    pid = uuid.uuid4().hex
    folder = os.path.dirname(PLAYER_FILE)
    if folder and not os.path.isdir(folder):
        os.makedirs(folder)
    with open(PLAYER_FILE, "w", encoding="utf-8") as fh:
        fh.write(pid + "\n")
    return pid


def now():
    return datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")


def post(api, body):
    body = dict(body)
    body["ts"] = now()
    req = Request(
        api + "/events",
        data=json.dumps(body).encode("utf-8"),
        headers={"Content-Type": "application/json"},
        method="POST",
    )
    with urlopen(req, timeout=8) as resp:
        print(resp.status, body["kind"], body.get("choice_id") or body.get("look_id") or "")


def main():
    api = api_url()
    sid = uuid.uuid4().hex
    pid = girl_player_id()
    print("API", api)
    print("player", pid)
    print("session", sid)
    print("Girl · nasi · bus · next payday · rice $5 (cannot pay)")
    print("Watch Class. Girl is a second row. Re-run keeps this player_id.")
    print()

    post(api, {
        "session_id": sid,
        "player_id": pid,
        "kind": "session_start",
        "look_id": "Girl",
        "pocket": 8,
        "fuel": 70,
        "face": 55,
        "buffer": 8,
    })
    time.sleep(2)
    post(api, {
        "session_id": sid,
        "player_id": pid,
        "kind": "choice",
        "beat_id": "breakfast",
        "beat_name": "Breakfast",
        "choice_id": "breakfast_nasi",
        "choice_name": "Canteen nasi lemak",
        "cost": 2.5,
        "pocket": 5.5,
        "fuel": 85,
        "face": 55,
        "buffer": 5.5,
    })
    time.sleep(2)
    post(api, {
        "session_id": sid,
        "player_id": pid,
        "kind": "choice",
        "beat_id": "transit",
        "beat_name": "Getting to school",
        "choice_id": "transit_bus",
        "choice_name": "Bus",
        "cost": 0.7,
        "pocket": 4.8,
        "fuel": 83,
        "face": 55,
        "buffer": 4.8,
    })
    time.sleep(2)
    post(api, {
        "session_id": sid,
        "player_id": pid,
        "kind": "choice",
        "beat_id": "fomo",
        "beat_name": "After school",
        "choice_id": "fomo_next",
        "choice_name": "Next payday",
        "cost": 0,
        "pocket": 4.8,
        "fuel": 83,
        "face": 37,
        "buffer": 4.8,
    })
    time.sleep(2)
    post(api, {
        "session_id": sid,
        "player_id": pid,
        "kind": "choice",
        "beat_id": "shock",
        "beat_name": "Surprise bill",
        "choice_id": "shock_rice",
        "choice_name": "Help buy rice",
        "cost": 0,
        "declined": True,
        "pocket": 4.8,
        "fuel": 83,
        "face": 45,
        "buffer": 4.8,
        "shock_survived": False,
    })
    time.sleep(1)
    post(api, {
        "session_id": sid,
        "player_id": pid,
        "kind": "session_end",
        "look_id": "Girl",
        "pocket": 4.8,
        "fuel": 83,
        "face": 45,
        "buffer": 4.8,
        "named_leak": "",
        "shock_survived": False,
        "won": True,
        "buffer_kid": True,
        "day_ended": True,
    })
    print()
    print("Done. Girl should be one row. Avg cash left uses this $4.80 too.")


if __name__ == "__main__":
    try:
        main()
    except Exception as exc:
        sys.stderr.write(str(exc) + "\n")
        sys.exit(1)
