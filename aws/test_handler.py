#!/usr/bin/env python3
"""Local checks for summarize: identity, overspend filter, badges on the play."""

import os
import sys
import unittest
from datetime import datetime, timedelta, timezone

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "src"))
import handler  # noqa: E402


def ev(session_id, kind, ts, **extra):
    row = {"session_id": session_id, "kind": kind, "ts": ts}
    row.update(extra)
    return row


class SummarizeTests(unittest.TestCase):
    def test_skip_meal_is_not_overspend(self):
        events = [
            ev("s1", "session_start", "2026-09-07T10:00:00Z", player_id="p1", look_id="Boy", pocket=8),
            ev("s1", "choice", "2026-09-07T10:00:05Z", player_id="p1", beat_id="breakfast",
               beat_name="Breakfast", choice_id="breakfast_skip", choice_name="Skip breakfast",
               cost=0, pocket=8, named_leak="skip meal"),
            ev("s1", "session_end", "2026-09-07T10:01:00Z", player_id="p1", look_id="Boy",
               pocket=2.3, named_leak="skip meal", won=True, buffer_kid=True, shock_survived=False),
        ]
        out = handler.summarize(events)
        self.assertEqual(out["sessions"], 1)
        play = out["plays"][0]
        self.assertEqual(play["overspend"], "")
        self.assertEqual(play["named_leak"], "skip meal")
        self.assertEqual(out["top_leak"], "none")

    def test_grab_still_grab(self):
        events = [
            ev("s1", "session_start", "2026-09-07T10:00:00Z", player_id="p1", look_id="Boy", pocket=8),
            ev("s1", "choice", "2026-09-07T10:00:10Z", player_id="p1", beat_id="transit",
               choice_id="transit_grab", choice_name="Grab", cost=9, pocket=-1, named_leak="Grab"),
            ev("s1", "session_end", "2026-09-07T10:01:00Z", player_id="p1",
               pocket=-1, named_leak="Grab", won=False, buffer_kid=False),
        ]
        play = handler.summarize(events)["plays"][0]
        self.assertEqual(play["overspend"], "Grab")

    def test_drinks_still_drinks(self):
        events = [
            ev("s1", "session_start", "2026-09-07T10:00:00Z", player_id="p1", look_id="Girl", pocket=8),
            ev("s1", "choice", "2026-09-07T10:00:10Z", player_id="p1", beat_id="fomo",
               choice_id="fomo_gongcha", named_leak="drinks", cost=4.8, pocket=3.2),
            ev("s1", "session_end", "2026-09-07T10:01:00Z", player_id="p1",
               pocket=3.2, named_leak="drinks", won=True, buffer_kid=True),
        ]
        out = handler.summarize(events)
        self.assertEqual(out["plays"][0]["overspend"], "drinks")
        self.assertEqual(out["top_leak"], "drinks")

    def test_skip_plus_grab_is_grab(self):
        events = [
            ev("s1", "session_start", "2026-09-07T10:00:00Z", player_id="p1", look_id="Boy", pocket=8),
            ev("s1", "choice", "2026-09-07T10:00:05Z", player_id="p1", beat_id="breakfast",
               named_leak="skip meal", cost=0, pocket=8),
            ev("s1", "choice", "2026-09-07T10:00:10Z", player_id="p1", beat_id="transit",
               named_leak="Grab", cost=9, pocket=-1),
            ev("s1", "session_end", "2026-09-07T10:01:00Z", player_id="p1",
               named_leak="Grab", pocket=-1),
        ]
        self.assertEqual(handler.summarize(events)["plays"][0]["overspend"], "Grab")

    def test_one_row_keeps_every_morning_with_time(self):
        events = [
            ev("m1", "session_start", "2026-09-07T10:00:00Z", player_id="p1", look_id="Girl", pocket=8),
            ev("m1", "choice", "2026-09-07T10:00:05Z", player_id="p1", beat_id="breakfast",
               choice_id="breakfast_skip", choice_name="Skip breakfast", named_leak="skip meal", pocket=8),
            ev("m1", "session_end", "2026-09-07T10:01:00Z", player_id="p1", look_id="Girl",
               pocket=2, named_leak="skip meal", achievement_id="shock_photo",
               shock_survived=True, buffer_kid=True),
            ev("m2", "session_start", "2026-09-07T10:02:00Z", player_id="p1", look_id="Boy", pocket=10),
            ev("m2", "choice", "2026-09-07T10:02:05Z", player_id="p1", beat_id="breakfast",
               choice_id="breakfast_nasi", choice_name="Canteen nasi lemak", pocket=7.5),
            ev("m2", "session_end", "2026-09-07T10:03:00Z", player_id="p1", look_id="Boy",
               pocket=3.8, named_leak="", shock_survived=False, buffer_kid=True, won=True),
        ]
        out = handler.summarize(events)
        self.assertEqual(out["sessions"], 1)
        play = out["plays"][0]
        self.assertEqual(play["player_id"], "p1")
        self.assertEqual(play["session_id"], "m2")
        self.assertEqual(play["look_id"], "Boy")
        self.assertEqual(play["status"], "done")
        self.assertEqual(play["expenses"][0]["choice_id"], "breakfast_nasi")
        self.assertEqual(play["pocket"], 3.8)
        self.assertEqual(play["morning_count"], 2)
        self.assertEqual(len(play["mornings"]), 2)
        self.assertEqual(play["mornings"][0]["session_id"], "m2")
        self.assertEqual(play["mornings"][0]["started_ts"], "2026-09-07T10:02:00Z")
        self.assertEqual(play["mornings"][1]["session_id"], "m1")
        self.assertEqual(play["mornings"][1]["expenses"][0]["choice_id"], "breakfast_skip")
        self.assertEqual(play["mornings"][1]["badges"], ["shock_photo", "buffer_kid"])
        self.assertEqual(play["mornings"][0]["badges"], ["buffer_kid"])

    def test_girl_is_second_row(self):
        events = [
            ev("s1", "session_start", "2026-09-07T10:00:00Z", player_id="boy", look_id="Boy", pocket=8),
            ev("s1", "choice", "2026-09-07T10:00:05Z", player_id="boy", beat_id="breakfast", pocket=5.5),
            ev("s1", "session_end", "2026-09-07T10:01:00Z", player_id="boy", look_id="Boy", pocket=5.5),
            ev("s2", "session_start", "2026-09-07T10:00:30Z", player_id="girl", look_id="Girl", pocket=8),
            ev("s2", "choice", "2026-09-07T10:00:35Z", player_id="girl", beat_id="breakfast", pocket=5.5),
            ev("s2", "session_end", "2026-09-07T10:01:30Z", player_id="girl", look_id="Girl",
               pocket=4.8, buffer_kid=True, won=True),
        ]
        out = handler.summarize(events)
        looks = sorted(p["look_id"] for p in out["plays"])
        self.assertEqual(looks, ["Boy", "Girl"])
        self.assertEqual(out["sessions"], 2)

    def test_legacy_session_without_player_id_stays_its_own_row(self):
        events = [
            ev("aaa", "session_start", "2026-09-07T10:00:00Z", look_id="Boy", pocket=8),
            ev("aaa", "choice", "2026-09-07T10:00:05Z", beat_id="breakfast", pocket=5),
            ev("aaa", "session_end", "2026-09-07T10:01:00Z", look_id="Boy", pocket=5),
            ev("bbb", "session_start", "2026-09-07T10:02:00Z", look_id="Boy", pocket=8),
            ev("bbb", "choice", "2026-09-07T10:02:05Z", beat_id="breakfast", pocket=6),
            ev("bbb", "session_end", "2026-09-07T10:03:00Z", look_id="Boy", pocket=6),
        ]
        out = handler.summarize(events)
        self.assertEqual(out["sessions"], 2)

    def test_class_badges_are_distinct_paid_bills(self):
        events = [
            ev("s1", "session_end", "2026-09-07T10:01:00Z", player_id="p1", look_id="Boy",
               pocket=1.5, achievement_id="shock_photo", shock_survived=True, buffer_kid=False),
            ev("s1", "choice", "2026-09-07T10:00:50Z", player_id="p1", beat_id="shock",
               choice_id="shock_photo", declined=False, pocket=1.5),
            ev("s2", "session_end", "2026-09-07T10:02:00Z", player_id="p2", look_id="Girl",
               pocket=4, buffer_kid=True, won=True),
        ]
        out = handler.summarize(events)
        self.assertEqual(out["unlocks"], 2)
        self.assertEqual(out["recent_unlocks"], [])
        self.assertEqual(out["badge_list"], [])
        boy = [p for p in out["plays"] if p["player_id"] == "p1"][0]
        girl = [p for p in out["plays"] if p["player_id"] == "p2"][0]
        self.assertEqual(boy["badges"], ["shock_photo"])
        self.assertEqual(girl["badges"], ["buffer_kid"])

    def test_playing_status_on_live_session(self):
        events = [
            ev("s1", "session_start", "2099-01-01T00:00:00Z", player_id="p1", look_id="Boy", pocket=8),
            ev("s1", "choice", "2099-01-01T00:00:05Z", player_id="p1", beat_id="breakfast",
               choice_id="breakfast_nasi", pocket=5.5),
        ]
        play = handler.summarize(events)["plays"][0]
        self.assertEqual(play["status"], "playing")
        self.assertTrue(play["playing"])
        self.assertEqual(len(play["expenses"]), 1)

    def test_latest_session_picks_newest_morning(self):
        events = [
            ev("old", "session_start", "2099-01-01T10:00:00Z", player_id="p1", look_id="Boy"),
            ev("old", "choice", "2099-01-01T10:00:05Z", player_id="p1", beat_id="breakfast",
               choice_id="breakfast_skip", pocket=8),
            ev("old", "session_end", "2099-01-01T10:01:00Z", player_id="p1", pocket=8),
            ev("new", "session_start", "2099-01-01T10:02:00Z", player_id="p1", look_id="Boy"),
            ev("new", "choice", "2099-01-01T10:02:05Z", player_id="p1", beat_id="breakfast",
               choice_id="breakfast_nasi", pocket=5.5),
        ]
        play = handler.summarize(events)["plays"][0]
        self.assertEqual(play["session_id"], "new")
        self.assertEqual(play["status"], "playing")
        self.assertEqual(play["expenses"][0]["choice_id"], "breakfast_nasi")

    def test_skipped_bill_does_not_keep_yesterdays_badge(self):
        events = [
            ev("m1", "session_end", "2026-09-07T10:01:00Z", player_id="p1", look_id="Girl",
               pocket=3, achievement_id="shock_phone", shock_survived=True, buffer_kid=True),
            ev("m1", "choice", "2026-09-07T10:00:50Z", player_id="p1", beat_id="shock",
               choice_id="shock_phone", declined=False, cost=4, pocket=3),
            ev("m2", "session_start", "2026-09-07T10:02:00Z", player_id="p1", look_id="Girl", pocket=8),
            ev("m2", "choice", "2026-09-07T10:02:40Z", player_id="p1", beat_id="shock",
               beat_name="Surprise bill", choice_id="shock_peshoes", choice_name="PE shoes",
               declined=True, cost=0, pocket=4.8),
            ev("m2", "session_end", "2026-09-07T10:03:00Z", player_id="p1", look_id="Girl",
               pocket=4.8, shock_survived=False, buffer_kid=True, won=True),
        ]
        out = handler.summarize(events)
        play = out["plays"][0]
        self.assertEqual(play["session_id"], "m2")
        self.assertEqual(play["badges"], ["buffer_kid"])
        self.assertTrue(play["buffer_kid"])
        self.assertEqual(out["unlocks"], 2)
        yesterday = [block for block in play["mornings"] if block["session_id"] == "m1"][0]
        self.assertIn("shock_phone", yesterday["badges"])
        skipped = [block for block in play["mornings"] if block["session_id"] == "m2"][0]
        self.assertTrue(skipped["expenses"][0]["declined"])
        self.assertEqual(skipped["expenses"][0]["choice_name"], "PE shoes")

    def test_paid_bill_this_morning_is_the_badge(self):
        events = [
            ev("s1", "choice", "2026-09-07T10:00:50Z", player_id="p1", beat_id="shock",
               choice_id="shock_peshoes", choice_name="PE shoes", declined=False, cost=8, pocket=0),
            ev("s1", "session_end", "2026-09-07T10:01:00Z", player_id="p1", look_id="Boy",
               pocket=0, achievement_id="shock_peshoes", shock_survived=True, buffer_kid=False),
        ]
        play = handler.summarize(events)["plays"][0]
        self.assertEqual(play["badges"], ["shock_peshoes"])
        self.assertNotIn("buffer_kid", play["badges"])

    def test_same_bill_across_players_counts_once(self):
        events = [
            ev("s1", "session_end", "2026-09-07T10:01:00Z", player_id="p1", look_id="Boy",
               pocket=3, achievement_id="shock_photo", shock_survived=True),
            ev("s1", "choice", "2026-09-07T10:00:50Z", player_id="p1", beat_id="shock",
               choice_id="shock_photo", declined=False, pocket=3),
            ev("s2", "session_end", "2026-09-07T10:02:00Z", player_id="p2", look_id="Girl",
               pocket=3, achievement_id="shock_photo", shock_survived=True),
            ev("s2", "choice", "2026-09-07T10:01:50Z", player_id="p2", beat_id="shock",
               choice_id="shock_photo", declined=False, pocket=3),
        ]
        out = handler.summarize(events)
        self.assertEqual(out["unlocks"], 1)

    def test_skip_meal_ignored_in_class_tile_when_drinks_exist(self):
        events = [
            ev("s1", "session_end", "2026-09-07T10:01:00Z", player_id="p1", named_leak="skip meal",
               look_id="Boy", pocket=2, buffer_kid=True),
            ev("s1", "choice", "2026-09-07T10:00:05Z", player_id="p1", beat_id="breakfast",
               named_leak="skip meal", pocket=8),
            ev("s2", "session_end", "2026-09-07T10:02:00Z", player_id="p2", named_leak="drinks",
               look_id="Girl", pocket=3.2, buffer_kid=True),
            ev("s2", "choice", "2026-09-07T10:00:05Z", player_id="p2", beat_id="fomo",
               named_leak="drinks", pocket=3.2),
        ]
        out = handler.summarize(events)
        self.assertEqual(out["top_leak"], "drinks")
        self.assertNotIn("skip meal", out["leaks"])


    def test_thumbdrive_still_counts_on_next_morning(self):
        events = [
            ev("m1", "session_start", "2026-09-08T08:00:00Z", player_id="p1", look_id="Boy", pocket=8),
            ev("m1", "choice", "2026-09-08T08:00:50Z", player_id="p1", beat_id="shock",
               choice_id="shock_thumbdrive", choice_name="Thumb drive", declined=False, cost=8, pocket=0),
            ev("m1", "session_end", "2026-09-08T08:01:00Z", player_id="p1", look_id="Boy",
               pocket=0, achievement_id="shock_thumbdrive", shock_survived=True, buffer_kid=False),
            ev("m2", "session_start", "2099-01-01T00:00:00Z", player_id="p1", look_id="Boy", pocket=8),
        ]
        out = handler.summarize(events)
        self.assertEqual(out["sessions"], 1)
        play = out["plays"][0]
        self.assertEqual(play["status"], "playing")
        self.assertEqual(play["badges"], [])
        self.assertEqual(out["unlocks"], 1)
        yesterday = [block for block in play["mornings"] if block["session_id"] == "m1"][0]
        self.assertEqual(yesterday["badges"], ["shock_thumbdrive"])

    def test_stale_next_morning_keeps_yesterdays_thumbdrive(self):
        stale_start = (datetime.now(timezone.utc) - timedelta(minutes=5)).strftime("%Y-%m-%dT%H:%M:%SZ")
        events = [
            ev("m1", "session_start", "2026-09-07T08:00:00Z", player_id="p1", look_id="Boy", pocket=8),
            ev("m1", "choice", "2026-09-07T08:00:50Z", player_id="p1", beat_id="shock",
               choice_id="shock_thumbdrive", choice_name="Thumb drive", declined=False, cost=8, pocket=0),
            ev("m1", "session_end", "2026-09-07T08:01:00Z", player_id="p1", look_id="Boy",
               pocket=0, achievement_id="shock_thumbdrive", shock_survived=True, buffer_kid=False),
            ev("m2", "session_start", stale_start, player_id="p1", look_id="Boy", pocket=8),
        ]
        out = handler.summarize(events)
        self.assertEqual(out["sessions"], 1)
        play = out["plays"][0]
        self.assertEqual(play["status"], "left")
        self.assertEqual(out["unlocks"], 1)
        yesterday = [block for block in play["mornings"] if block["session_id"] == "m1"][0]
        self.assertEqual(yesterday["badges"], ["shock_thumbdrive"])

    def test_reputation_at_start_is_face(self):
        events = [
            ev("s1", "session_start", "2099-01-01T00:00:00Z", player_id="p1", look_id="Girl",
               pocket=8, fuel=70, face=55),
        ]
        play = handler.summarize(events)["plays"][0]
        self.assertEqual(play["punctual"], 0)
        self.assertEqual(play["friendly"], 55)
        self.assertEqual(play["reputation"], 55)

    def test_reputation_from_behaviour(self):
        events = [
            ev("s1", "session_start", "2026-09-07T10:00:00Z", player_id="p1", look_id="Girl",
               pocket=8, fuel=70, face=55),
            ev("s1", "choice", "2026-09-07T10:00:40Z", player_id="p1", beat_id="shock",
               choice_id="shock_rice", declined=True, cost=0, pocket=4.8, fuel=83, face=37),
            ev("s1", "session_end", "2026-09-07T10:01:00Z", player_id="p1", look_id="Girl",
               pocket=4.8, fuel=83, face=37, shock_survived=False, buffer_kid=True, won=True),
        ]
        play = handler.summarize(events)["plays"][0]
        self.assertEqual(play["punctual"], 0)
        self.assertEqual(play["friendly"], 37)
        self.assertEqual(play["adaptive"], 100)
        self.assertEqual(play["generous"], 0)
        self.assertEqual(play["reputation"], 37)

    def test_reputation_paid_bill_is_generous(self):
        events = [
            ev("s1", "choice", "2026-09-07T10:00:40Z", player_id="p1", beat_id="shock",
               choice_id="shock_photo", declined=False, cost=5, pocket=3, fuel=80, face=60),
            ev("s1", "session_end", "2026-09-07T10:01:00Z", player_id="p1",
               pocket=3, fuel=80, face=60, shock_survived=True, buffer_kid=True),
        ]
        play = handler.summarize(events)["plays"][0]
        self.assertEqual(play["punctual"], 0)
        self.assertEqual(play["generous"], 100)
        self.assertEqual(play["reputation"], 60)

    def test_posted_reputation_json_is_accepted(self):
        events = [
            ev("s1", "session_start", "2026-09-07T10:00:00Z", player_id="p1", look_id="Girl",
               pocket=8, fuel=70, face=55, punctual=0, friendly=55, adaptive=100,
               generous=50, reputation=55),
            ev("s1", "session_end", "2026-09-07T10:01:00Z", player_id="p1", look_id="Girl",
               pocket=8, fuel=70, face=55, punctual=0, friendly=55, adaptive=100,
               generous=50, reputation=55, won=True, buffer_kid=True),
        ]
        play = handler.summarize(events)["plays"][0]
        self.assertEqual(play["reputation"], 55)
        self.assertEqual(play["friendly"], 55)
        self.assertEqual(play["punctual"], 0)
        self.assertEqual(play["generous"], 50)


if __name__ == "__main__":
    unittest.main()
