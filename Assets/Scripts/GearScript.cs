using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GearScript : MonoBehaviour {

    [SerializeField] private bool gearDownAtStart = true;
    [SerializeField] private float gearDrag;

    [Header("Breaking")]
    [SerializeField] private float breakSpeed;
    [SerializeField] private float crushSpeed;
    [SerializeField] private float gearlessArea;

    [Header("Mats")]
    [SerializeField] private PhysicsMaterial2D brakeMat;
    [SerializeField] private PhysicsMaterial2D rollMat;
    [SerializeField] private Sprite gearDownSprite;
    [SerializeField] private Sprite gearUpSprite;

    private float timeBraking;
    private float time;

    private Sprite origSpriteOfPlane;

    void OnCollisionEnter2D(Collision2D col) {
        if (transform.parent != null) {
            if (Vector3.Project(col.contacts[0].relativeVelocity, -col.contacts[0].normal).magnitude > crushSpeed) breakGear();
        }
    }

    void Start() {
        GetComponent<Animator>().enabled = false;
        if (gearDownAtStart) {
            GetComponent<SpriteRenderer>().sprite = gearDownSprite;
        } else {
            GetComponent<SpriteRenderer>().sprite = gearUpSprite;
        }
        GetComponent<Animator>().SetBool("gearDownAtStart", gearDownAtStart);
        GetComponent<Animator>().SetBool("gearDeployed", gearDownAtStart);

        origSpriteOfPlane = transform.parent.GetComponent<SpriteRenderer>().sprite;
    }

    public float getGearDrag() {
        return isGearDown() ? gearDrag : 0f;
    }

    public void gearDown() {
        GetComponent<Animator>().enabled = true;
        GetComponent<Animator>().SetBool("gearDeployed", true);
    }

    public void gearUp() {
        GetComponent<Animator>().enabled = true;
        GetComponent<Animator>().SetBool("gearDeployed", false);
    }

    public bool isGearDown() {
        return GetComponent<Animator>().GetBool("gearDeployed");
    }

    public void breakGear() {
        gearDown();
        Vector3 vel = transform.parent.GetComponent<Rigidbody2D>().linearVelocity;
        transform.parent.GetComponent<Aerodynamics>().setFrontArea(transform.parent.GetComponent<Aerodynamics>().getFrontArea() + gearlessArea);
        transform.SetParent(null, true);
        gameObject.AddComponent<Rigidbody2D>();
        GetComponent<Rigidbody2D>().linearDamping = 1;
        GetComponent<Rigidbody2D>().linearVelocity = vel;
        GetComponent<Rigidbody2D>().angularVelocity = Random.Range(-GetComponent<Rigidbody2D>().linearVelocity.magnitude, GetComponent<Rigidbody2D>().linearVelocity.magnitude);
        
        Destroy(gameObject, 10f);
    }

    public void toggleGear() {
        if (isGearDown()) {
            gearUp();
        } else {
            gearDown();
        }
    }

    public void brake() {
        GetComponent<BoxCollider2D>().sharedMaterial = brakeMat;
        timeBraking++;
    }

    private void unbrakeIfNoBrake() {
        time++;
        if (time > timeBraking + 1) {
            time = 0;
            timeBraking = 0;
            GetComponent<BoxCollider2D>().sharedMaterial = rollMat;
        }
    }

    public void hideGear() {
        GetComponent<SpriteRenderer>().enabled = false;
    }

    public void unhideGear() {
        GetComponent<SpriteRenderer>().enabled = true;
    }

    void Update() {
        unbrakeIfNoBrake();
        GetComponent<BoxCollider2D>().enabled = GetComponent<SpriteRenderer>().sprite == gearDownSprite;
        if (transform.parent != null) {
            if (isGearDown() && transform.parent.GetComponent<Rigidbody2D>().linearVelocity.magnitude > breakSpeed) breakGear();
        }
        if (transform.parent != null) {
            if (transform.parent.GetComponent<SpriteRenderer>().sprite == origSpriteOfPlane) {
                unhideGear();
            } else {
                hideGear();
            }
        } else {
            unhideGear();
        }
    }
}
