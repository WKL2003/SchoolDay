# School Day

Unity desktop game with an AWS metrics backend and a live dashboard. Interview assignment for MAGES Studio.

You play one school day on an $8 allowance: breakfast, transit, friends, then a surprise bill. The goal is to keep a $2 buffer without skipping meals or dropping out of the group. Next morning carries leftover cash or debt, energy, and reputation, then adds another $8.

## Play in Unity

1. Open this folder as a Unity project.
2. Open `Assets/Scenes/SchoolDay.unity`.
3. Press Play. Mouse only. About a minute.

Pick Boy or Girl, then play the day. Each run reshuffles choices from the pools. The end card has Next morning only (no reset). Badges stay on the student.

## What's in this repo

| Path | What it is |
| --- | --- |
| `Assets/Game/` | Gameplay, ScriptableObject data, UI |
| `Assets/Scenes/SchoolDay.unity` | Playable scene |
| `aws/` | Lambda handler, local server, deploy |
| `dashboard/` | Live educator page |

Numbers and copy live on assets under `Assets/Game/Data/`. Runtime scripts are in `Assets/Game/Runtime/`. The Canvas is ordinary uGUI in the scene, not built from code.

## Metrics

`DaySession` does not talk to the network. `MetricsClient` listens to `DayDirector` and POSTs JSON (`session_start`, `choice`, `session_end`).

Set the host on `Assets/Game/Data/MetricsConfig.asset` → **BaseUrl**. Path is `/events`.

| Mode | BaseUrl |
| --- | --- |
| Offline | empty |
| Local | `http://127.0.0.1:8787` |
| AWS | the API Gateway URL |

Local (no AWS keys):

```bash
python3 aws/local_server.py
```

Then Play in Unity. A confirm should show up on `http://127.0.0.1:8787/`.

AWS deploy (region `ap-southeast-1`, after `aws configure`):

```bash
./aws/deploy.sh
```

## Backend and dashboard

```
Unity POST /events  →  Lambda  →  DynamoDB
Dashboard GET /summary every 3s
```

| File | Job |
| --- | --- |
| `aws/src/handler.py` | `/events`, `/summary`, `/health` |
| `aws/src/store.py` | DynamoDB if `TABLE_NAME` is set, else `aws/data/events.json` |
| `aws/template.yaml` | table, Lambda, HTTP API, S3, CloudFront |
| `aws/deploy.sh` | package, deploy, upload dashboard |
| `dashboard/index.html` | tiles and live student list |
| `dashboard/config.js` | API host |

## Change content without new C#

- Price: edit **Cost** on the choice asset under `Assets/Game/Data/Choices/`.
- New character: new `CharacterLook`, duplicate a select card, assign it in the Inspector.
- New deal: tag `PromoGroup` on the choice and add a row on `PromoCatalog`.
