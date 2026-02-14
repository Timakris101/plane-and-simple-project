using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using static Utils;

public class BaseDialNeedle : MonoBehaviour {
    protected Func<float> valueToRead;
    [SerializeField] private BaseDial dial;
    [SerializeField] private float scaling;
    [SerializeField] private float needleSize;
    [SerializeField] private float needleThickness;
    [SerializeField] private bool bounded;

    protected virtual void setReadingVal() {
        GameObject vehicle = GameObject.Find("Camera").GetComponent<CamScript>().getControlledOrSpectatedVehicle();
        if (vehicle == null) {
            valueToRead = () => 0f;
            return;
        }
        switch(dial.type) {
            case "Speed": 
            valueToRead = () => vehicle.GetComponent<Rigidbody2D>().linearVelocity.magnitude;
            break;
            
            case "Altitude": 
            valueToRead = () => vehicle.transform.position.y;
            break;

            case "Rotation": 
            valueToRead = () => vehicle.transform.localEulerAngles.z;
            transform.localScale = new Vector3(1, vehicle.transform.localScale.y, 1);
            break;

            default: 
            valueToRead = () => 0f;
            break;
        }
    }

    void Update() {
        dial = transform.parent.GetComponent<BaseDial>();
        setReadingVal();
        transform.GetChild(0).GetComponent<RectTransform>().sizeDelta = new Vector2(needleSize, needleThickness);
        transform.GetChild(0).GetComponent<RectTransform>().localPosition = new Vector2(needleSize / 2f, 0f);

        float angleToPointTo = (dial.angleStepDir == 1f ? dial.startAngle : dial.endAngle) + ((bounded ? Mathf.Clamp(valueToRead(), dial.startVal, dial.endVal) : valueToRead() - dial.startVal) * scaling * dial.valueToAngleConversionFactor() * dial.angleStepDir);
        transform.localEulerAngles = new Vector3(0f, 0f, angleToPointTo);
    }
}
