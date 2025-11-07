using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlapScript : MonoBehaviour {
    [SerializeField] private float maxDeflection; //note: includes base 90 deg offset
    [SerializeField] private float flapSpeed;
    [SerializeField] private bool flapsDown;
    [SerializeField] private float flapEffectiveness;
    [SerializeField] private float flapDrag;
    [SerializeField] private float breakSpeed;

    private Sprite origSpriteOfPlane;

    void Start() {
        origSpriteOfPlane = transform.parent.GetComponent<SpriteRenderer>().sprite;
    }

    void Update() {
        handleFlaps();
        if (transform.parent != null) {
            if (deflection() >= maxDeflection && transform.parent.GetComponent<Rigidbody2D>().linearVelocity.magnitude > breakSpeed) breakFlaps();
        }
        if (transform.parent != null) {
            if (transform.parent.GetComponent<SpriteRenderer>().sprite == origSpriteOfPlane) {
                unhideFlaps();
            } else {
                hideFlaps();
            }
        } else {
            unhideFlaps();
        }
    }

    private void handleFlaps() {
        if (transform.parent != null) {
            if (flapsDown) {
                if (deflection() < maxDeflection) {
                    transform.RotateAround(transform.Find("Rp").position, transform.forward * transform.parent.localScale.y, flapSpeed * Time.deltaTime);
                }
            } else {
                if (deflection() > 0f) {
                    transform.RotateAround(transform.Find("Rp").position, transform.forward * transform.parent.localScale.y, -flapSpeed * Time.deltaTime);
                }
            }
        }
    }

    public float deflection() {
        return transform.localEulerAngles.z - 90f;
    }

    public void hideFlaps() {
        GetComponent<SpriteRenderer>().enabled = false;
    }

    public void unhideFlaps() {
        GetComponent<SpriteRenderer>().enabled = true;
    }

    public void breakFlaps() {
        Vector3 vel = transform.parent.GetComponent<Rigidbody2D>().linearVelocity;
        transform.SetParent(null, true);
        gameObject.AddComponent<Rigidbody2D>();
        GetComponent<Rigidbody2D>().linearDamping = 1;
        GetComponent<Rigidbody2D>().linearVelocity = vel;
        GetComponent<Rigidbody2D>().angularVelocity = Random.Range(-GetComponent<Rigidbody2D>().linearVelocity.magnitude, GetComponent<Rigidbody2D>().linearVelocity.magnitude);
        
        Destroy(gameObject, 10f);
    }

    public void toggleFlaps() {
        flapsDown = !flapsDown;
    }

    public float getFlapEffectiveness() {
        return flapEffectiveness;
    }

    public float getFlapDrag() {
        return flapDrag;
    }

    public float getMaxDeflection() {
        return maxDeflection;
    }
}
