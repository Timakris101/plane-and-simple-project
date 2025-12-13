using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using static Utils;

public class BaseDial : MonoBehaviour {
    [SerializeField] private GameObject baseNumberText;
    [SerializeField] private float numberScaling;
    [SerializeField] public float startVal;
    [SerializeField] public float endVal;
    [SerializeField] public float stepSize;
    [SerializeField] public float startAngle;
    [SerializeField] public float endAngle;
    [SerializeField] public float angleStepDir;
    [SerializeField] public float numDistanceFromCenter;
    [SerializeField] public string type;
    
    void OnEnable() {
        if (stepSize == 0) return;
        GameObject label = progenyWithScript<TMP_Text>(gameObject)[0];
        label.GetComponent<TMP_Text>().text = type;
        if (progenyWithScript<TMP_Text>(gameObject).Count > 1) {
            for (int i = 0; i < transform.childCount; i++) {
                if (transform.GetChild(i).gameObject == label) continue;
                if (transform.GetChild(i).GetComponent<BaseDialNeedle>() != null) continue;
                Destroy(transform.GetChild(i).gameObject);
            }
        }
        List<float> validAngles = new List<float>();
        if (stepSize == 0) return;
        float angleStepSize = Mathf.Abs((endAngle - (startAngle > endAngle ? startAngle - 360f : startAngle)) / (endVal - startVal) * stepSize);
        for (float angle = (startAngle > endAngle ? startAngle - 360f : startAngle); angle <= endAngle; angle += angleStepSize) {
            validAngles.Add(angle);
        }
        float valToStartWritingFrom = angleStepDir == 1f ? startVal : endVal;
        for (int i = 0; i < validAngles.Count; i++) {
            float valAtThisAngle = valToStartWritingFrom + angleStepDir * i * stepSize;
            Vector3 localPosToMakeNum = new Vector3(Mathf.Cos(validAngles[i] * Mathf.Deg2Rad), Mathf.Sin(validAngles[i] * Mathf.Deg2Rad), 0f) * numDistanceFromCenter;
            GameObject newNumText = Instantiate(baseNumberText, new Vector3(0,0,0), Quaternion.identity, transform);
            newNumText.GetComponent<RectTransform>().localPosition = localPosToMakeNum;
            newNumText.GetComponent<TMP_Text>().text = (valAtThisAngle * numberScaling).ToString();
        }
    }

    public float valueToAngleConversionFactor() {
        return Mathf.Abs((endAngle - (startAngle > endAngle ? startAngle - 360f : startAngle)) / (endVal - startVal));
    }

    void Update() {
        
    }
}
