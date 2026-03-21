using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BailoutHandler : MonoBehaviour {

    [SerializeField] private GameObject crew;
    [SerializeField] private float bailOutDelay;
    private int bailCalled;
    private int counter;
    private float bailOutTimer;
    [SerializeField] private float ejectionSeatStrength;

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

            bailCrewMember(transform.GetChild(i).gameObject);
            
            i--;
        }
    }

    public void bailCrewMember(GameObject crewToBail) {
        GameObject newCrew = Instantiate(crew, crewToBail.transform.position, Quaternion.identity);
        Destroy(newCrew, 10f);
        newCrew.GetComponent<Rigidbody2D>().linearVelocity = GetComponent<Rigidbody2D>().linearVelocity + (Vector2) transform.up * transform.localScale.y * ejectionSeatStrength;

        if (transform.Find("Camera") != null) transform.Find("Camera").parent = null;

        crewToBail.GetComponent<BoxCollider2D>().size = newCrew.GetComponent<BoxCollider2D>().size;
        crewToBail.GetComponent<BoxCollider2D>().offset = newCrew.GetComponent<BoxCollider2D>().offset;
        crewToBail.transform.rotation = newCrew.transform.rotation;
        crewToBail.transform.parent = newCrew.transform;
    }
}
