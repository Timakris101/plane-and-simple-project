using UnityEngine;

public class PistonEngineScript : EngineScript {
    [SerializeField] protected AnimationCurve enginePowerByAlt;
    [SerializeField] protected float powerHp;
    [SerializeField] private float wepHp;
    private float propEff = .7f;

    public override void setVal(float val) {
        powerHp = val;
    }

    private float basePowerHpToWep = 0f;
    private void Awake() {
        basePowerHpToWep = wepHp / powerHp;
    }

    public override float getThrustNewtons(float speed) {
        return (enginesOn && canUseEngineGeneral()) ? (((PlaneController) vc).getInWEP() ? powerHp * basePowerHpToWep : powerHp) / Mathf.Max(30f, speed) * 745.7f * enginePowerByAlt.Evaluate(transform.position.y) * propEff * throttle : 0f;
    }

    public override string getType() {return "power";}

    public override float getVal() {return powerHp;}
    public override float getOverPowerVal() {return wepHp;}

    public override float consumptionRateFuelPerSec() {return ((enginesOn && canUseEngineGeneral()) ? (((PlaneController) vc).getInWEP() ? powerHp * basePowerHpToWep : powerHp) : 0) * throttle * fuelConsumedPerUnitThrustPerSecond;}

    public override bool canUseEngineSpecific() {
        bool anyPropellers = false;
        for (int i = 0; i < transform.parent.childCount; i++) {
            if (transform.parent.GetChild(i).GetComponent<PropellerScript>() != null) {
                anyPropellers = true;
                break;
            }
        }
        return anyPropellers;
    }
}
