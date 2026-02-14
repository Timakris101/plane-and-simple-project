using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Utils;
using Unity.Netcode;

public class BulletScript : NetworkBehaviour {
    [Header("Base Stats")]
    [SerializeField] private float initSpeed;
    [SerializeField] private float damage;
    [SerializeField] private float penetrationVal;
    [SerializeField] private float fragmentationAmt;
    [SerializeField] private float fragmentationStrength;
    [SerializeField] private float armorPenMeters;
    [SerializeField] private float armorFuseSec;
    [SerializeField] private float armingDist;
    [SerializeField] private float fuseTimeSec;
    [SerializeField] private GameObject effect;
    [SerializeField] private float lifeTime;
    [SerializeField] private float damageVariation;
    [SerializeField] private bool bulletRicochets;
    private float effectLifeTime = .2f;
    [SerializeField] private float timer;
    private float explosiveScreenShakeFactor = 1f / 5000f;
    
    [Header("Plane")]
    [SerializeField] private GameObject planeFired;

    void OnCollisionEnter2D(Collision2D col) {
        bool hit;
        dealDamage(col, out hit);
        if (!hit) return;

        Collider2D[] objsNearby = Physics2D.OverlapCircleAll(transform.position, penetrationVal);
        foreach (Collider2D collider in objsNearby) {
            if (collider.gameObject.GetComponent<Rigidbody2D>() == null) continue;
            if (collider.gameObject == gameObject) continue;
            Vector3 impulse = fragmentationStrength * (collider.transform.position - transform.position).normalized / Mathf.Pow(Mathf.Max((transform.position - collider.transform.position).magnitude, 1f), 2f);
            collider.transform.gameObject.GetComponent<Rigidbody2D>().AddForceAtPosition(impulse, transform.position, ForceMode2D.Impulse);
            if (GameObject.Find("Camera").GetComponent<CamScript>().getControlledOrSpectatedVehicle() == maxAncestor(collider.transform.gameObject)) GameObject.Find("Camera").GetComponent<CamScript>().shakeScreen(.1f, Mathf.Sqrt(fragmentationStrength / Mathf.Pow(Mathf.Max((transform.position - collider.transform.position).magnitude, 1f), 2f) * explosiveScreenShakeFactor));
        }
    }

    private Vector3 prevVel;
    private void FixedUpdate() {
        prevVel = GetComponent<Rigidbody2D>().linearVelocity;
    }

    void dealDamage(Collision2D col, out bool actuallyHit) {
        actuallyHit = false;

        int armorHitCount;
        float effectiveArmorPen;
        float newPenVal;
        GameObject objClosestToBullet;
        handleArmor(col, out armorHitCount, out effectiveArmorPen, out newPenVal, out objClosestToBullet);

        if (armorHitCount == 1 && effectiveArmorPen <= 0f && objClosestToBullet.GetComponent<ArmorScript>() != null && bulletRicochets) return;

        if (objClosestToBullet == null && col.transform.GetComponent<BoxCollider2D>() != null) {
            missObj(col);
            return;
        }  

        Vector3 hitPoint;
        if (didMiss(col, effectiveArmorPen, Mathf.Infinity, out hitPoint) && col.transform.GetComponent<BoxCollider2D>() != null) {
            missObj(col);
            return;
        }

        Vector3 posToStartFrom = (armorHitCount == 0 && hitPoint != new Vector3(0,0,0)) ? hitPoint : col.contacts[0].point;
        handleDamage(posToStartFrom, -col.relativeVelocity, armorFuseSec, effectiveArmorPen, newPenVal);
        makeEffectAndDestroyObj(posToStartFrom);

        actuallyHit = true;
    }

    void dealDamage() {
        float effectiveArmorPen = armorPenMeters * prevVel.magnitude / initSpeed;
        Vector3 beginningHitPos = transform.position - prevVel * Time.fixedDeltaTime;
        handleDamage(beginningHitPos, GetComponent<Rigidbody2D>().linearVelocity, armorFuseSec, effectiveArmorPen, penetrationVal);

        makeEffectAndDestroyObj(transform.position);
    }

    bool didMiss(Collision2D col, float effectiveArmorPen, float newPenVal, out Vector3 whereHit) {
        whereHit = new Vector3(0,0,0);
        RaycastHit2D[] hits = Physics2D.RaycastAll(col.contacts[0].point, -col.relativeVelocity, newPenVal);
        foreach (RaycastHit2D hit in hits) {
            if (hit.transform.gameObject != maxAncestor(col.gameObject)) continue;
            if (hit.collider.transform.GetComponent<DamageModel>() != null) {
                DamageModel d = hit.collider.transform.GetComponent<DamageModel>();
                if (!(Random.Range(0f, 1f) < (d.getHitChance(Vector3.Angle(col.relativeVelocity, d.transform.right))))) continue;
                whereHit = hit.point;
                return false;
            }
        }
        return true;
    }
    
    void missObj(Collision2D col) {
        col.transform.GetComponent<Rigidbody2D>().AddForceAtPosition(-(prevVel - (Vector3) GetComponent<Rigidbody2D>().linearVelocity) * GetComponent<Rigidbody2D>().mass, transform.position, ForceMode2D.Impulse);
        GetComponent<Rigidbody2D>().linearVelocity = prevVel;
        Physics2D.IgnoreCollision(gameObject.GetComponent<Collider2D>(), col.transform.GetComponent<Collider2D>());
    }

    void handleDamage(Collision2D col, float postHitFuse, float effectiveArmorPen, float newPenVal) {
        handleDamage(col.contacts[0].point, -col.relativeVelocity, postHitFuse, effectiveArmorPen, newPenVal);
    }

    void handleDamage(Vector3 beginningHitPos, Vector3 relativeVel, float postHitFuse, float effectiveArmorPen, float newPenVal) {
        BulletMessagePacket bulletMessagePacket = new BulletMessagePacket();

        for (int i = 0; i < fragmentationAmt; i++) {
            Vector2 fragmentationVector = i == 0 ? new Vector2(0,0) : new Vector2(Random.Range(-fragmentationStrength, fragmentationStrength), Random.Range(-fragmentationStrength, fragmentationStrength));
            Vector2 fragVecPlusVel = fragmentationVector + (Vector2) relativeVel;
            RaycastHit2D[] hits = Physics2D.RaycastAll(beginningHitPos + relativeVel * postHitFuse, fragVecPlusVel, newPenVal);
            Debug.DrawRay(beginningHitPos + relativeVel * postHitFuse, fragVecPlusVel.normalized * newPenVal, Color.red, 10f);
            foreach (RaycastHit2D hit in hits) {
                if (hit.collider.transform.GetComponent<DamageModel>() != null && hit.collider.transform.GetComponent<ArmorScript>() == null) {
                    DamageModel d = hit.collider.transform.GetComponent<DamageModel>();
                    if (!(Random.Range(0f, 1f) < (d.getHitChance(Vector3.Angle(-fragVecPlusVel, d.transform.right))))) continue;

                    float damageToModel = Random.Range((1f - damageVariation), (1f + damageVariation)) * damage * (fragmentationStrength == 0f ? (relativeVel.magnitude / initSpeed) : 1f);
                    d.damage(damageToModel);

                    bulletMessagePacket.addDamage(d, damageToModel);
                }
                if (hit.collider.transform.GetComponent<ArmorScript>() != null) {
                    float effArmorThickness = Mathf.Abs(hit.collider.transform.GetComponent<BoxCollider2D>().size.x / Mathf.Cos(Mathf.Deg2Rad * Vector3.Angle(prevVel, hit.normal)));
                    if (effArmorThickness < effectiveArmorPen) {
                        effectiveArmorPen -= effArmorThickness;
                    } else {
                        effectiveArmorPen = 0f;
                        break;
                    }
                }
            }
        }

        if (planeFired == null) return;
        if (nonAiControllerOfVehicle(planeFired) == null) return;
        if (nonAiControllerOfVehicle(planeFired).enabled && planeFired.GetComponent<BulletMessageReader>().enabled) planeFired.GetComponent<BulletMessageReader>().receivePacket(bulletMessagePacket);
    }

    void handleArmor(Collision2D col, out int armorHitCount, out float effectiveArmorPen, out float newPenVal, out GameObject objClosestToBullet) {
        Vector3 beginningHitPos = transform.position - prevVel * Time.fixedDeltaTime;
        RaycastHit2D[] hits = Physics2D.RaycastAll(beginningHitPos, -col.relativeVelocity);
        newPenVal = penetrationVal;
        armorHitCount = 0;
        int index = 0;
        effectiveArmorPen = armorPenMeters * col.relativeVelocity.magnitude / initSpeed;
        objClosestToBullet = null;
        foreach (RaycastHit2D hit in hits) {
            if (hit.transform == transform) continue;
            if (hit.transform.gameObject != maxAncestor(col.gameObject)) continue;

            if (objClosestToBullet == null) objClosestToBullet = hit.collider.gameObject;
            if ((hit.point - (Vector2) transform.position).magnitude < (objClosestToBullet.transform.position - transform.position).magnitude) {
                objClosestToBullet = hit.collider.gameObject;
            }

            if (hit.collider.transform.GetComponent<ArmorScript>() != null) {
                float effArmorThickness = Mathf.Abs(hit.collider.transform.GetComponent<BoxCollider2D>().size.x / Mathf.Cos(Mathf.Deg2Rad * Vector3.Angle(col.relativeVelocity, hit.normal)));
                armorHitCount++;

                float shakeToVehicle = GetComponent<Rigidbody2D>().mass * GetComponent<Rigidbody2D>().linearVelocity.magnitude * Mathf.Cos(Mathf.Deg2Rad * Vector3.Angle(col.relativeVelocity, hit.normal));
                hit.collider.transform.GetComponent<ArmorScript>().damage(shakeToVehicle);

                if (effArmorThickness < effectiveArmorPen) {
                    effectiveArmorPen -= effArmorThickness;
                } else {
                    effectiveArmorPen = 0f;
                    newPenVal = ((Vector3) hit.point - beginningHitPos).magnitude;
                    break;
                }
            }
            index++;
        }
    }

    private void makeEffectAndDestroyObj(Vector3 effectPos) {
        GameObject newEffect = GameObject.Find("MultiplayerCreateDestroy") != null ? GameObject.Find("MultiplayerCreateDestroy").GetComponent<MultiplayerCreateAndDestroy>().create(effect, effectPos, Quaternion.identity) : Instantiate(effect, effectPos, Quaternion.identity);
        if (newEffect != null) {
            var mainModule = newEffect.GetComponent<ParticleSystem>().main;
            if (mainModule.startSpeed.constantMax == 0) mainModule.startSpeed = new ParticleSystem.MinMaxCurve(0, penetrationVal / mainModule.startLifetime.constant);

            if (GameObject.Find("MultiplayerCreateDestroy") != null) {
                GameObject.Find("MultiplayerCreateDestroy").GetComponent<MultiplayerCreateAndDestroy>().destroy(newEffect, effectLifeTime);
            } else {
                Destroy(newEffect, lifeTime);
            }
        }
        if (GameObject.Find("MultiplayerCreateDestroy") != null) {
            GameObject.Find("MultiplayerCreateDestroy").GetComponent<MultiplayerCreateAndDestroy>().destroy(gameObject);
        } else {
            Destroy(gameObject);
        }
    }

    public void setPlaneFired(GameObject plane) {
        planeFired = plane;
    }

    void Start() {
        if (GameObject.Find("MultiplayerCreateDestroy") != null) {
            GameObject.Find("MultiplayerCreateDestroy").GetComponent<MultiplayerCreateAndDestroy>().destroy(gameObject, lifeTime);
            if (IsServer) {
                GetComponent<Collider2D>().enabled = true;
            } else {
                GetComponent<Collider2D>().enabled = false;
            }
        } else {
            Destroy(gameObject, lifeTime);
        }
        collisionToPlaneFired(false);
    }

    public void setFuseTime(float sec) {
        fuseTimeSec = sec;
    }

    void collisionToPlaneFired(bool collide) {
        Physics2D.IgnoreCollision(planeFired.GetComponent<Collider2D>(), GetComponent<Collider2D>(), !collide);
        for (int i = 0; i < planeFired.transform.childCount; i++) {
            if (planeFired.transform.GetChild(i).GetComponent<Collider2D>() != null) Physics2D.IgnoreCollision(planeFired.transform.GetChild(i).GetComponent<Collider2D>(), GetComponent<Collider2D>(), !collide);
        }
    }

    void Update() {
        if (planeFired == null) {
            Destroy(gameObject);
            return;
        }

        if ((transform.position - planeFired.transform.position).magnitude > armingDist) {
            collisionToPlaneFired(true);
        }

        timer += Time.deltaTime;
        if (fuseTimeSec > 0 && timer > fuseTimeSec) dealDamage();

        transform.right = GetComponent<Rigidbody2D>().linearVelocity.normalized;
    }

    public float getInitSpeed() {
        return initSpeed;
    }
}

public class BulletMessage {
    private DamageModel damageModel;
    private float damage = 0f;

    public BulletMessage(DamageModel d) {
        damageModel = d;
    }

    public void addDamage(float dmg) {
        damage += dmg;
    }

    public float getDamage() {
        return damage;
    }

    public DamageModel getDamageModel() {
        return damageModel;
    }

    public override string ToString() {
        return damageModel.gameObject.name + " hit for: " + damage + (modelDown() ? ", module down" : "") + (modelCrit() ? ", critical" : "") + (targetDown() ? ", target down" : "") + "\n";
    }

    public bool modelCrit() {
        float criticalityNum = .25f;
        return moduleBroughtBelowHealthDecimal(criticalityNum);
    }

    public bool modelDown() {
        return moduleBroughtBelowHealthDecimal(0f);
    }

    public bool targetDown() {
        return damageModel.isCritical() && modelDown();
    }

    private bool moduleBroughtBelowHealthDecimal(float num) {
        return damageModel.healthAsDecimal() < num && (damageModel.getHealth() + damage) / damageModel.getMaxHealth() > num;
    }
}

public class BulletMessagePacket {
    List<BulletMessage> messages = new List<BulletMessage>();

    public BulletMessagePacket(BulletMessagePacket packet) {
        messages = packet.getMessages();
    }

    public BulletMessagePacket() {}

    public void addDamage(DamageModel mdl, float dmg) {
        foreach (BulletMessage message in messages) {
            if (message.getDamageModel() == mdl) {
                message.addDamage(dmg);
                return;
            }
        }
        BulletMessage b = new BulletMessage(mdl);
        b.addDamage(dmg);
        messages.Add(b);
    }

    public List<BulletMessage> getMessages() {
        return messages;
    }

    public override string ToString() {
        string str = "";
        foreach (BulletMessage message in messages) {
            str += message.ToString();
        }
        return str;
    }
}