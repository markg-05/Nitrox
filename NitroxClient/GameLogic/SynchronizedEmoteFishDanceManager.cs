using System.Collections;
using System.Collections.Generic;
using Nitrox.Model.Core;
using Nitrox.Model.DataStructures;
using NitroxClient.GameLogic.Spawning.WorldEntities;
using NitroxClient.MonoBehaviours;
using UnityEngine;
using UWE;
using Object = UnityEngine.Object;

namespace NitroxClient.GameLogic;

internal sealed class SynchronizedEmoteFishDanceManager
{
    private const int FISH_COUNT = 12;
    private const float COOLDOWN_SECONDS = 12f;

    private static readonly TechType[] fishTypes = [TechType.Peeper, TechType.Boomerang, TechType.Hoopfish, TechType.GarryFish];

    private readonly LocalPlayer localPlayer;
    private readonly PlayerManager playerManager;
    private readonly SeamothPassengers seamothPassengers;
    private readonly SynchronizedEmoteDetector detector = new();

    private GameObject activeDance;
    private float nextAllowedTime;

    public SynchronizedEmoteFishDanceManager(LocalPlayer localPlayer, PlayerManager playerManager, SeamothPassengers seamothPassengers)
    {
        this.localPlayer = localPlayer;
        this.playerManager = playerManager;
        this.seamothPassengers = seamothPassengers;
    }

    public void Register(SessionId sessionId, PlayerEmoteGroup group, bool isInsideVehicle)
    {
        float now = Time.unscaledTime;
        if (!Multiplayer.Active || isInsideVehicle || activeDance || now < nextAllowedTime ||
            !TryGetEligibleParticipant(sessionId, out Transform participant))
        {
            return;
        }

        if (!detector.TryRegister(sessionId, group, now, participant.position, out SynchronizedEmoteMatch match) ||
            !TryGetEligibleParticipant(match.FirstPlayer, out _) ||
            !TryGetEligibleParticipant(match.SecondPlayer, out _))
        {
            return;
        }

        nextAllowedTime = now + COOLDOWN_SECONDS;
        CoroutineHost.StartCoroutine(SpawnDance(match));
    }

    private IEnumerator SpawnDance(SynchronizedEmoteMatch match)
    {
        List<GameObject> fishPrefabs = [];
        foreach (TechType fishType in fishTypes)
        {
            TaskResult<GameObject> result = new();
            yield return DefaultWorldEntitySpawner.RequestPrefab(fishType, result);
            if (result.value)
            {
                fishPrefabs.Add(result.value);
            }
        }

        if (!Multiplayer.Active || fishPrefabs.Count == 0 ||
            !TryGetEligibleParticipant(match.FirstPlayer, out Transform firstParticipant) ||
            !TryGetEligibleParticipant(match.SecondPlayer, out Transform secondParticipant))
        {
            yield break;
        }

        GameObject danceRoot = new("Nitrox Synchronized Emote Fish Dance");
        danceRoot.SetActive(false);
        danceRoot.transform.position = (firstParticipant.position + secondParticipant.position) * 0.5f;

        Transform[] fish = new Transform[FISH_COUNT];
        for (int i = 0; i < FISH_COUNT; i++)
        {
            GameObject fishVisual = Object.Instantiate(fishPrefabs[i % fishPrefabs.Count], danceRoot.transform, false);
            fishVisual.name = $"Dance Fish {i + 1}";
            MakeCosmeticOnly(fishVisual);
            fishVisual.SetActive(true);
            fish[i] = fishVisual.transform;
        }

        SynchronizedFishDance dance = danceRoot.AddComponent<SynchronizedFishDance>();
        dance.Configure(fish, firstParticipant, secondParticipant);
        activeDance = danceRoot;
        danceRoot.SetActive(true);
    }

    private bool TryGetEligibleParticipant(SessionId sessionId, out Transform participant)
    {
        participant = null;
        if (localPlayer.SessionId.HasValue && localPlayer.SessionId.Value == sessionId)
        {
            Player player = Player.main;
            if (!player || !player.IsUnderwaterForSwimming() || player.currentSub || player.currentEscapePod ||
                player.currentMountedVehicle || player.inSeamoth || player.mode == Player.Mode.Piloting ||
                seamothPassengers.IsPassenger)
            {
                return false;
            }

            participant = player.transform;
            return true;
        }

        if (!playerManager.TryFind(sessionId, out RemotePlayer remotePlayer) || !remotePlayer.Body ||
            remotePlayer.AnimationController == null || !remotePlayer.AnimationController["is_underwater"] ||
            remotePlayer.SubRoot || remotePlayer.EscapePod || remotePlayer.Vehicle || remotePlayer.PassengerSeamoth ||
            remotePlayer.PilotingChair || remotePlayer.PlayerContext.DrivingVehicle != null ||
            remotePlayer.PlayerContext.PassengerSeamoth != null)
        {
            return false;
        }

        participant = remotePlayer.Body.transform;
        return true;
    }

    private static void MakeCosmeticOnly(GameObject fish)
    {
        foreach (MonoBehaviour behaviour in fish.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (behaviour is not SkyApplier)
            {
                Object.DestroyImmediate(behaviour);
            }
        }

        foreach (Collider collider in fish.GetComponentsInChildren<Collider>(true))
        {
            Object.DestroyImmediate(collider);
        }

        foreach (Rigidbody rigidbody in fish.GetComponentsInChildren<Rigidbody>(true))
        {
            Object.DestroyImmediate(rigidbody);
        }
    }
}

internal sealed class SynchronizedFishDance : MonoBehaviour
{
    private const float DURATION_SECONDS = 7f;
    private const float ENTRANCE_SECONDS = 0.45f;
    private const float EXIT_SECONDS = 0.8f;
    private const float ORBIT_SPEED = 1.65f;
    private const float BEATS_PER_SECOND = 2f;

    private Transform[] fish;
    private Vector3[] originalScales;
    private Transform firstParticipant;
    private Transform secondParticipant;
    private float elapsed;

    public void Configure(Transform[] fish, Transform firstParticipant, Transform secondParticipant)
    {
        this.fish = fish;
        this.firstParticipant = firstParticipant;
        this.secondParticipant = secondParticipant;
        originalScales = new Vector3[fish.Length];
        for (int i = 0; i < fish.Length; i++)
        {
            originalScales[i] = fish[i].localScale;
        }
    }

    private void OnEnable()
    {
        Multiplayer.OnAfterMultiplayerEnd += Stop;
    }

    private void OnDestroy()
    {
        Multiplayer.OnAfterMultiplayerEnd -= Stop;
    }

    private void Update()
    {
        elapsed += Time.unscaledDeltaTime;
        if (elapsed >= DURATION_SECONDS)
        {
            Stop();
            return;
        }

        if (firstParticipant && secondParticipant)
        {
            Vector3 targetCenter = (firstParticipant.position + secondParticipant.position) * 0.5f;
            float followAmount = 1f - Mathf.Exp(-6f * Time.unscaledDeltaTime);
            transform.position = Vector3.Lerp(transform.position, targetCenter, followAmount);
        }

        float entrance = Mathf.Clamp01(elapsed / ENTRANCE_SECONDS);
        float exit = Mathf.Clamp01((DURATION_SECONDS - elapsed) / EXIT_SECONDS);
        float envelope = Mathf.SmoothStep(0f, 1f, Mathf.Min(entrance, exit));
        float beat = Mathf.Sin(elapsed * Mathf.PI * 2f * BEATS_PER_SECOND);
        int fishPerRing = Mathf.Max(1, fish.Length / 2);

        for (int i = 0; i < fish.Length; i++)
        {
            int ring = i / fishPerRing;
            int ringIndex = i % fishPerRing;
            float direction = ring == 0 ? 1f : -1f;
            float phase = ringIndex * Mathf.PI * 2f / fishPerRing + ring * Mathf.PI / fishPerRing;
            float angle = phase + direction * elapsed * ORBIT_SPEED;
            float radius = (ring == 0 ? 2.1f : 3.15f) + beat * 0.2f;
            float height = (ring == 0 ? 0.65f : -0.65f) + beat * 0.42f;

            fish[i].localPosition = new Vector3(Mathf.Cos(angle) * radius, height, Mathf.Sin(angle) * radius);
            Vector3 tangent = new(-Mathf.Sin(angle) * direction, beat * 0.08f, Mathf.Cos(angle) * direction);
            fish[i].localRotation = Quaternion.LookRotation(tangent, Vector3.up);
            fish[i].localScale = originalScales[i] * envelope * (1f + beat * 0.08f);
        }
    }

    private void Stop()
    {
        if (gameObject)
        {
            Destroy(gameObject);
        }
    }
}
