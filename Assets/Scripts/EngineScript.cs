using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EngineScript : MonoBehaviour {
    protected VehicleController vc;
    [SerializeField] protected float throttle;
    [SerializeField] protected bool enginesOn;

    [SerializeField] private GameObject wepOrAbEffect;
    GameObject instantiatedEffect;

    void Start() {
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
