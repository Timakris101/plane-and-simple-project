using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using static Utils;

public class DamageTextScript : MonoBehaviour {

    [SerializeField] private float lifeTime;
    [SerializeField] private float jitterStrength;
    [SerializeField] private float upwardSpeed;
    private GameObject text;

    float startFontSize;

    void Start() {
        Destroy(gameObject, lifeTime);
        text = progenyWithScript<TMP_Text>(gameObject)[0];
        text.transform.SetParent(transform.parent);
        transform.SetParent(null, true);
        Destroy(text, lifeTime);

        startFontSize = text.GetComponent<TMP_Text>().fontSize;
    }

    void Update() {
        text.GetComponent<TMP_Text>().alpha -= (1 / lifeTime * Time.deltaTime);
        text.transform.position = parentWithScript<Camera>(text).GetComponent<Camera>().WorldToScreenPoint(transform.position);
        float jitter = Random.Range(-jitterStrength, jitterStrength);

        float curCamScaling = (parentWithScript<Camera>(text).GetComponent<Camera>().WorldToScreenPoint(new Vector3(0,0,0)) - parentWithScript<Camera>(text).GetComponent<Camera>().WorldToScreenPoint(new Vector3(1,0,0))).magnitude;

        transform.position += new Vector3(jitter, upwardSpeed + jitter, 0) * Time.deltaTime * curCamScaling;
        text.GetComponent<TMP_Text>().fontSize = startFontSize * curCamScaling;
    }
}
