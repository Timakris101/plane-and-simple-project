using UnityEngine;
using static Utils;

public class AmmoPacket : PacketScript {
    public override void effect(GameObject plane) {
        if (allObjectsInTreeWith<GunScript>(plane).Count == 0) return;
        foreach (GameObject d in allObjectsInTreeWith<GunScript>(plane)) d.GetComponent<GunScript>().fillAmmo();
    }
}
