using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using static Utils;

public class Water : NetworkBehaviour {
    [SerializeField] private float dragForceCoef;
    [SerializeField] private GameObject splashEffect;
    [SerializeField] private float splashCoef;
    [SerializeField] private float maxSplashSize;

    private float seaLevel => GetComponent<SpriteRenderer>().size.y / 2f;

    void OnTriggerEnter2D(Collider2D other) {
        if (other.transform.gameObject.layer != LayerMask.NameToLayer("Vehicle") && other.transform.gameObject.layer != LayerMask.NameToLayer("Crew") && other.transform.parent == null) {
            if (NetworkManager.Singleton.IsListening) {
                GameObject.Find("MultiplayerCreateAndDestroy").GetComponent<MultiplayerCreateAndDestroy>().destroy(other.transform.gameObject);
            } else {
                Destroy(other.transform.gameObject);
            }
        }
        if (other.transform.parent == null) {
            GameObject newSplash = Instantiate(splashEffect, other.transform.position, Quaternion.identity);
            Destroy(newSplash, 10f);
            var mainModule = newSplash.GetComponent<ParticleSystem>().main;
            mainModule.startSpeed = new ParticleSystem.MinMaxCurve(splashSize(other.transform.gameObject) / 5f, splashSize(other.transform.gameObject));
            mainModule.startLifetime = Mathf.Clamp(other.GetComponent<Rigidbody2D>().mass, .1f, 5f);

            var emission = newSplash.GetComponent<ParticleSystem>().emission;
            ParticleSystem.Burst[] bursts = new ParticleSystem.Burst[emission.burstCount];
            emission.GetBursts(bursts);
            bursts[0].count = Mathf.Clamp(other.GetComponent<Rigidbody2D>().mass, 3, 1000);
            emission.SetBursts(bursts);

            newSplash.transform.up = Vector3.Reflect((Vector3) other.GetComponent<Rigidbody2D>().linearVelocity, transform.up);

            newSplash.GetComponent<AudioSource>().volume = splashSize(other.transform.gameObject);
            if (NetworkManager.Singleton.IsListening) {
                GameObject.Find("MultiplayerCreateAndDestroy").GetComponent<MultiplayerCreateAndDestroy>().destroy(newSplash, 10f);
            }
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
            if (damageModel.transform.position.y < seaLevel) damageModel.GetComponent<DamageModel>().drown();
        }
    }

    void OnTriggerExit2D(Collider2D other) {
        foreach (GameObject damageModel in allObjectsInTreeWith<DamageModel>(other.transform.gameObject)) {
            if (damageModel.transform.position.y < seaLevel) damageModel.GetComponent<DamageModel>().undrown();
        }
    }
}
