using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BailoutHandler : MonoBehaviour {

    [SerializeField] private GameObject crew;
    [SerializeField] private float bailOutDelay;
    private int bailCalled;
    private int counter;
    private float bailOutTimer;

    void Update() {
        counter++;
        if (counter > bailCalled + 1) {
            bailCalled = 0;
            counter = 0;
            bailOutTimer = 0;
        }
    }

    public void callBailOut() {
        bailCalled++;

        bailOutTimer += Time.deltaTime;
        if (bailOutTimer > bailOutDelay) {
            bailOutTimer = 0;
            bailOut();
        }
    }

    public void bailOut() {
        for (int i = 0; i < transform.childCount; i++) {
            if (transform.GetChild(i).GetComponent<DamageModel>() == null) continue;
            if (!transform.GetChild(i).GetComponent<DamageModel>().isCrewRole()) continue;
            if (!transform.GetChild(i).GetComponent<DamageModel>().isAlive()) continue;

            GameObject hitbox = transform.GetChild(i).gameObject;

            GameObject newCrew = Instantiate(crew, hitbox.transform.position, Quaternion.identity);
            newCrew.GetComponent<Rigidbody2D>().linearVelocity = GetComponent<Rigidbody2D>().linearVelocity;

            if (transform.Find("Camera") != null) transform.Find("Camera").parent = null;

            hitbox.GetComponent<BoxCollider2D>().size = newCrew.GetComponent<BoxCollider2D>().size;
            hitbox.GetComponent<BoxCollider2D>().offset = newCrew.GetComponent<BoxCollider2D>().offset;
            hitbox.transform.rotation = newCrew.transform.rotation;
            hitbox.transform.parent = newCrew.transform;
            
            i--;
        }
    }
}
