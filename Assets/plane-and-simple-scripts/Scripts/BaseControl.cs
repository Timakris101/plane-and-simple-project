using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using static Utils;

public class BaseControl : MonoBehaviour {
    [SerializeField] private string mode;
    private Func<float> lookValue;

    protected virtual void writeToLookValue() {
        GameObject vehicle = parentWithScript<CamScript>(gameObject).GetComponent<CamScript>().getControlledOrSpectatedVehicle();
        if (vehicle == null) return;

        vehicle.GetComponent<PlaneController>().setThrottle(GetComponent<Slider>().value);
    }

    protected virtual void setReadingVal() {
        GameObject vehicle = parentWithScript<CamScript>(gameObject).GetComponent<CamScript>().getControlledOrSpectatedVehicle();
        if (vehicle == null) {
            lookValue = () => 0f;
            return;
        }

        lookValue = () => vehicle.GetComponent<PlaneController>().getThrottle();
    }

    void Update() {
        setReadingVal();
        switch (mode) {
            case "Read":
            GetComponent<Slider>().value = lookValue();
            break;

            case "Write":
            writeToLookValue();
            break;

            default:
            break;
        }
    }
}
