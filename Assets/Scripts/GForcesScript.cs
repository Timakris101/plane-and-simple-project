using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GForcesScript : MonoBehaviour {
    [SerializeField] private float rollOverThresh;
    [SerializeField] private Vector3 currentGs;
    [SerializeField] private float feltGs;
    private Vector3 prevVel;
    [SerializeField] private float sleepyGs;
    [SerializeField] private float killingGs;
    [SerializeField] private float planeStructDestroyingGs;
    [SerializeField] private float planeDestroyingGs;
    private float minRolloverSpeed = 4f;
    private bool sleepy;
    private float inGlocTimer;
    private float timeToGloc = 4f;
    private float inSleepTimer;
    private float timeToUnsleep = 1f;

    [Header("DestructiveEffects")]
    [SerializeField] private GameObject fire;
    [SerializeField] private GameObject explosion;
    private bool destroyed = false;
    private Sprite origSprite;

    void Start() {
        origSprite = GetComponent<SpriteRenderer>().sprite;
    }

    void FixedUpdate() {
        if (feltGs < rollOverThresh && GetComponent<Rigidbody2D>().linearVelocity.magnitude > minRolloverSpeed && !GetComponent<PlaneController>().pilotDeadOrGone() && !sleepy && GetComponent<SpriteRenderer>().sprite == origSprite) {
            rollover();
        }
        if (overGPlaneToDeath() && !destroyed) {
            destroyed = true;
            Instantiate(explosion, transform.position, Quaternion.identity);
            Instantiate(fire, transform, false);
            GetComponent<Aerodynamics>().setSpeedOfControlEff(Mathf.Infinity);
            if (SceneManager.GetActiveScene().name == "Arcade") Destroy(gameObject, 10f);
        }
        if (overGPlane()) {
            if (transform.Find("WingHitbox") != null) transform.Find("WingHitbox").GetComponent<DamageModel>().kill();
            if (transform.Find("TailHitbox") != null) transform.Find("TailHitbox").GetComponent<DamageModel>().kill();
        }
        if (overGPersonToDeath()) {
            for (int i = 0; i < transform.childCount; i++) {
                if (transform.GetChild(i).GetComponent<DamageModel>() == null) continue;
                if (!transform.GetChild(i).GetComponent<DamageModel>().isCrewRole()) continue;

                transform.GetChild(i).GetComponent<DamageModel>().kill();
            }
        }
        updateSleepy();
        calculateGs();
    }

    private void updateSleepy() {
        if (!sleepy) inGlocTimer = Mathf.Max(inGlocTimer + (feltGs - sleepyGs) * Time.fixedDeltaTime, 0f);

        if (!sleepy && inGlocTimer > timeToGloc) {
            sleepy = true;
            inSleepTimer = 0;
        }
        if (sleepy) {
            inSleepTimer += Time.fixedDeltaTime;
        }
        if (inSleepTimer > timeToUnsleep) {
            sleepy = false;
        }
    }

    private void rollover() {
        transform.localScale = new Vector3(transform.localScale.x, transform.localScale.y * -1, transform.localScale.z);
        GetComponent<Animator>().SetTrigger("Rollover");
    }

    private void calculateGs() {
        if (prevVel.magnitude != 0f) {
            Vector3 curVel = GetComponent<Rigidbody2D>().linearVelocity;
            Vector3 currentForces = (curVel - prevVel) / Time.fixedDeltaTime / 9.8f;

            if (currentForces.magnitude != 0) currentGs = transform.localScale.y * (currentForces + Vector3.up);
            feltGs = (transform.up.x != 0 ? Vector3.Project(currentGs, transform.up).x / transform.up.x : Vector3.Project(currentGs, transform.up).y / transform.up.y);
        }
        
        prevVel = GetComponent<Rigidbody2D>().linearVelocity;
    }

    public bool overGPlaneToDeath() {
        return currentGs.magnitude > planeDestroyingGs;
    }

    public bool overGPlane() {
        return currentGs.magnitude > planeStructDestroyingGs;
    }

    public bool overGPersonToDeath() {
        return currentGs.magnitude > killingGs;
    }

    public bool overGPerson() {
        return Mathf.Abs(feltGs) > Mathf.Abs(sleepyGs);
    }

    public bool isPersonSleepy() {
        return sleepy;
    }

    public float howSleepyIsPerson() {
        if (sleepy) return 1f;
        return inGlocTimer / timeToGloc;
    }
}
