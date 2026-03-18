using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CloudScript : MonoBehaviour {
    private float timer;
    [SerializeField] private float maxLifeTime;
    private float lifeTime;
    [SerializeField] private float maxSpeed;
    private float speed;
    private float speedOfDissilution = 0.1f;
    private float speedOfExpansion = 0.01f;

    private Color color => GetComponent<SpriteRenderer>().material.color;

    void Awake() {
        GetComponent<SpriteRenderer>().material.color = new Color(color.r, color.g, color.b, 0f);
    }

    void Start() {
        speed = Random.Range(-maxSpeed, maxSpeed);
        lifeTime = Random.Range(0f, maxLifeTime);
    }

    void Update() {
        if (color.a < 1f && timer < lifeTime) GetComponent<SpriteRenderer>().material.color = new Color(color.r, color.g, color.b, color.a + speedOfDissilution * Time.deltaTime);

        transform.position += Vector3.right * speed * Time.deltaTime;
        
        timer += Time.deltaTime;
        if (timer >= lifeTime) {
            GetComponent<SpriteRenderer>().material.color = new Color(color.r, color.g, color.b, color.a - speedOfDissilution * Time.deltaTime);
            transform.localScale = new Vector3(transform.localScale.x * (1f + speedOfExpansion * Time.deltaTime), transform.localScale.y * (1f + speedOfExpansion * Time.deltaTime), 1f);
        }

        if (GetComponent<SpriteRenderer>().material.color.a <= 0) {
            Destroy(gameObject);
        }
    }
}
