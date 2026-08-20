using Photon.Pun;
using System;
using System.Reflection;
using UnityEngine;
using GorillaLocomotion;

namespace HudOverlay.Hud
{    
    public static class TPlayer
    {
        public static Vector3 World2Player(Vector3 world) =>
            world - GorillaTagger.Instance.bodyCollider.transform.position + GorillaTagger.Instance.transform.position;  
        public static Vector3 closePosition;
        public static Vector3 lastPosition = Vector3.zero;
        public static void TagPlayer(VRRig plr)
        {
            GorillaTagger.Instance.offlineVRRig.enabled = false;
            GorillaTagger.Instance.offlineVRRig.transform.SetPositionAndRotation(plr.transform.position + new Vector3(0f, -0.25f, 0f), plr.transform.rotation);

            PhotonNetwork.SendAllOutgoingCommands();

            MethodInfo method = typeof(PhotonNetwork).GetMethod("RunViewUpdate", BindingFlags.Static | BindingFlags.NonPublic);
            if (method != null)
            {
                method.Invoke(null, Array.Empty<object>());
            }

            PhotonView photonView = GameObject.Find("Player Objects/RigCache/Network Parent/GameMode(Clone)").GetPhotonView();
            if (photonView != null)
            {
                photonView.RPC("RPC_ReportT", RpcTarget.All, new object[]
                {
                    plr.Creator.ActorNumber
                });
            }

            GorillaTagger.Instance.offlineVRRig.enabled = true;
            PhotonNetwork.SendAllOutgoingCommands();

            MethodInfo method2 = typeof(PhotonNetwork).GetMethod("RunViewUpdate", BindingFlags.Static | BindingFlags.NonPublic);
            if (method2 != null)
            {
                method2.Invoke(null, Array.Empty<object>());
            }

            MethodInfo method3 = typeof(PhotonView).GetMethod("OnSerialize", BindingFlags.Instance | BindingFlags.NonPublic);
            if (method3 != null)
            {
                method3.Invoke(photonView, new object[2]);
            }

            PhotonNetwork.NetworkingClient.LoadBalancingPeer.SendAcksOnly();
        }   
        public static void TeleportPlayer(Vector3 pos, bool keepVelocity = false) // Prevents your hands from getting stuck on trees
        {
            GTPlayer.Instance.TeleportTo(World2Player(pos), GTPlayer.Instance.transform.rotation, keepVelocity);
            VRRig.LocalRig.transform.position = pos;

            closePosition = Vector3.zero;
            lastPosition = Vector3.zero; // Thanks for Seralyth for the code :D
        }
        public static Vector3 GetVector3(VRRig plr)
        {
            string name = plr?.Creator?.NickName ?? "Unknown";
            Vector3 pos = plr != null ? plr.transform.position : Vector3.zero;
            Debug.Log($"[HudOverlay] Say hi -> {name} at {pos}");
            return pos;
        }
    }
}