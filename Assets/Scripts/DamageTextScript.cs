using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using static Utils;

public class DamageTextScript : MonoBehaviour {

    [SerializeField] private float lifeTime;
    private float jitterStrength = 5f;
    private float upwardSpeed = 20f;

    void Start() {
        Destroy(gameObject, lifeTime);
        transform.position += new Vector3(Random.Range(-jitterStrength, jitterStrength), Random.Range(-jitterStrength, jitterStrength), 0f);
    }

    void Update() {
        progenyWithScript<TMP_Text>(gameObject)[0].GetComponent<TMP_Text>().alpha -= (1 / lifeTime * Time.deltaTime);
        float jitter = Random.Range(-jitterStrength, jitterStrength);
        transform.position += new Vector3(jitter, upwardSpeed + jitter, 0) * Time.deltaTime;
    }
}
