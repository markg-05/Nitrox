using Nitrox.Model.Core;
using NitroxClient.GameLogic;
using UnityEngine;

namespace Nitrox.Test.Client.GameLogic;

[TestClass]
public sealed class SynchronizedEmoteDetectorTest
{
    [TestMethod]
    public void MatchesSameNearbyEmoteWithinWindow()
    {
        SynchronizedEmoteDetector detector = new();

        detector.TryRegister((SessionId)2, PlayerEmoteGroup.TeamUp, 10f, Vector3.zero, out _).Should().BeFalse();
        detector.TryRegister((SessionId)1, PlayerEmoteGroup.TeamUp, 11.25f, new Vector3(15f, 0f, 0f), out SynchronizedEmoteMatch match).Should().BeTrue();

        match.FirstPlayer.Should().Be((SessionId)1);
        match.SecondPlayer.Should().Be((SessionId)2);
    }

    [TestMethod]
    public void RejectsDifferentEmotesOrLateAndDistantMatches()
    {
        SynchronizedEmoteDetector detector = new();

        detector.TryRegister((SessionId)1, PlayerEmoteGroup.Yes, 0f, Vector3.zero, out _).Should().BeFalse();
        detector.TryRegister((SessionId)2, PlayerEmoteGroup.No, 0.1f, Vector3.zero, out _).Should().BeFalse();

        detector.Clear();
        detector.TryRegister((SessionId)1, PlayerEmoteGroup.Yes, 0f, Vector3.zero, out _).Should().BeFalse();
        detector.TryRegister((SessionId)2, PlayerEmoteGroup.Yes, SynchronizedEmoteDetector.SYNC_WINDOW_SECONDS + 0.01f, Vector3.zero, out _).Should().BeFalse();

        detector.Clear();
        detector.TryRegister((SessionId)1, PlayerEmoteGroup.Yes, 0f, Vector3.zero, out _).Should().BeFalse();
        detector.TryRegister((SessionId)2, PlayerEmoteGroup.Yes, 0.1f, new Vector3(SynchronizedEmoteDetector.MAX_DISTANCE_METERS + 0.01f, 0f, 0f), out _).Should().BeFalse();
    }

    [TestMethod]
    public void DoesNotMatchOnePlayerWithTheirOwnRepeatedEmote()
    {
        SynchronizedEmoteDetector detector = new();

        detector.TryRegister((SessionId)1, PlayerEmoteGroup.LetsGo, 3f, Vector3.zero, out _).Should().BeFalse();
        detector.TryRegister((SessionId)1, PlayerEmoteGroup.LetsGo, 3.1f, Vector3.zero, out _).Should().BeFalse();
    }

    [TestMethod]
    public void ConsumesMatchedEvents()
    {
        SynchronizedEmoteDetector detector = new();

        detector.TryRegister((SessionId)1, PlayerEmoteGroup.ShowOff, 4f, Vector3.zero, out _).Should().BeFalse();
        detector.TryRegister((SessionId)2, PlayerEmoteGroup.ShowOff, 4.1f, Vector3.zero, out _).Should().BeTrue();
        detector.TryRegister((SessionId)3, PlayerEmoteGroup.ShowOff, 4.2f, Vector3.zero, out _).Should().BeFalse();
    }
}
