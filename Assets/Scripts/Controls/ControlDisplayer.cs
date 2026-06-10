using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using static Utils;

public class ControlDisplayer : MonoBehaviour {
    [SerializeField] private bool showing;

    void Start() {
        List<GameObject> controlDisplays = progenyWithScript<ControlDisplay>(gameObject);
        foreach (GameObject controlDisplay in controlDisplays) {
            if (controlDisplay.GetComponent<Graphic>() != null) {
                controlDisplay.SetActive(showing);
                //mafiaReport();
            }
            foreach (GameObject imgObj in progenyWithScript<Graphic>(controlDisplay)) {
                imgObj.SetActive(showing);
                //mafiaReport();
            }
        }
    }

    public void mafiaReport() {
        Debug.Log("*italian accent* hey boss, we found something... " + (!showing ? "*BANG*" : ""));
    }
}
