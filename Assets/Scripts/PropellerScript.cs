using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Utils;

public class PropellerScript : MonoBehaviour {

    private float idleCoef;
    [SerializeField] private float engineAccelRate;
    private bool engineOn;
    [Tooltip("X: position x | Y: position y | Z: rotation z | W: order (0 means gone)")]
    [SerializeField] private Quaternion[] valsOfPropAtAnimIndexNonWingless;
    [Tooltip("X: position x | Y: position y | Z: rotation z | W: order (0 means gone)")]
    [SerializeField] private Quaternion[] valsOfPropAtAnimIndexWingless;
    [SerializeField] private float engineAmt = 0;

    private EngineScript es;

    private PlaneController pc;

    void OnTriggerEnter2D(Collider2D col) {
        if (col.transform.tag == "Ground" && col.transform.position.z == transform.position.z) {
            for (int i = 0; i < transform.parent.childCount; i++) {
                GameObject potentialProp = transform.parent.GetChild(i).gameObject;
                if (potentialProp.GetComponent<PropellerScript>() != null) {
                    Destroy(potentialProp);
                }
            }
        }
    }

    public bool isPropOfFallenWing() {
        return valsOfPropAtAnimIndexWingless.Length == 0;
    }

    void Start() {
        idleCoef = 0.05f;
        GetComponent<Animator>().speed = transform.parent.GetComponent<PlaneInit>().getEnginesStartOn() ? 1 : 0;

        for (int i = 0; i < transform.parent.childCount; i++) {
            if (transform.parent.GetChild(i).GetComponent<PropellerScript>() != null) engineAmt++;
        }

        if (valsOfPropAtAnimIndexWingless.Length != 0) {
            for (int i = 0; i < valsOfPropAtAnimIndexNonWingless.Length; i++) {
                valsOfPropAtAnimIndexWingless[i] = valsOfPropAtAnimIndexNonWingless[i];
            }
        }

        es = allObjectsInTreeWith<EngineScript>(gameObject)[0].GetComponent<EngineScript>();
    }

    void setPlaneController() {
        foreach (PlaneController c in transform.parent.GetComponents<PlaneController>()) {
            if (c.enabled) {
                pc = c;
                break;
            }
        } 
    }

    void Update() {
        setPlaneController();
        engineOn = es.getEnginesOn();
        if (es.canUseEngineGeneral()) {
            float maxSpeedYearnedFor = (pc.getInWEP() ? (es.getOverPowerVal() / es.getVal()) : 1f);
            if (engineOn && GetComponent<Animator>().speed <= Mathf.Min(Mathf.Min(es.getThrottle() * maxSpeedYearnedFor + idleCoef, maxSpeedYearnedFor), 2f)) {
                GetComponent<Animator>().speed *= engineAccelRate;
                GetComponent<Animator>().speed += engineAccelRate - 1;
            } else {
                GetComponent<Animator>().speed /= engineAccelRate;
            }
        } else {
            if (engineOn && GetComponent<Animator>().speed <= idleCoef) {
                GetComponent<Animator>().speed *= engineAccelRate;
                GetComponent<Animator>().speed += engineAccelRate - 1;
            } else {
                GetComponent<Animator>().speed /= engineAccelRate;
            }
        }
        GetComponent<AudioSource>().pitch = GetComponent<Animator>().speed;
        GetComponent<AudioSource>().volume = (.5f + .5f * GetComponent<Animator>().speed) * .8f;

        if (engineAmt != 1 && (!transform.parent.GetComponent<Animator>().GetBool("Tailless") || transform.parent.GetComponent<Animator>().GetBool("Wingless"))) {
            Quaternion[] arrToUse = (transform.parent.GetComponent<Animator>().GetBool("Wingless") ? valsOfPropAtAnimIndexWingless : valsOfPropAtAnimIndexNonWingless);
            int indexToUse = int.Parse(transform.parent.GetComponent<SpriteRenderer>().sprite.name.Substring(transform.parent.GetComponent<SpriteRenderer>().sprite.name.Length - 1));
            transform.localPosition = new Vector3(arrToUse[indexToUse].x, arrToUse[indexToUse].y, 0f);
            transform.localEulerAngles = new Vector3(0, 0, arrToUse[indexToUse].z);
            GetComponent<SpriteRenderer>().sortingOrder = (int) arrToUse[indexToUse].w;
            GetComponent<SpriteRenderer>().enabled = arrToUse[indexToUse].w != 0;
        }
    }

    private void OnDisable() {
        GetComponent<AudioSource>().volume = 0f;
    }
}
