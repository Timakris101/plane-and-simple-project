using UnityEngine;
using static Utils;

public class RepairPacket : PacketScript {
    public override void effect(GameObject plane) {
        if (allObjectsInTreeWith<DamageModel>(plane).Count == 0) return;
        foreach (GameObject d in allObjectsInTreeWith<DamageModel>(plane)) d.GetComponent<DamageModel>().repair();
    }
}
