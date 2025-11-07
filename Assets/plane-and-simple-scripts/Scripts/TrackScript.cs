using UnityEngine;

public class TrackScript : MonoBehaviour {
    [SerializeField] private bool contactingGround;
    [SerializeField] private PhysicsMaterial2D brakeMat;
    [SerializeField] private PhysicsMaterial2D rollMat;

    void OnCollisionStay2D(Collision2D other) {
        if (other.transform.tag == "Ground") {
            contactingGround = true;
        }
    }

    void OnCollisionExit2D(Collision2D other) {
        if (other.transform.tag == "Ground") {
            contactingGround = false;
        }
    }

    public bool isContactingGround() {
        return contactingGround;
    }

    public bool usable() {
        return contactingGround && GetComponent<DamageModel>().isAlive();
    }

    public void braking(bool b) {
        if (b) {
            GetComponent<BoxCollider2D>().sharedMaterial = brakeMat;
        } else {
            GetComponent<BoxCollider2D>().sharedMaterial = rollMat;
        }
    }
}
