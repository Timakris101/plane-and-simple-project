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
        if (transform.Find("Camera") != null) transform.Find("Camera").GetComponent<CamScript>().uncoupleCam();
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
        if (INPUTS.transform.parent != transform) return 0;
        switch (PlayerPrefs.GetString("ControlMode")) {
            case "Joystick": 
                return INPUTS.directionInput();
            case "Touch":
                Vector3 screenToWorld = INPUTS.pointerPositionInput();
                float input = GetComponent<AiPlaneController>().pointTowards(screenToWorld);
                INPUTS.directionInputObj().GetComponent<SliderControl>().setVal(input);
                return input;
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
        if (INPUTS.transform.parent != transform) return;
        if (INPUTS.GetComponent<CustomInputs>().ejectInput()) {
            GetComponent<BailoutHandler>().callBailOut();
        }
    }

    protected virtual void handleSwapping() {
        if (INPUTS == null) return;
        if (INPUTS.transform.parent != transform) return;
        if (INPUTS.GetComponent<CustomInputs>().swapViewInput()) {
            toggleGunners();
        }
    }

    protected virtual void handleControls() {
        if (INPUTS == null) return;
        if (INPUTS.transform.parent != transform) return;

        setThrottle(INPUTS.GetComponent<CustomInputs>().throttleInput());

        inWEP = INPUTS.GetComponent<CustomInputs>().wepInput();

        if (INPUTS.GetComponent<CustomInputs>().engineInput()) toggleEngines();

        if (transform.Find("Flaps") != null) {
            if (INPUTS.GetComponent<CustomInputs>().flapInput()) transform.Find("Flaps").GetComponent<FlapScript>().toggleFlaps();
        }

        if (transform.Find("Gear") && !onGround) {
            if (INPUTS.GetComponent<CustomInputs>().gearInput()) {
                foreach (GameObject gear in gears) {
                    if (gear != null) gear.GetComponent<GearScript>().toggleGear();
                }
            }
        }
        if (transform.Find("Gear")) {
            if (INPUTS.GetComponent<CustomInputs>().brakeInput()) transform.Find("Gear").GetComponent<GearScript>().brake();
        }

        if (checkForGunAmmo()) setGuns(INPUTS.GetComponent<CustomInputs>().gunInput());
        if (checkForBombAmmo()) setBombs(INPUTS.GetComponent<CustomInputs>().bombInput());
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
