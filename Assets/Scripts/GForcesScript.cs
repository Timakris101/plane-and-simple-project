using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;
using static Utils;

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
    [SerializeField] private Sprite[] rolloverAllowingSprites;

    [Header("DestructiveEffects")]
    [SerializeField] private GameObject fire;
    [SerializeField] private GameObject explosion;
    private bool destroyed = false;

    bool justRolledOver = false;
    int counterPastRollover = 0;
    
    void FixedUpdate() {
        updateSleepy();
        calculateGs();
    }

    void Update() {
        for (int i = 0; i < GetComponent<Animator>().parameterCount; i++) {
            if (GetComponent<Animator>().GetParameter(i).name == "yScale") GetComponent<Animator>().SetInteger("yScale", (int) transform.localScale.y);
        }
        
        if (ableToRollover()) {
            rollover();
            justRolledOver = true;
        }
        if (justRolledOver) {
            counterPastRollover++;
        }
        if (counterPastRollover == 10) {
            justRolledOver = false;
            counterPastRollover = 0;
        }
        if (overGPlaneToDeath() && !destroyed) {
            destroyed = true;
            GameObject effect = Instantiate(explosion, transform.position, Quaternion.identity);
            Destroy(effect, 10f);
            //Instantiate(fire, transform, false);
            GetComponent<Aerodynamics>().setSpeedOfControlEff(Mathf.Infinity);
            if (SceneManager.GetActiveScene().name == "Arcade") Destroy(gameObject, 10f);
            
            foreach (GameObject dm in progenyWithScript<DamageModel>(gameObject)) {
                dm.GetComponent<DamageModel>().kill();
            }
        }
        if (overGPlane() && !justRolledOver) {
            if (transform.Find("WingHitbox") != null) transform.Find("WingHitbox").GetComponent<DamageModel>().kill();
            //if (transform.Find("TailHitbox") != null) transform.Find("TailHitbox").GetComponent<DamageModel>().kill();
        }
        if (overGPersonToDeath()) {
            foreach (GameObject dm in progenyWithScript<DamageModel>(gameObject)) {
                if (!dm.GetComponent<DamageModel>().isCrewRole()) continue;
                dm.GetComponent<DamageModel>().kill();
            }
        }
    }

    public bool ableToRollover() {
        return (feltGs < rollOverThresh || (progenyWithScript<CamScript>(gameObject).Count > 0 ? (progenyWithScript<CamScript>(gameObject)[0].GetComponent<CustomInputs>().rotateVehicleInput()) : false)) && GetComponent<Rigidbody2D>().linearVelocity.magnitude > minRolloverSpeed && !GetComponent<PlaneController>().pilotDeadOrGone() && !sleepy && rolloverAllowingSprites.Contains(GetComponent<SpriteRenderer>().sprite);
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

    public void rollover() {
        transform.localScale = new Vector3(transform.localScale.x, transform.localScale.y * -1, transform.localScale.z);
        GetComponent<Animator>().SetTrigger("Rollover");
    }

    private void calculateGs() {
        if (prevVel.magnitude != 0f) {
            Vector3 curVel = GetComponent<Rigidbody2D>().linearVelocity;
            Vector3 currentForces = (curVel - prevVel) / Time.fixedDeltaTime / 9.8f;

            if (currentForces.magnitude != 0) currentGs = transform.localScale.y * (currentForces + Vector3.up);
            feltGs = Vector3.Dot(currentGs, transform.up);
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
