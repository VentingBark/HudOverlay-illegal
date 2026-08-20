using System;
using System.Collections.Generic;
using System.Linq;
using PlayFab;
using PlayFab.ClientModels;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace HudOverlay.Hud
{
    public class RigUtilities
    {
        public static VRRig GetVRRigFromPlayer(NetPlayer p) =>
            GorillaGameManager.StaticFindRigForPlayer(p);

        public static NetPlayer GetPlayerFromVRRig(VRRig p) =>
            p.Creator ?? NetworkSystem.Instance.GetPlayer(NetworkSystem.Instance.GetOwningPlayerID(p.gameObject));

        public static NetPlayer GetPlayerFromID(string id) =>
            PhotonNetwork.PlayerList.FirstOrDefault(player => player.UserId == id);

        public static Player NetPlayerToPlayer(NetPlayer p) =>
            p.GetPlayerRef();

        public static Player GetRandomPlayer(bool includeSelf = false) =>
            includeSelf ?
            PhotonNetwork.PlayerList[UnityEngine.Random.Range(0, PhotonNetwork.PlayerList.Length)] :
            PhotonNetwork.PlayerListOthers[UnityEngine.Random.Range(0, PhotonNetwork.PlayerListOthers.Length)];

        private static VRRig rigTarget;
        private static float rigTargetChange;
        public static VRRig GetTargetPlayer(float targetChangeDelay = 1f)
        {
            bool stillValid = rigTarget != null
                               && rigTarget.gameObject != null
                               && rigTarget.gameObject.activeInHierarchy
                               && !(Time.time > rigTargetChange);

            if (stillValid) return rigTarget;

            rigTargetChange = Time.time + targetChangeDelay;
            rigTarget = GetRandomVRRig();

            return rigTarget;
        }

        public static VRRig GetRandomVRRig(bool includeSelf = false) =>
            GetVRRigFromPlayer(GetRandomPlayer(includeSelf));

        public static NetworkView GetNetworkViewFromVRRig(VRRig p) =>
            p.GetComponent<NetworkView>();

        public static PhotonView GetPhotonViewFromVRRig(VRRig p) =>
            GetNetworkViewFromVRRig(p)?.GetView;

        public static VRRig GetClosestVRRig()
        {
            VRRig local = VRRig.LocalRig;
            if (local == null) return null;

            return UnityEngine.Object.FindObjectsByType<VRRig>(FindObjectsSortMode.None)
                .Where(r => r != local && r.gameObject != null && r.gameObject.activeInHierarchy)
                .OrderBy(r => Vector3.Distance(r.transform.position, local.transform.position))
                .FirstOrDefault();
        }

        // ---------------------------------------------------------
        // Account creation date lookup (PlayFab)
        // ---------------------------------------------------------

        public static readonly Dictionary<string, float> waitingForCreationDate = new Dictionary<string, float>();
        public static readonly Dictionary<string, string> creationDateCache = new Dictionary<string, string>();

        public static string GetCreationDate(string input, Action<string> onTranslated = null, string format = "MMMM dd, yyyy h:mm tt")
        {
            if (creationDateCache.TryGetValue(input, out string cached))
                return cached;

            bool onCooldown = waitingForCreationDate.TryGetValue(input, out float cooldown) && Time.time < cooldown;
            if (!onCooldown)
            {
                waitingForCreationDate[input] = Time.time + 10f;
                GetCreationCoroutine(input, onTranslated, format);
            }

            return "Loading...";
        }

        private static void GetCreationCoroutine(string userId, Action<string> onTranslated, string format)
        {
            PlayFabClientAPI.GetAccountInfo(new GetAccountInfoRequest { PlayFabId = userId },
                result =>
                {
                    waitingForCreationDate.Remove(userId);
                    string creationDate = result.AccountInfo.Created.ToString(format);
                    creationDateCache[userId] = creationDate;
                    onTranslated?.Invoke(creationDate);
                },
                error =>
                {
                    waitingForCreationDate.Remove(userId);
                    onTranslated?.Invoke("Error");
                });
        }

        // ---------------------------------------------------------
        // Platform detection
        // ---------------------------------------------------------

        public static string GetPlatform(Player player)
        {
            if (player?.CustomProperties == null) return "Unknown";

            try
            {
                if (player.CustomProperties.TryGetValue("platform", out object p1) && p1 != null)
                    return p1.ToString();
                if (player.CustomProperties.TryGetValue("Platform", out object p2) && p2 != null)
                    return p2.ToString();
                if (player.CustomProperties.TryGetValue("device", out object p3) && p3 != null)
                    return p3.ToString();
            }
            catch { }

            return "Unknown";
        }

        public static string GetPlatform(NetPlayer netPlayer)
        {
            if (netPlayer == null) return "Unknown";
            return GetPlatform(NetPlayerToPlayer(netPlayer));
        }

        // ---------------------------------------------------------
        // Suspicious property check
        // ---------------------------------------------------------

        private static readonly string[] SuspiciousKeys = new[]
        {
            "mod", "menu", "cheat", "hack", "seralyth", "iis", "stupid", "bark",
            "Bark", "shiba", "shibagt", "wholesome", "walksim", "longarms", "fly",
            "speed", "godmode", "iiDk", "iiDksTemplate", "aspect", "Genesis", "genesis",
            "prism", "starlight", "aids", "titans", "hollow", "kronos", "vex",
            "solaris", "overdrive", "nebula", "kismet", "silence", "zenith", "void",
            "shadow", "orbit", "inferno", "spectre", "synapse", "isGhost", "ghost",
            "isInvisible", "invis", "platPos", "rainbow", "rg",
            "targetID", "tagGunTarget", "oldModded", "spoofName", "fakeDev", "devStick",
            "stickOwned", "customCosmetics", "infiniteJumps", "noClipActive", "antiReport",
            "antiBan", "masterClientSpoof", "roomMaster", "__custom_prop_0x1337",
            "_0x_shiba_enc", "__b_a_r_k__", "_x_iiDk_v2", "pt_bypass", "f_anti_ac",
            "gt_internal_dbg", "pf_master_override", "photon_dev_token", "photon_room_owner_raw",
            "_g_menu_sync_v4", "_ac_silence_flag", "_bypass_vrrig_hash", "__p_flight_v2",
            "jumpMultiplier", "maxJumpSpeed", "scale", "slideControl", "maxArmLength",
            "locomotionEnabledLayers", "velocityHistory", "defaultSlideControl",
            "defaultJumpMultiplier", "bodyCollider", "headCollider", "leftControllerTransform",
            "rightControllerTransform", "mainSkin", "setMatIndex", "concatStringOfCosmeticsAllowed",
            "isOffline", "playerColor", "customColorString", "isGorilla", "cosmetics",
            "cosmeticsObjectDict", "myBodyMaterial", "setHue", "headRot", "leftHandTransform",
            "rightHandTransform", "taggedTime", "GorillaPhysicsController.forceApply",
            "VRRig.photonView.OwnerActorNumber", "GorillaTagManager.currentInfectedArray",
            "GorillaGameManager.isGameEnded", "GorillaSurfaceOverride.frictionMultiplier",
            "PlayFabAuthenticator.GorillaPlayerId", "NetworkSystem.Instance.SimulatedLatency",
            "GorillaScoreBoard.lines", "Platform", "StickyPlatform", "Tag Gun Raycast",
            "TP Gun Marker", "Freeze Gun Beam", "ESP Box", "Beacon", "Target Marker",
            "Ghost Monke Rig", "Decoy Rig", "Fly Board", "Jetpack Prop", "Rocket Prop",
            "Laser Pointer", "Water Balloon Spawner", "Snowball Spawner", "Rock Spawner",
            "Splash Effect Prefab", "Stick", "Admin Stick", "Ban Hammer", "Finger Painter Badge",
            "Illustrator Badge", "Moderator Badge", "Gold Monke", "Golden Rig",
            "Blinking Sun Hat", "Early Access Badge", "Slingshot", "Paintbrawl Guns",
            "Balloon Props", "RPC_SetTag", "RPC_UpdateColor", "RPC_PlaySound",
            "RPC_InitializeNoob", "RPC_SetMasterClient", "RPC_RequestCosmetics",
            "RPC_PopBalloon", "RPC_ThrowProjectile"
        };


        public static string GetSuspiciousProps(Player player)
        {
            // Path 1: Returns immediately if player or custom properties are null
            if (player?.CustomProperties == null) 
                return string.Empty;

            List<string> detectedProps = new List<string>();

            foreach (var key in player.CustomProperties.Keys)
            {
                string keyString = key.ToString();
                foreach (var sus in SuspiciousKeys)
                {
                    if (keyString.IndexOf(sus, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        detectedProps.Add(keyString);
                        break; // Stop checking this key once matched
                    }
                }
            }

            // Path 2: Ensures a string is ALWAYS returned, even if no matches were found
            return detectedProps.Count > 0 ? string.Join(", ", detectedProps) : string.Empty;
        }
        public static bool HasSuspiciousProps(Player player)
        {
            if (player?.CustomProperties == null) return false;

            foreach (var key in player.CustomProperties.Keys)
            {
                string k = key.ToString().ToLower();
                foreach (var sus in SuspiciousKeys)
                {
                    if (k.Contains(sus))
                        return true;
                }
            }
            return false;
        }

        public static bool HasSuspiciousProps(NetPlayer netPlayer)
        {
            if (netPlayer == null) return false;
            return HasSuspiciousProps(NetPlayerToPlayer(netPlayer));
        }

        // ---------------------------------------------------------
        // Time in room
        // ---------------------------------------------------------

        public static string GetTimeInRoom(Player player)
        {
            try
            {
                var netPlayer = NetworkSystem.Instance?.GetPlayer(player.ActorNumber);
                if (netPlayer != null)
                {
                    float seconds = Time.time - netPlayer.JoinedTime;
                    if (seconds < 60f) return $"{seconds:F0}s";
                    return $"{(seconds / 60f):F1}m";
                }
            }
            catch { }
            return "N/A";
        }

        public static string GetTimeInRoom(NetPlayer netPlayer)
        {
            if (netPlayer == null) return "N/A";
            try
            {
                float seconds = Time.time - netPlayer.JoinedTime;
                if (seconds < 60f) return $"{seconds:F0}s";
                return $"{(seconds / 60f):F1}m";
            }
            catch { }
            return "N/A";
        }
        public static VRRig GetVRRigFromPhotonPlayer(Player p)
        {
            if (p == null) return null;
            NetPlayer netPlayer = NetworkSystem.Instance.GetPlayer(p.ActorNumber);
            return netPlayer != null ? GetVRRigFromPlayer(netPlayer) : null;
        }
    }
}