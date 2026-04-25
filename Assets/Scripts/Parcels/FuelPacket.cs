using UnityEngine;
using static Utils;

public class FuelPacket : PacketScript {
    public override void effect(GameObject plane) {
        if (allObjectsInTreeWith<FuelTankScript>(plane).Count == 0) return;
        allObjectsInTreeWith<FuelTankScript>(plane)[0].GetComponent<FuelTankScript>().addFuelPercent(.5f);
    }
}
