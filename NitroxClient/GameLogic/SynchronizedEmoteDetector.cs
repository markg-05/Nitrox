using System.Collections.Generic;
using Nitrox.Model.Core;
using UnityEngine;

namespace NitroxClient.GameLogic;

public readonly record struct SynchronizedEmoteMatch(SessionId FirstPlayer, SessionId SecondPlayer);

public sealed class SynchronizedEmoteDetector
{
    public const float SYNC_WINDOW_SECONDS = 1.25f;
    public const float MAX_DISTANCE_METERS = 15f;

    private readonly Dictionary<SessionId, EmoteEvent> recentEvents = [];
    private readonly List<SessionId> expiredSessions = [];

    public bool TryRegister(SessionId sessionId, PlayerEmoteGroup group, float timestamp, Vector3 position, out SynchronizedEmoteMatch match)
    {
        match = default;
        if (!IsFinite(timestamp) || !IsFinite(position))
        {
            return false;
        }

        RemoveExpired(timestamp);

        SessionId? matchingSession = null;
        float maximumDistanceSquared = MAX_DISTANCE_METERS * MAX_DISTANCE_METERS;
        foreach (KeyValuePair<SessionId, EmoteEvent> entry in recentEvents)
        {
            SessionId candidateSession = entry.Key;
            EmoteEvent candidate = entry.Value;
            if (candidateSession == sessionId || candidate.Group != group ||
                Mathf.Abs(timestamp - candidate.Timestamp) > SYNC_WINDOW_SECONDS ||
                (position - candidate.Position).sqrMagnitude > maximumDistanceSquared)
            {
                continue;
            }

            if (!matchingSession.HasValue || candidateSession.CompareTo(matchingSession.Value) < 0)
            {
                matchingSession = candidateSession;
            }
        }

        if (matchingSession.HasValue)
        {
            recentEvents.Remove(matchingSession.Value);
            recentEvents.Remove(sessionId);
            match = sessionId.CompareTo(matchingSession.Value) < 0
                ? new SynchronizedEmoteMatch(sessionId, matchingSession.Value)
                : new SynchronizedEmoteMatch(matchingSession.Value, sessionId);
            return true;
        }

        recentEvents[sessionId] = new EmoteEvent(group, timestamp, position);
        return false;
    }

    public void Clear() => recentEvents.Clear();

    private void RemoveExpired(float timestamp)
    {
        expiredSessions.Clear();
        foreach (KeyValuePair<SessionId, EmoteEvent> entry in recentEvents)
        {
            EmoteEvent recentEvent = entry.Value;
            if (Mathf.Abs(timestamp - recentEvent.Timestamp) > SYNC_WINDOW_SECONDS)
            {
                expiredSessions.Add(entry.Key);
            }
        }

        foreach (SessionId sessionId in expiredSessions)
        {
            recentEvents.Remove(sessionId);
        }
    }

    private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

    private static bool IsFinite(Vector3 value) => IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);

    private readonly record struct EmoteEvent(PlayerEmoteGroup Group, float Timestamp, Vector3 Position);
}
