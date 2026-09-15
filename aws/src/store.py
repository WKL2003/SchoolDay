import json
import os
import threading
from decimal import Decimal


def make_store():
    table = os.environ.get("TABLE_NAME", "").strip()
    if table:
        return DynamoStore(table)

    path = os.environ.get("EVENT_FILE", "").strip()
    if not path:
        here = os.path.dirname(os.path.abspath(__file__))
        path = os.path.join(os.path.dirname(here), "data", "events.json")
    return FileStore(path)


class FileStore:
    def __init__(self, path):
        self.path = path
        self.lock = threading.Lock()
        folder = os.path.dirname(path)
        if folder and not os.path.isdir(folder):
            os.makedirs(folder)

    def put(self, event):
        with self.lock:
            rows = self._read()
            rows.append(event)
            tmp = self.path + ".tmp"
            with open(tmp, "w", encoding="utf-8") as fh:
                json.dump(rows, fh, indent=2)
            os.replace(tmp, self.path)

    def list_all(self):
        with self.lock:
            return list(self._read())

    def _read(self):
        if not os.path.isfile(self.path):
            return []
        with open(self.path, "r", encoding="utf-8") as fh:
            data = json.load(fh)
        if isinstance(data, list):
            return data
        return []


class DynamoStore:
    def __init__(self, table_name):
        import boto3

        self.table = boto3.resource("dynamodb").Table(table_name)

    def put(self, event):
        self.table.put_item(Item=_to_item(event))

    def list_all(self):
        items = []
        kwargs = {}
        while True:
            resp = self.table.scan(**kwargs)
            items.extend(resp.get("Items") or [])
            last = resp.get("LastEvaluatedKey")
            if not last or len(items) >= 2000:
                break
            kwargs["ExclusiveStartKey"] = last
        return [_from_item(item) for item in items]


def _to_item(event):
    session_id = event.get("session_id") or ""
    ts = event.get("ts") or ""
    kind = event.get("kind") or "choice"
    beat = str(event.get("beat_id") or "")
    choice = str(event.get("choice_id") or "")
    sk = ts + "#" + kind
    if beat or choice:
        sk = sk + "#" + beat + "#" + choice
    item = {
        "pk": session_id,
        "sk": sk,
        "session_id": session_id,
        "kind": kind,
        "ts": ts,
    }
    for key in (
        "player_id",
        "look_id",
        "beat_id",
        "beat_name",
        "choice_id",
        "choice_name",
        "named_leak",
        "achievement_id",
        "weekday",
    ):
        value = event.get(key)
        if value:
            item[key] = str(value)

    for key in (
        "cost",
        "pocket",
        "fuel",
        "face",
        "buffer",
        "duration",
        "reputation",
        "punctual",
        "friendly",
        "adaptive",
        "generous",
    ):
        if key in event and event[key] is not None and event[key] != "":
            item[key] = Decimal(str(event[key]))

    for key in (
        "went_broke",
        "declined",
        "shock_survived",
        "day_ended",
        "won",
        "buffer_kid",
    ):
        if key in event:
            item[key] = bool(event[key])

    return item


def _from_item(item):
    out = {}
    for key, value in item.items():
        if key in ("pk", "sk"):
            continue
        out[key] = _plain(value)
    return out


def _plain(value):
    if isinstance(value, Decimal):
        as_int = int(value)
        if value == as_int:
            return as_int
        return float(value)
    return value
