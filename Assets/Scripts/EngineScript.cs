using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Utils;

public class EngineScript : MonoBehaviour {
    protected VehicleController vc;
    [SerializeField] protected float throttle;
    [SerializeField] protected bool enginesOn;
    [SerializeField] protected float fuelConsumedPerUnitThrustPerSecond;

    [SerializeField] private GameObject wepOrAbEffect;
    GameObject instantiatedEffect;
    GameObject fuelTank;

    void Start() {
        if (allObjectsInTreeWith<FuelTankScript>(gameObject).Count != 0) fuelTank = allObjectsInTreeWith<FuelTankScript>(gameObject)[0];
        setVehicleController(); 
        if (wepOrAbEffect != null) {
            instantiatedEffect = Instantiate(wepOrAbEffect, transform);
            instantiatedEffect.transform.localPosition = GetComponent<BoxCollider2D>().offset;
        }
    }

    void Update() {
        setVehicleController();
        if (instantiatedEffect != null) {
            ParticleSystem ps = instantiatedEffect.GetComponent<ParticleSystem>();
            PlaneController pc = null;
            foreach (PlaneController c in transform.parent.GetComponents<PlaneController>()) {
                if (c.enabled) {
                    pc = c;
                    break;
                }
            } 
            if (pc.getInWEP() && getVal() != 0 && enginesOn) {
                if (!ps.isPlaying) {
                    ps.Play(true);
                }
            } else {
                if (ps.isPlaying) {
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                }
            }
        }
    }

    public virtual float consumptionRateFuelPerSec() {return 0;}

    public virtual void setVal(float val) {}

    void setVehicleController() {
        vc = null;
        foreach (VehicleController c in transform.parent.GetComponents<VehicleController>()) {
            if (c.enabled) {
                vc = c;
                break;
            }
        } 
    }

    public virtual float getVal() {return 0f;}
    public virtual float getOverPowerVal() {return 0f;}

    public virtual float getThrustNewtons(float speed, bool reverse) {return 0f;}
    public virtual float getThrustNewtons(float speed) {return 0f;}
    public virtual float getThrustNewtons() {return 0f;}

    public virtual string getType() {return "";}

    public bool canUseEngineGeneral() {
        if (fuelTank == null) return canUseEngineSpecific();
        return canUseEngineSpecific() && !fuelTank.GetComponent<FuelTankScript>().empty();
    }

    public virtual bool canUseEngineSpecific() {return true;}

    public void setThrottle(float f) {
        throttle = f;
    }
    public float getThrottle() {
        return throttle;
    }
    public void setEngines(bool b) {
        enginesOn = b;
    }
    public bool getEnginesOn() {
        return enginesOn;
    }
}
