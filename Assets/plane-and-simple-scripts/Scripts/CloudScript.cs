using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CloudScript : MonoBehaviour {
    private float timer;
    [SerializeField] private float maxLifeTime;
    private float lifeTime;
    [SerializeField] private float maxSpeed;
    private float speed;

    void Start() {
        speed = Random.Range(-maxSpeed, maxSpeed);
        lifeTime = Random.Range(0f, maxLifeTime);
    }

    void Update() {
        transform.position += Vector3.right * speed * Time.deltaTime;
        
        timer += Time.deltaTime;
        var emissionModule = GetComponent<ParticleSystem>().emission;
        if (timer >= lifeTime) {
            emissionModule.rateOverTime = 0f;
        }
        if (GetComponent<ParticleSystem>().particleCount == 0 && emissionModule.rateOverTime.constant == 0f) {
            Destroy(gameObject);
        }
    }
}
