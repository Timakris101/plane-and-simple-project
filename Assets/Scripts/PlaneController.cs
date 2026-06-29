using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Utils;
using Unity.Netcode;

public class PlaneController : VehicleController {
    protected bool inWEP;
    public float throttleChangeSpeed = 1f;
    private bool pilotDead => !transform.Find("PilotHitbox").GetComponent<DamageModel>().isAlive();
    private bool pilotGone => transform.Find("PilotHitbox") == null;
    private bool unconcious => GetComponent<GForcesScript>().isPersonSleepy();
    private bool onGround;
    private EngineScript es;

    protected List<GameObject> gears;

    void OnCollisionStay2D() {
        onGround = true;
    }

    void OnCollisionExit2D() {
        onGround = false;
    }

    void OnEnable() {
        setGunnersToManual(false);
    }

    public override bool whenToRemoveCamera() {return pilotDeadOrGone();}

    public override bool vehicleDead() {
        bool criticalSystemDamage = false;
        foreach (GameObject d in damageModels) {
            if (d == null) continue;
            if (!d.GetComponent<DamageModel>().isCrewRole() && d.GetComponent<DamageModel>().isCritical()) {
                if (!d.GetComponent<DamageModel>().isAlive()) {
                    criticalSystemDamage = true;
                    break;
                }
            }
        }
        if (allCrewGoneFromVehicle()) return true;
        return criticalSystemDamage || pilotDeadOrGone();
    }


    new void Awake() {
        base.Awake();
        es = progenyWithScript<EngineScript>(gameObject)[0].GetComponent<EngineScript>();
        gears = progenyWithScript<GearScript>(gameObject);
    }

    public void removeCam() {
        GameObject.Find("Camera").GetComponent<CamScript>().uncoupleCam();
    }

    public bool pilotDeadOrGone() {
        if (pilotGone) {
            return true;
        }
        return pilotDead;
    }

    public float getDir() {
        if (!pilotDeadOrGone()/* && (IsOwner || GameObject.Find("NetworkManager") == null)*/) {
            if (altitudeFromTerrain() == Mathf.Infinity) {
                if (Vector3.Dot(transform.right, Vector3.up) <= 0 && transform.position.y < GetComponent<AiPlaneController>().getMinAlt() + Constants.Water.seaLevel) {
                    return GetComponent<AiPlaneController>().pointTowards(transform.position + Vector3.up + Vector3.right * Mathf.Clamp(transform.position.x, -1, 1));
                } else {
                    return GetComponent<AiPlaneController>().pointTowards(new Vector3(0, transform.position.y ,0));
                }
            }
            if (gunnersAreManual()) {
                GetComponent<AiPlaneController>().Update();
                return GetComponent<AiPlaneController>().wantedDir() * (unconcious ? Constants.GForceEffectConstants.unconciousPilotEffectiveness : 1f);
            } else {
                return wantedDir() * (unconcious ? Constants.GForceEffectConstants.unconciousPilotEffectiveness : 1f);
            }
        }
        return 0;
    }

    public float altitudeFromTerrain() {
        float altitude = Mathf.Infinity;
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector3.down, Mathf.Abs(transform.position.y), LayerMask.GetMask("Terrain"));
        if (hit) {
            altitude = (hit.point - (Vector2) transform.position).magnitude;
        }
        return altitude;
    }

    protected virtual float wantedDir() {
        if (INPUTS == null) return 0;
        if (!Object.Equals(GameObject.Find("Camera").GetComponent<CamScript>().getControlledVehicle(), gameObject)) return 0f;
        switch (PlayerPrefs.GetString("ControlMode", "Joystick")) {
            case "Joystick": 
                return INPUTS.directionInput();
            case "Joystick1":
                float desiredTheta = INPUTS.directionInput1().y;
                Vector3 control = transform.position + new Vector3(Mathf.Cos(desiredTheta), Mathf.Sin(desiredTheta), 0f);
                float input1 = GetComponent<AiPlaneController>().pointTowards(control);
                if (INPUTS.directionInput1().x == 0) input1 = 0;
                return input1;
            default:
                return 0f;
        }
    }

    private float oobCounter;
    public override void handleFeasibleControls() {
        if (!pilotDeadOrGone() && !unconcious/* && (IsOwner || GameObject.Find("NetworkManager") == null)*/) {
            if (gunnersAreManual()) {
                GetComponent<AiPlaneController>().handleControls();
            } else {
                handleControls();
            }
        }
        if (altitudeFromTerrain() == Mathf.Infinity) {
            oobCounter += Time.deltaTime;
            if (oobCounter > 20) GetComponent<BailoutHandler>().callBailOut();
        } else {
            oobCounter = 0;
        }
        if (pilotDeadOrGone()) setGuns(false);
        
        if (!allCrewGoneFromVehicle()) {
            handleNonPilotControls();
            handleSwapping();
        }
    }

    protected virtual void handleNonPilotControls() {
        if (INPUTS == null) return;
        if (!Object.Equals(GameObject.Find("Camera").GetComponent<CamScript>().getControlledVehicle(), gameObject)) return;
        if (INPUTS.ejectInput()) {
            GetComponent<BailoutHandler>().callBailOut();
        }
    }

    protected virtual void handleSwapping() {
        if (INPUTS == null) return;
        if (!Object.Equals(GameObject.Find("Camera").GetComponent<CamScript>().getControlledVehicle(), gameObject)) return;
        if (INPUTS.swapViewInput()) {
            toggleGunners();
        }
    }

    protected virtual void handleControls() {
        if (INPUTS == null) return;
        if (!Object.Equals(GameObject.Find("Camera").GetComponent<CamScript>().getControlledVehicle(), gameObject)) return;

        setThrottle(INPUTS.throttleInput());

        inWEP = INPUTS.wepInput();

        if (INPUTS.engineInput()) toggleEngines();

        if (transform.Find("Flaps") != null) {
            if (INPUTS.flapInput()) transform.Find("Flaps").GetComponent<FlapScript>().toggleFlaps();
        }

        if (transform.Find("Gear") && !onGround) {
            if (INPUTS.gearInput()) {
                foreach (GameObject gear in gears) {
                    if (gear != null) gear.GetComponent<GearScript>().toggleGear();
                }
            }
        }
        if (transform.Find("Gear")) {
            if (INPUTS.brakeInput()) transform.Find("Gear").GetComponent<GearScript>().brake();
        }

        if (checkForGunAmmo()) setGuns(INPUTS.gunInput());
        if (checkForBombAmmo()) setBombs(INPUTS.bombInput());
    }

    protected bool checkForGunAmmo() {
        foreach (GameObject gun in guns) {
            if (gun == null) continue;
            if (gun.GetComponent<BombHolderScript>()) continue;
            if (gun.transform.parent != transform) continue;
            if (gun.GetComponent<GunScript>().getAmmo() != 0) return true;
        }
        return false;
    }

    protected bool checkForBombAmmo() {
        foreach (GameObject bh in bombHolders) {
            if (bh.GetComponent<BombHolderScript>().getAmmo() != 0) return true;
        }
        return false;
    }

    protected void setGuns(bool shooting) {
        foreach (GameObject gun in guns) {
            if (gun == null) continue;
            if (gun.transform.parent != transform) continue;
            gun.GetComponent<GunScript>().setShooting(shooting);
        }
        foreach (GameObject bh in bombHolders) {
            bh.GetComponent<BombHolderScript>().setShooting(false);
        }
    }

    protected void setBombs(bool bombing) {
        foreach (GameObject bh in bombHolders) {
            bh.GetComponent<BombHolderScript>().setShooting(false);
        }
        foreach (GameObject bh in bombHolders) {
            if (bh.GetComponent<BombHolderScript>().getAmmo() != 0) {
                bh.GetComponent<BombHolderScript>().setShooting(bombing);

                if (bh.GetComponent<BombHolderScript>().getAmmo() == 1) {
                    resetTimerOfBombholdersExcept(bh);
                }

                break;
            }
        }
    }

    private void resetTimerOfBombholdersExcept(GameObject curBh) {
         foreach (GameObject bh in bombHolders) {
            if (curBh == bh) continue;
            bh.GetComponent<BombHolderScript>().setTimer(0);
        }
    }

    public void toggleEngines() {
        es.setEngines(!es.getEnginesOn());
    }

    public void setEngines(bool b) {
        es.setEngines(b);
    }

    public void setThrottle(float val) {
        es.setThrottle(val);
    }

    public float getThrottle() {
        return es.getThrottle();
    }

    public bool getEnginesOn() {
        return es.getEnginesOn();
    }

    public bool getInWEP() {
        return inWEP;
    }
}
