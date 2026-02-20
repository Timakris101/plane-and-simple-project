using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Utils;
 
public class AiPlaneController : PlaneController {
    [SerializeField] private float angularThreshForGuns;
    private float sixAngle = 45f;
    [SerializeField] private string mode;
    protected float minAltitude = 100f;
    [SerializeField] protected float gunRange;
    private float headonOscillationMagnitude = 10f;
    protected GameObject primaryBullet;
    protected bool isBomber = false;
    private List<GameObject> squadronList = new List<GameObject>();
    private Vector2 offset;
    private GameObject squadronLead;

    public void setSquadronList(List<GameObject> list) {
        squadronList = list;
    }

    public void setOffset(Vector2 offset) {
        this.offset = offset;
    }

    public GameObject squadronLeader() {
        for (int i = 0; i < squadronList.Count; i++) {
            if (squadronList[i].GetComponent<VehicleController>().vehicleDead()) {
                squadronList.Remove(squadronList[i]);
                i--;
            }
        }
        return squadronList.Count != 0 ? squadronList[0] : gameObject;
    }

    void Start() {
        squadronLead = squadronLeader();
        base.Start();
    }

    void Update() {
        if (squadronLead == null) squadronLead = squadronLeader();
        if ((frameCounter + (int) ((float) (index) * ((float) framesBeforeTargetUpdate / (float) allVehicles.Length))) % (framesBeforeTargetUpdate) == 0 || squadronLead.GetComponent<VehicleController>().vehicleDead()) {
            squadronLead = squadronLeader();
        }
        base.Update();
    }

    protected override float wantedDir() {
        primaryBullet = null;
        foreach (GameObject gunOrBh in guns) {
            if (gunOrBh.transform.parent == transform) {
                isBomber = gunOrBh.TryGetComponent<BombHolderScript>(out BombHolderScript component);
                primaryBullet = gunOrBh.GetComponent<GunScript>().getBullet();
                break;
            }
        }
        
        if (altitudeFromTerrain() < minAltitude) return pointTowards(transform.position + Vector3.up);

        if (targetedObj == null/* || targetedObj.GetComponent<Rigidbody2D>().linearVelocity.magnitude < 1f*/) {
            mode = "formation";
            if (squadronLead == gameObject || squadronLead == null) return pointTowards(transform.position + Vector3.Project(transform.right, Vector3.right));
            return pointTowards(squadronLead.transform.position + (Vector3) offset * squadronList.IndexOf(gameObject) + squadronLead.transform.right * 100f);
        }

        if (primaryBullet == null || isBomber) return pointTowards(transform.position + Vector3.Project(transform.right, Vector3.right));

        if (Mathf.Abs(angleTo(targetedObj.transform.position)) > 180f - sixAngle && Mathf.Abs(Vector2.SignedAngle(targetedObj.transform.right, transform.right)) < 90f) {
            mode = "defensive";
        } else {
            mode = "pursuit";
        }

        if (Mathf.Abs(Vector2.SignedAngle(targetedObj.transform.right, transform.right)) > 135f && Mathf.Abs(angleTo(targetedObj.transform.position)) < sixAngle) {
            mode = "headon";
        }

        // if (mode == "defensive" && Vector3.Project(targetedObj.transform.position - transform.position, Vector3.up).y < 0 && GetComponent<Rigidbody2D>().linearVelocity.magnitude > targetedObj.GetComponent<Rigidbody2D>().linearVelocity.magnitude) mode = "hammerhead";

        if ((mode == "defensive" || mode == "hammerhead") && GetComponent<Rigidbody2D>().linearVelocity.magnitude < targetedObj.GetComponent<Rigidbody2D>().linearVelocity.magnitude) mode = "overshoot";

        if (mode == "hammerhead") {
            if (targetedObj.GetComponent<Rigidbody2D>().linearVelocity.magnitude < 15f || GetComponent<Rigidbody2D>().linearVelocity.magnitude < 10f || Mathf.Abs(Vector2.SignedAngle(targetedObj.transform.right, transform.right)) > 45f || targetedObj.GetComponent<Rigidbody2D>().linearVelocity.magnitude > GetComponent<Rigidbody2D>().linearVelocity.magnitude) {
                mode = "pursuit";
            } else {
                return pointTowards(transform.position + Vector3.up);
            }
        }

        if (mode == "pursuit" || mode == "overshoot" || mode == "defensive") return pointTowards(positionToTarget(primaryBullet, transform.right));

        if (mode == "headon") return pointTowards(positionToTarget(primaryBullet, transform.right) + Vector3.Cross(transform.forward, positionToTarget(primaryBullet, transform.right) - transform.position) * Random.Range(-headonOscillationMagnitude, headonOscillationMagnitude));

        return 0;
    }

    public float pointTowards(Vector3 pos) {
        return Mathf.Clamp(Mathf.Pow(angleTo(pos), 3), -1, 1);
    }

    private float angleTo(Vector3 pos) {
        return Vector3.SignedAngle((pos - transform.position).normalized, transform.right, transform.forward);
    }

    protected override void handleNonPilotControls() {
        bool criticalSystemDestroyed = false;
        foreach (GameObject d in damageModels) {
            if (d == null) continue;
            if (d.GetComponent<DamageModel>().isCritical()) {
                if (!d.GetComponent<DamageModel>().isAlive()) {
                    criticalSystemDestroyed = true;
                    break;
                }
            }
        }
        if (transform.Find("PilotHitbox") == null) return; //already bailed out
        if (criticalSystemDestroyed || GetComponent<Rigidbody2D>().linearVelocity.magnitude <= .1f || !transform.Find("PilotHitbox").GetComponent<DamageModel>().isAlive()) GetComponent<BailoutHandler>().callBailOut();
    }

    protected override void handleSwapping() {}

    protected override void handleControls() {
        setThrottle(1f);
        inWEP = false;
        if (mode == "overshoot") setThrottle(0f);
        
        if (squadronLead != null) {
            if (mode == "formation" && gameObject != squadronLead) {
                if (Vector3.Dot(transform.position - (squadronLead.transform.position + (Vector3) offset * squadronList.IndexOf(gameObject)), squadronLead.transform.right) > 0f) {
                    setThrottle(0f);
                    inWEP = false;
                } else {
                    setThrottle(1f);
                    inWEP = true;
                }
            }
        }

        if (targetedObj != null && primaryBullet != null) {
            if (targetInSights(primaryBullet) && (transform.position - positionToTarget(primaryBullet, transform.right)).magnitude < gunRange && !targetedObj.GetComponent<VehicleController>().vehicleDead()) {
                setGuns(true);
                if (isBomber) setBombs(true);
            } else {
                setGuns(false);
                if (isBomber) setBombs(false);
            }
        } else {
            setGuns(false);
            if (isBomber) setBombs(false);
        }
        if ((isBomber || mode == "formation") && Vector3.Dot(transform.up * transform.localScale.y, Vector3.up) < 0f && GetComponent<GForcesScript>().ableToRollover() && Mathf.Abs(wantedDir()) < .1f && !vehicleDead()) GetComponent<GForcesScript>().rollover();
    }

    public float getMinAlt() {
        return minAltitude;
    }

    protected bool targetInSights(GameObject bullet) {
        return Mathf.Abs(Vector3.SignedAngle((positionToTarget(bullet, transform.right) - transform.position).normalized, transform.right, transform.forward)) < angularThreshForGuns;
    }

    protected Vector3 positionToTarget(GameObject bullet, Vector3 gunDir) {
        Vector3 a = targetedObj.GetComponent<AccelerationHolder>().getAccel() - gameObject.GetComponent<AccelerationHolder>().getAccel();
        Vector3 v = targetedObj.GetComponent<Rigidbody2D>().linearVelocity - GetComponent<Rigidbody2D>().linearVelocity;
        Vector3 p = targetedObj.transform.position - transform.position;
        float s = bullet.GetComponent<BulletScript>().getInitSpeed();

        float[] coefficients = new float[5];
        coefficients[0] = (Mathf.Pow(a.x, 2f) + Mathf.Pow(a.y, 2f) + Mathf.Pow(a.z, 2f)) / 4f;
        coefficients[1] = (a.x * v.x + a.y * v.y + a.z * v.z);
        coefficients[2] = (Mathf.Pow(v.x, 2f) + p.x * a.x + Mathf.Pow(v.y, 2f) + p.y * a.y + Mathf.Pow(v.z, 2f) + p.z * a.z - Mathf.Pow(s, 2f));
        coefficients[3] = 2f * (p.x * v.x + p.y * v.y + p.z * v.z);
        coefficients[4] = (Mathf.Pow(p.x, 2f) + Mathf.Pow(p.y, 2f) + Mathf.Pow(p.z, 2f));

        float timeOfFlight = newtonRaphson(s == 0 ? 0f : p.magnitude / s, 5, 5f, coefficients);

        if (s == 0f && targetedObj.GetComponent<Rigidbody2D>().linearVelocity.magnitude < 15f) { //really hacky and dumb solution for bombs
            timeOfFlight = (targetedObj.transform.position - transform.position).x / ((Vector3) GetComponent<Rigidbody2D>().linearVelocity).x;
            return targetedObj.transform.position + (Vector3) (targetedObj.GetComponent<Rigidbody2D>().linearVelocity) * timeOfFlight - (Vector3) Physics2D.gravity * Mathf.Pow(timeOfFlight, 2) / 2f;
        } else if (s == 0f) {
            return transform.position - transform.right * Mathf.Infinity;
        }

        if (timeOfFlight == -Mathf.Infinity) {
            return targetedObj.transform.position;
        }

        return targetedObj.transform.position + (Vector3) v * timeOfFlight + (Vector3) (a - (Vector3) Physics2D.gravity) * Mathf.Pow(timeOfFlight, 2) / 2f;
    }
}
