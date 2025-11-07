using UnityEngine;

public class GroundVehicleEngineScript : EngineScript {
    [SerializeField] protected float powerHp;
    [SerializeField] private float frontGearAmt;
    [SerializeField] private float reverseGearAmt;

    public override float getThrustNewtons(float speed, bool reverse) {
        return enginesOn ? (powerHp) / Mathf.Max(1f, speed) * 745.7f * (reverse ? reverseGearAmt / frontGearAmt : 1f) : 0f;
    }

    public override void setVal(float val) {
        powerHp = val;
    }

    public override string getType() {return "power";}

    public override float getVal() {return powerHp;}
}
