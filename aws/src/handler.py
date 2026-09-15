import json
import os
from datetime import datetime, timezone

from store import make_store

_store = None

STALE_AFTER = 90


def get_store():
    global _store
    if _store is None:
        _store = make_store()
    return _store


def lambda_handler(event, context):
    method, path, body = parse(event)

    if method == "OPTIONS":
        return respond(204, "")

    if method == "POST" and path.rstrip("/") == "/events":
        return post_event(body)

    if method == "GET" and path.rstrip("/") == "/summary":
        return get_summary()

    if method == "GET" and path.rstrip("/") == "/health":
        return respond(200, {"ok": True, "table": os.environ.get("TABLE_NAME", "")})

    return respond(404, {"error": "not found"})


def parse(event):
    rc = event.get("requestContext") or {}
    http = rc.get("http") or {}
    method = (http.get("method") or event.get("httpMethod") or "GET").upper()
    path = event.get("rawPath") or event.get("path") or "/"
    body = event.get("body")
    if event.get("isBase64Encoded") and body:
        import base64

        body = base64.b64decode(body).decode("utf-8")
    return method, path, body


def post_event(body):
    try:
        payload = json.loads(body or "{}")
    except ValueError:
        return respond(400, {"error": "body must be JSON"})

    if not isinstance(payload, dict):
        return respond(400, {"error": "body must be an object"})

    session_id = str(payload.get("session_id") or "").strip()
    kind = str(payload.get("kind") or "").strip()
    if not session_id or not kind:
        return respond(400, {"error": "session_id and kind are required"})

    if not payload.get("ts"):
        payload["ts"] = datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")
    payload["session_id"] = session_id
    payload["kind"] = kind

    player_id = str(payload.get("player_id") or "").strip()
    if player_id:
        payload["player_id"] = player_id
    else:
        payload.pop("player_id", None)

    get_store().put(payload)
    out = {"ok": True, "session_id": session_id, "kind": kind}
    if player_id:
        out["player_id"] = player_id
    return respond(200, out)


def get_summary():
    events = get_store().list_all()
    return respond(200, summarize(events))


def summarize(events):
    by_player = {}
    for ev in events:
        key = player_key(ev)
        if not key:
            continue
        by_player.setdefault(key, []).append(ev)

    plays = []
    for pid, rows in by_player.items():
        view = player_view(pid, rows)
        if keep_play(view):
            plays.append(view)
    plays.sort(key=lambda row: row.get("last_ts") or "", reverse=True)

    ended = [play for play in plays if play.get("status") == "done"]
    n_end = len(ended)
    survived = sum(1 for play in ended if play.get("shock_survived"))
    pockets = [float(play.get("pocket") or 0) for play in plays]
    reps = [int(play.get("reputation") or 0) for play in plays]

    overspend_counts = {}
    for play in plays:
        for block in play_blocks(play):
            if (block.get("status") or "") not in ("done", "report"):
                continue
            label = (block.get("overspend") or "").strip()
            if label:
                overspend_counts[label] = overspend_counts.get(label, 0) + 1
    top_leak = pick_top_overspend(overspend_counts)

    paid_bills = class_badge_ids(plays)

    return {
        "sessions": len(plays),
        "ended": n_end,
        "shock_survival_pct": int(round(100.0 * survived / n_end)) if n_end else 0,
        "top_leak": top_leak,
        "avg_pocket": round(sum(pockets) / len(pockets), 2) if pockets else 0,
        "avg_reputation": int(round(sum(reps) / float(len(reps)))) if reps else 0,
        "leaks": overspend_counts,
        "unlocks": len(paid_bills),
        "recent_unlocks": [],
        "badge_list": [],
        "plays": plays,
    }


def player_key(ev):
    pid = str(ev.get("player_id") or "").strip()
    if pid:
        return pid
    return str(ev.get("session_id") or "").strip()


def player_view(pid, rows):
    sessions = player_sessions(rows)
    if not sessions:
        view = session_view("", [])
        view["player_id"] = pid or ""
        view["mornings"] = []
        view["morning_count"] = 0
        return view

    mornings = []
    for index, (sid, session_rows) in enumerate(sessions):
        block = session_view(sid, session_rows)
        block["player_id"] = pid or sid or ""
        block["overspend"] = overspend_from_rows(session_rows)
        block["badges"] = player_badges(session_rows)
        block["started_ts"] = morning_started(session_rows)
        block["morning"] = index + 1
        mornings.append(block)

    latest = mornings[-1]
    view = dict(latest)
    view["player_id"] = pid or latest.get("session_id") or ""
    view["mornings"] = list(reversed(mornings))
    view["morning_count"] = len(mornings)
    return view


KIND_RANK = {"session_start": 0, "choice": 1, "session_end": 2}


def player_sessions(rows):
    by_sid = {}
    for ev in rows:
        sid = str(ev.get("session_id") or "")
        by_sid.setdefault(sid, []).append(ev)
    if not by_sid:
        return []

    def rank(sid):
        start_ts = ""
        last_ts = ""
        for ev in by_sid[sid]:
            ts = ev.get("ts") or ""
            if ts >= last_ts:
                last_ts = ts
            if ev.get("kind") == "session_start" and ts >= start_ts:
                start_ts = ts
        return (start_ts or last_ts, last_ts, sid)

    ordered = []
    for sid in sorted(by_sid, key=rank):
        session_rows = sorted(
            by_sid[sid],
            key=lambda ev: (ev.get("ts") or "", KIND_RANK.get(ev.get("kind") or "", 1)),
        )
        ordered.append((sid, session_rows))
    return ordered


def latest_session(rows):
    sessions = player_sessions(rows)
    if not sessions:
        return "", []
    return sessions[-1]


def morning_started(rows):
    for ev in rows:
        if ev.get("kind") == "session_start" and ev.get("ts"):
            return ev.get("ts")
    if rows:
        return rows[0].get("ts") or ""
    return ""


def play_blocks(play):
    blocks = play.get("mornings") if play else None
    if blocks:
        return blocks
    return [play] if play else []


def class_badge_ids(plays):
    found = []
    seen = set()
    for play in plays or []:
        for block in play_blocks(play):
            for badge in block.get("badges") or []:
                badge_id = str(badge or "").strip()
                if not badge_id or badge_id in seen:
                    continue
                seen.add(badge_id)
                found.append(badge_id)
            if block.get("buffer_kid") and "buffer_kid" not in seen:
                seen.add("buffer_kid")
                found.append("buffer_kid")
    return found


def player_badges(latest_rows):
    badges = []
    seen = set()

    def add(badge_id):
        badge_id = str(badge_id or "").strip()
        if not badge_id or badge_id in seen:
            return
        seen.add(badge_id)
        badges.append(badge_id)

    for ev in latest_rows:
        if ev.get("kind") != "choice":
            continue
        if (ev.get("beat_id") or "") != "shock":
            continue
        if ev.get("declined"):
            continue
        try:
            cost = float(ev.get("cost") or 0)
        except (TypeError, ValueError):
            cost = 0
        if cost <= 0:
            continue
        add(ev.get("choice_id"))
        add(ev.get("achievement_id"))

    latest_end = None
    for ev in latest_rows:
        if ev.get("kind") == "session_end":
            latest_end = ev
    if latest_end and latest_end.get("shock_survived"):
        add(latest_end.get("achievement_id"))
    if latest_end and latest_end.get("buffer_kid"):
        add("buffer_kid")
    return badges


def keep_play(view):
    for block in play_blocks(view):
        if keep_block(block):
            return True
    return False


def keep_block(block):
    if not block:
        return False
    if block.get("expenses"):
        return True
    if (block.get("status") or "") in ("playing", "done", "report"):
        return True
    if block.get("badges"):
        return True
    if block.get("named_leak") or block.get("won") or block.get("buffer_kid"):
        return True
    return False


def session_view(sid, rows):
    rows = sorted(rows, key=lambda row: row.get("ts") or "")
    look = ""
    pocket = 0
    fuel = 0
    face = 0
    end = None
    expenses = []

    for ev in rows:
        kind = ev.get("kind")
        if ev.get("look_id"):
            look = ev.get("look_id")
        if ev.get("pocket") is not None and ev.get("pocket") != "":
            pocket = ev.get("pocket")
        if ev.get("fuel") is not None and ev.get("fuel") != "":
            fuel = ev.get("fuel")
        if ev.get("face") is not None and ev.get("face") != "":
            face = ev.get("face")
        if kind == "choice" and not is_title(ev):
            expenses.append(feed_row(ev))
        if kind == "session_end":
            end = ev

    last_ts = rows[-1].get("ts") if rows else ""
    status = "done"
    if end is None:
        status = "left" if stale(last_ts) else "playing"

    view = {
        "session_id": sid or "",
        "player_id": "",
        "look_id": look,
        "pocket": pocket,
        "fuel": fuel,
        "face": face,
        "playing": status == "playing",
        "status": status,
        "last_ts": last_ts or "",
        "expenses": expenses,
        "named_leak": "",
        "overspend": "",
        "won": False,
        "buffer_kid": False,
        "shock_survived": False,
        "went_broke": False,
        "achievement_id": "",
        "badges": [],
        "reputation": 0,
        "punctual": 0,
        "friendly": 0,
        "adaptive": 0,
        "generous": 0,
    }
    if end is None:
        view["overspend"] = overspend_from_rows(rows)
        apply_reputation(view, rows, end)
        return view

    view["named_leak"] = end.get("named_leak") or ""
    view["overspend"] = overspend_from_rows(rows)
    view["won"] = bool(end.get("won"))
    view["buffer_kid"] = bool(end.get("buffer_kid"))
    view["shock_survived"] = bool(end.get("shock_survived"))
    view["went_broke"] = bool(end.get("went_broke"))
    view["achievement_id"] = end.get("achievement_id") or ""
    if end.get("pocket") is not None and end.get("pocket") != "":
        view["pocket"] = end.get("pocket")
    if end.get("fuel") is not None and end.get("fuel") != "":
        view["fuel"] = end.get("fuel")
    if end.get("face") is not None and end.get("face") != "":
        view["face"] = end.get("face")
    apply_reputation(view, rows, end)
    return view


def apply_reputation(view, rows, end):
    source = end
    if source is None:
        for ev in reversed(rows or []):
            if ev.get("reputation") not in (None, ""):
                source = ev
                break
    posted = as_float(source.get("reputation"), None) if source else None
    if posted is not None and source is not view:
        view["reputation"] = int(max(0, min(100, round(posted))))
        view["punctual"] = as_float(source.get("punctual"), view.get("fuel") or 0)
        view["friendly"] = as_float(source.get("friendly"), view.get("face") or 0)
        view["adaptive"] = as_float(source.get("adaptive"), 0)
        view["generous"] = as_float(source.get("generous"), 50)
        return

    shock_reached = end is not None
    if not shock_reached:
        for ev in rows:
            if (ev.get("beat_id") or "") == "shock":
                shock_reached = True
                break

    face = as_float(view.get("face"))
    pocket = as_float(view.get("pocket"))
    friendly = max(0.0, min(100.0, face))
    if view.get("went_broke"):
        adaptive = 0.0
    elif pocket + 0.001 >= 2.0:
        adaptive = 100.0
    elif pocket > 0:
        adaptive = 100.0 * pocket / 2.0
    else:
        adaptive = 0.0
    over = (view.get("overspend") or "").strip().lower()
    if over in ("grab", "drinks"):
        adaptive *= 0.5
    if not shock_reached:
        generous = 50.0
    elif view.get("shock_survived"):
        generous = 100.0
    else:
        generous = 0.0
    debt = max(0.0, -pocket)
    debt_factor = max(0.0, min(1.0, 1.0 - debt / 8.0)) if debt > 0 else 1.0
    view["punctual"] = 0.0
    view["friendly"] = friendly
    view["adaptive"] = adaptive
    view["generous"] = generous
    view["reputation"] = int(max(0, min(100, round(friendly * debt_factor))))


def as_float(value, default=0.0):
    if value is None or value == "":
        return default
    try:
        return float(value)
    except (TypeError, ValueError):
        return default


def overspend_label(named_leak):
    raw = str(named_leak or "").strip()
    if not raw:
        return ""
    key = raw.lower()
    compact = key.replace(" ", "").replace("_", "").replace("-", "")
    if key == "none" or compact == "skipmeal" or ("skip" in key and "meal" in key):
        return ""
    return raw


def overspend_from_rows(rows):
    end_label = ""
    seen = []
    for ev in rows:
        label = overspend_label(ev.get("named_leak"))
        if not label:
            continue
        if ev.get("kind") == "session_end":
            end_label = label
        else:
            seen.append(label)
    if end_label:
        return end_label
    for label in seen:
        if label.lower() == "grab":
            return label
    return seen[0] if seen else ""


def pick_top_overspend(counts):
    if not counts:
        return "none"
    best = ""
    best_n = 0
    for name, n in counts.items():
        if n > best_n:
            best = name
            best_n = n
        elif n == best_n and name.lower() == "grab":
            best = name
    return best or "none"


def stale(ts):
    when = parse_ts(ts)
    if when is None:
        return True
    return (datetime.now(timezone.utc) - when).total_seconds() > STALE_AFTER


def parse_ts(ts):
    if not ts:
        return None
    text = str(ts).strip()
    if text.endswith("Z"):
        text = text[:-1] + "+00:00"
    try:
        when = datetime.fromisoformat(text)
    except ValueError:
        return None
    if when.tzinfo is None:
        when = when.replace(tzinfo=timezone.utc)
    return when


def is_title(ev):
    beat = ev.get("beat_id") or ""
    choice = ev.get("choice_id") or ""
    return beat == "title_monday" or choice == "title_start"


def feed_row(ev):
    named = ev.get("named_leak") or ""
    return {
        "ts": ev.get("ts") or "",
        "session_id": ev.get("session_id") or "",
        "player_id": ev.get("player_id") or "",
        "kind": ev.get("kind") or "",
        "beat_id": ev.get("beat_id") or "",
        "beat_name": ev.get("beat_name") or "",
        "choice_id": ev.get("choice_id") or "",
        "choice_name": ev.get("choice_name") or "",
        "declined": bool(ev.get("declined")),
        "cost": ev.get("cost") or 0,
        "pocket": ev.get("pocket") or 0,
        "named_leak": named,
        "overspend": overspend_label(named),
        "won": bool(ev.get("won")),
        "shock_survived": bool(ev.get("shock_survived")),
        "buffer_kid": bool(ev.get("buffer_kid")),
        "went_broke": bool(ev.get("went_broke")),
    }


def respond(status, body):
    if body == "":
        payload = ""
    else:
        payload = json.dumps(body, default=str)

    return {
        "statusCode": status,
        "headers": {
            "Content-Type": "application/json",
            "Access-Control-Allow-Origin": "*",
            "Access-Control-Allow-Headers": "content-type",
            "Access-Control-Allow-Methods": "GET,POST,OPTIONS",
        },
        "body": payload,
    }
