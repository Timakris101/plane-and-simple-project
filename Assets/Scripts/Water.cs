using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Utils;

public class Water : MonoBehaviour {
    [SerializeField] private float dragForceCoef;
    [SerializeField] private GameObject splashEffect;
    [SerializeField] private float splashCoef;
    [SerializeField] private float maxSplashSize;

    private float seaLevel => GetComponent<SpriteRenderer>().size.y / 2f;

    void OnTriggerEnter2D(Collider2D other) {
        if (other.transform.gameObject.layer != LayerMask.NameToLayer("Vehicle") && other.transform.gameObject.layer != LayerMask.NameToLayer("Crew") && other.transform.parent == null) {
            //GameObject.Find("NetworkManager").GetComponent<MultiplayerCreateAndDestroy>().destroy(other.transform.gameObject);
        }
        if (other.transform.parent == null) {
            GameObject newSplash = Instantiate(splashEffect, other.transform.position, Quaternion.identity);
            var mainModule = newSplash.GetComponent<ParticleSystem>().main;
            mainModule.startSpeed = new ParticleSystem.MinMaxCurve(splashSize(other.transform.gameObject) / 2f, splashSize(other.transform.gameObject));
            //GameObject.Find("NetworkManager").GetComponent<MultiplayerCreateAndDestroy>().destroy(newSplash, mainModule.startLifetime.constant);
        }
    }

    private float splashSize(GameObject objEntering) {
        return Mathf.Min(objEntering.GetComponent<Rigidbody2D>().mass * Mathf.Pow(objEntering.GetComponent<Rigidbody2D>().linearVelocity.y, 2) * splashCoef, maxSplashSize);
    }

    void OnTriggerStay2D(Collider2D other) {
        if (other.transform.GetComponent<Rigidbody2D>() != null) {
            float dragForce = dragForceCoef * Mathf.Pow(other.transform.GetComponent<Rigidbody2D>().linearVelocity.magnitude, 2);
            if (other.transform.GetComponent<Rigidbody2D>().linearVelocity.magnitude > .5f) other.transform.GetComponent<Rigidbody2D>().AddForce(-other.transform.GetComponent<Rigidbody2D>().linearVelocity.normalized * dragForce * Mathf.Clamp01((seaLevel - other.transform.position.y)), ForceMode2D.Force);
        }
        foreach (GameObject damageModel in allObjectsInTreeWith<DamageModel>(other.transform.gameObject)) {
            if (damageModel.transform.position.y > seaLevel) damageModel.GetComponent<DamageModel>().drown();
        }
    }
}
