using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Utils;
using Unity.Netcode;

public class DamageModel : NetworkBehaviour {

    private string effect;
    [SerializeField] private bool assym;
    [SerializeField] private AnimationCurve hitChanceByAngle;
    [SerializeField] protected float maxHealth;
    [SerializeField] private float health;
    [SerializeField] private bool crewRole;
    [SerializeField] private bool criticalSystem;
    [SerializeField] private GameObject idealDestructiveEffect;
    private GameObject destructiveEffect;
    [SerializeField] private Sprite replacementSprite;
    private float startingValOfEffect;
    [SerializeField] private float effectivenessFalloffRate;

    [Header("Engine")]
    [SerializeField] private float fireDamagePerSec;

    [Header("Fuel")]
    [SerializeField] private float fuelFireDamagePerSec;
    [SerializeField] private float fuelFireFuelConsumptionPerSec;
    [SerializeField] private float brokenTankLeekRate;
    [SerializeField] private float damageToLeekRate;

    [Header("Tail")]
    [SerializeField] private GameObject tailPos;
    [SerializeField] private GameObject tailNeg;

    [Header("Wing")]
    [SerializeField] private float ripSpeed;
    [SerializeField] private GameObject wingPos;
    [SerializeField] private GameObject wingNeg;
    [SerializeField] private float animatorSpeedFactor;

    protected float screenShakeFactor = 10f;
    private float speedScreenShakeFactor = 1f / 200f;

    private Aerodynamics aero;
    private bool effectApplied;
    private float drowningDps = 0f;

    private List<GameObject> otherDamageModels;

    public bool isCrewRole() {
        return crewRole;
    }

    public bool isCritical() {
        return criticalSystem;
    }

    void Awake() {
        health = maxHealth;
        if (!assym) {
            wingNeg = wingPos;
            tailNeg = tailPos;
        }
    }

    void Start() {
        otherDamageModels = allObjectsInTreeWith<DamageModel>(gameObject);

        aero = transform.parent.GetComponent<Aerodynamics>();
        effect = gameObject.name.Replace("Hitbox", "");
        switch(effect) {
            case "Tail":
                startingValOfEffect = aero.getBaseTorque();
                break;

            case "Wing":
                startingValOfEffect = aero.getWingArea();
                break;

            case "Engine":
                startingValOfEffect = GetComponent<EngineScript>().getVal();
                break;
        }
    }

    float prevHealth;
    void Update() {
        if (health > maxHealth) health = maxHealth;
        if (health <= 0) {
            if (transform.parent.GetComponent<ObjectOnVehicleScript>() != null) transform.parent.GetComponent<ObjectOnVehicleScript>().kill();
            if (idealDestructiveEffect != null && !effectApplied) {
                effectApplied = true;
                destructiveEffect = Instantiate(idealDestructiveEffect, transform.position, Quaternion.identity, transform);
            }
            if (GetComponent<SpriteRenderer>() != null) GetComponent<SpriteRenderer>().sprite = replacementSprite;
            switch (effect) {
                case "Wing":
                    transform.parent.GetComponent<Animator>().speed = Mathf.Min(transform.parent.GetComponent<Rigidbody2D>().linearVelocity.magnitude / animatorSpeedFactor, 2f);
                    aero.setBaseTorque(0);
                    break;

                case "Tail":
                    aero.setSpeedOfControlEff(Mathf.Infinity);
                    if (transform.parent.GetComponent<Rigidbody2D>().linearVelocity.magnitude > 5f) {
                        if (transform.parent.GetComponent<Rigidbody2D>().angularVelocity > 0) {
                            transform.parent.GetComponent<Rigidbody2D>().angularVelocity = Mathf.Min(5f * Mathf.Pow(transform.parent.GetComponent<Rigidbody2D>().linearVelocity.magnitude, 1f), 360f);
                        } else {
                            transform.parent.GetComponent<Rigidbody2D>().angularVelocity = Mathf.Max(-5f * Mathf.Pow(transform.parent.GetComponent<Rigidbody2D>().linearVelocity.magnitude, 1f), -360f);
                        }
                    }
                    aero.setWingArea(0f);
                    break;

                case "Engine":
                    if (destructiveEffect == null) break;
                    if (destructiveEffect.transform.childCount == 0) break;
                    foreach (GameObject damageModel in allObjectsInTreeWith<DamageModel>(gameObject)) {
                        damageModel.GetComponent<DamageModel>().damage(fireDamagePerSec * Time.deltaTime, false);
                    }
                    break;

                case "Fuel":
                    GetComponent<FuelTankScript>().setLeekRate(brokenTankLeekRate);
                    if (destructiveEffect == null) break;
                    if (destructiveEffect.transform.childCount == 0) break;
                    foreach (GameObject damageModel in allObjectsInTreeWith<DamageModel>(gameObject)) {
                        damageModel.GetComponent<DamageModel>().damage(fuelFireDamagePerSec * Time.deltaTime, false);
                    }
                    GetComponent<FuelTankScript>().setLeekRate(fuelFireFuelConsumptionPerSec);
                    break;
            }   
        } else {
            effectApplied = false;
        }

        if (effect == "Wing") {
            if (transform.parent.GetComponent<Rigidbody2D>().linearVelocity.magnitude > ripSpeed) {
                kill();
            } else {
                if (transform.parent.GetComponent<Rigidbody2D>().linearVelocity.magnitude > ripSpeed * .9f) {
                    if (GameObject.Find("Camera").GetComponent<CamScript>().getControlledOrSpectatedVehicle() == maxAncestor(gameObject)) GameObject.Find("Camera").GetComponent<CamScript>().shakeScreen(.1f, (transform.parent.GetComponent<Rigidbody2D>().linearVelocity.magnitude - ripSpeed * .9f) * speedScreenShakeFactor);
                }
            }
        }

        if (effect == "Engine") { 

        }

        if (health != prevHealth) {
            onHealthChange();
        }
        prevHealth = health;
    }

    public void onHealthChange() {
        damage(0);
    }

    public void drown() {
        if (drowningDps != 0f) damage(drowningDps * Time.deltaTime);
    }

    public float getHitChance(float angle) {
        return hitChanceByAngle.Evaluate(angle % 360f);
    }

    public void setHitChances(AnimationCurve hitChances) {
        hitChanceByAngle = hitChances;
    }

    public void damage(float amt) {damage(amt, true);}
    public void damage(float amt, bool shakeScreen) {
        if (GameObject.Find("Camera").GetComponent<CamScript>().getControlledOrSpectatedVehicle() == maxAncestor(gameObject) && shakeScreen) GameObject.Find("Camera").GetComponent<CamScript>().shakeScreen(.1f, amt / maxAncestor(gameObject).GetComponent<Rigidbody2D>().mass * screenShakeFactor);
        if (IsServer && GameObject.Find("NetworkManager") != null) {
            health -= amt;
            serverHitSetHealthRpc(health);
            whackThisManRpc();
            return;
        } else if (GameObject.Find("NetworkManager") == null) {
            health -= amt;
        }
        
        switch(effect) {
            case "Tail":
                if (health <= 0 && !transform.parent.GetComponent<Animator>().GetBool("Tailless")) {
                    handleSpawningTail();
                }
                break;

            case "Wing":
                if (health / maxHealth < Random.Range(0f, .5f)) {
                    if (transform.parent.Find("Gear") != null) transform.parent.Find("Gear").GetComponent<GearScript>().breakGear();
                }
                if (health / maxHealth < Random.Range(0f, .75f)) {
                    if (transform.parent.Find("Flaps") != null) transform.parent.Find("Flaps").GetComponent<FlapScript>().breakFlaps();
                }
                if (health <= 0 && !transform.parent.GetComponent<Animator>().GetBool("Wingless")) {
                    handleSpawningWing();
                }
                break;

            case "Engine":
                break;
        }

        switch(effect) {
            case "Tail":
                if (findOtherOfEffect("Wing").GetComponent<DamageModel>().isAlive()) aero.setBaseTorque(health <= 0 ? 0 : startingValOfEffect * (1 - ((maxHealth - health) * effectivenessFalloffRate / maxHealth)));
                break;

            case "Wing":
                aero.setWingArea(health <= 0 ? 0 : startingValOfEffect * (1 - ((maxHealth - health) * effectivenessFalloffRate / maxHealth)));
                break;

            case "Engine":
                GetComponent<EngineScript>().setVal(health <= 0 ? 0 : startingValOfEffect * (1 - ((maxHealth - health) * effectivenessFalloffRate / maxHealth)));
                break;

            case "Fuel":
                GetComponent<FuelTankScript>().addLeekRate(amt * damageToLeekRate);
                break;
        }
    }

    [Rpc(SendTo.Everyone)]
    void serverHitSetHealthRpc(float val) {
        health = val;
    }
    
    [Rpc(SendTo.Everyone)]
    void whackThisManRpc() {      
        switch(effect) {
            case "Tail":
                if (health <= 0 && !transform.parent.GetComponent<Animator>().GetBool("Tailless")) {
                    handleSpawningTail();
                }
                break;

            case "Wing":
                if (health / maxHealth < Random.Range(0f, .5f)) {
                    if (transform.parent.Find("Gear") != null) transform.parent.Find("Gear").GetComponent<GearScript>().breakGear();
                }
                if (health / maxHealth < Random.Range(0f, .75f)) {
                    if (transform.parent.Find("Flaps") != null) transform.parent.Find("Flaps").GetComponent<FlapScript>().breakFlaps();
                }
                if (health <= 0 && !transform.parent.GetComponent<Animator>().GetBool("Wingless")) {
                    handleSpawningWing();
                }
                break;

            case "Engine":
                break;
        }

        switch(effect) {
            case "Tail":
                if (findOtherOfEffect("Wing").GetComponent<DamageModel>().isAlive()) aero.setBaseTorque(health <= 0 ? 0 : startingValOfEffect * (1 - ((maxHealth - health) * effectivenessFalloffRate / maxHealth)));
                break;

            case "Wing":
                aero.setWingArea(health <= 0 ? 0 : startingValOfEffect * (1 - ((maxHealth - health) * effectivenessFalloffRate / maxHealth)));
                break;

            case "Engine":
                GetComponent<EngineScript>().setVal(health <= 0 ? 0 : startingValOfEffect * (1 - ((maxHealth - health) * effectivenessFalloffRate / maxHealth)));
                break;
        }
    }

    private GameObject findOtherOfEffect(string effect) {
        foreach (GameObject g in otherDamageModels) {
            if (g == null) continue;
            if (g.GetComponent<DamageModel>().effect == effect) return g;
        }
        return null;
    }

    private void handleSpawningTail() {
        GameObject obj = Instantiate(transform.parent.localScale.y > 0 ? tailPos : tailNeg, transform.position, transform.rotation);
        obj.GetComponent<Rigidbody2D>().linearVelocity = transform.parent.GetComponent<Rigidbody2D>().linearVelocity;
        obj.transform.localScale = transform.parent.localScale;
        transform.parent.GetComponent<Animator>().SetBool("Tailless", true);

        maxAncestor(gameObject).transform.Find("CoM").position += transform.right * GetComponent<BoxCollider2D>().size.x / 4f;
        transform.parent.GetComponent<BoxCollider2D>().size += new Vector2(-GetComponent<BoxCollider2D>().size.x, 0f);
        transform.parent.GetComponent<BoxCollider2D>().offset += new Vector2(GetComponent<BoxCollider2D>().size.x / 2, 0f);

        Destroy(obj, 10f);
    }

    private void handleSpawningWing() {
        GameObject obj = Instantiate(transform.parent.localScale.y > 0 ? wingPos : wingNeg, transform.position, transform.rotation);
        obj.GetComponent<Rigidbody2D>().linearVelocity = transform.parent.GetComponent<Rigidbody2D>().linearVelocity;
        obj.transform.localScale = transform.parent.localScale;
        transform.parent.GetComponent<Animator>().SetBool("Wingless", true);
        handlePropellerOn(obj);
        
        Destroy(obj, 10f);
    }

    private void handlePropellerOn(GameObject obj) {
        if (obj.transform.childCount != 0) {
            int fallenPropIndex = 0;
            for (int i = 0; i < transform.parent.childCount; i++) {
                GameObject possibleProp = transform.parent.GetChild(i).gameObject;
                if (possibleProp.GetComponent<PropellerScript>() != null) {
                    if (possibleProp.GetComponent<PropellerScript>().isPropOfFallenWing()) {
                        possibleProp.GetComponent<PropellerScript>().enabled = false;
                        possibleProp.GetComponent<Animator>().enabled = false;
                        possibleProp.transform.position = obj.transform.GetChild(fallenPropIndex).position;
                        possibleProp.transform.parent = obj.transform.GetChild(fallenPropIndex);
                        fallenPropIndex++;
                        i--;
                    }
                }
            }
        }
    }

    public void kill() {
        damage(health);
    }

    public void repair() {
        damage(health - maxHealth);
    }

    public bool isAlive() {
        return health > 0;
    }

    public float getHealth() {
        return health;
    }

    public float getMaxHealth() {
        return maxHealth;
    }

    public float healthAsDecimal() {
        return health / maxHealth;
    }
}
