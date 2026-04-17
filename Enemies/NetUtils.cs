using Godot;
using System;

public static class NetUtils
{
    public static void SafeDestroy(NetworkCore networkCore, NetID netId)
    {
        if (netId == null)
            return;

        // 🔑 Let their system run normally FIRST
        networkCore.NetDestroyObject(netId);

        // 🔒 Safety cleanup (only if something failed)
        var dict = GenericCore.Instance.netObjects;
        int id = (int)netId.netObjectID;

        if (dict.ContainsKey(id))
        {
            dict.Remove(id);
        }
    }
}