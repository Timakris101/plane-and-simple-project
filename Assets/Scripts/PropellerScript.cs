using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Utils;

public class PropellerScript : MonoBehaviour {

    private float idleCoef;
    [SerializeField] private float engineAccelRate;
    private bool engineOn;
    private bool engineBroken;
    [Tooltip("X: position x | Y: position y | Z: rotation z | W: order (0 means gone)")]
    [SerializeField] private Quaternion[] valsOfPropAtAnimIndexNonWingless;
    [Tooltip("X: position x | Y: position y | Z: rotation z | W: order (0 means gone)")]
    [SerializeField] private Quaternion[] valsOfPropAtAnimIndexWingless;
    [SerializeField] private float engineAmt = 0;

    private EngineScript es;

    private PlaneController pc;

    void OnTriggerEnter2D(Collider2D col) {
        if (col.transform.tag == "Ground") {
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
        engineBroken = !es.gameObject.GetComponent<DamageModel>().isAlive();
        if (!engineBroken) {
            if (engineOn && GetComponent<Animator>().speed <= Mathf.Min(es.getThrottle() + idleCoef, 1)) {
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

        if (engineAmt != 1 && (!transform.parent.GetComponent<Animator>().GetBool("Tailless") || transform.parent.GetComponent<Animator>().GetBool("Wingless"))) {
            Quaternion[] arrToUse = (transform.parent.GetComponent<Animator>().GetBool("Wingless") ? valsOfPropAtAnimIndexWingless : valsOfPropAtAnimIndexNonWingless);
            int indexToUse = int.Parse(transform.parent.GetComponent<SpriteRenderer>().sprite.name.Substring(transform.parent.GetComponent<SpriteRenderer>().sprite.name.Length - 1));
            transform.localPosition = new Vector3(arrToUse[indexToUse].x, arrToUse[indexToUse].y, 0f);
            transform.localEulerAngles = new Vector3(0, 0, arrToUse[indexToUse].z);
            GetComponent<SpriteRenderer>().sortingOrder = (int) arrToUse[indexToUse].w;
            GetComponent<SpriteRenderer>().enabled = arrToUse[indexToUse].w != 0;
        }
    }
}
