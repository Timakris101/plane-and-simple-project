using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Utils;

public class BulletScript : MonoBehaviour {
    [Header("Base Stats")]
    [SerializeField] private float initSpeed;
    [SerializeField] private float damage;
    [SerializeField] private float explosionRad;
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
    private float effectLifeTime = .2f;
    [SerializeField] private float timer;
    
    [Header("Plane")]
    [SerializeField] private GameObject planeFired;

    void OnCollisionEnter2D(Collision2D col) {
        dealDamage(col);
    }
    
    private Vector3 prevVel;
    private void FixedUpdate() {
        prevVel = GetComponent<Rigidbody2D>().linearVelocity;
    }

    void dealDamage(Collision2D col) {
        int armorHitCount;
        float effectiveArmorPen;
        float newPenVal;
        GameObject objClosestToBullet;
        handleArmor(col, out armorHitCount, out effectiveArmorPen, out newPenVal, out objClosestToBullet);

        if (armorHitCount == 1 && effectiveArmorPen <= 0f && objClosestToBullet.GetComponent<ArmorScript>() != null) return;

        if (objClosestToBullet == null && col.transform.GetComponent<BoxCollider2D>() != null) {
            missObj(col);
            return;
        }  

        if (didMiss(col, effectiveArmorPen, newPenVal) && col.transform.GetComponent<BoxCollider2D>() != null) {
            missObj(col);
            return;
        }

        handleDamage(col, armorFuseSec, effectiveArmorPen, newPenVal);

        makeEffectAndDestroyObj(transform.position);
    }

    void dealDamage() {
        float effectiveArmorPen = armorPenMeters * prevVel.magnitude / initSpeed;
        Vector3 beginningHitPos = transform.position - prevVel * Time.fixedDeltaTime;
        handleDamage(beginningHitPos, GetComponent<Rigidbody2D>().linearVelocity, armorFuseSec, effectiveArmorPen, penetrationVal);

        makeEffectAndDestroyObj(transform.position);
    }

    bool didMiss(Collision2D col, float effectiveArmorPen, float newPenVal) {
        RaycastHit2D[] hits = Physics2D.RaycastAll(col.contacts[0].point, -col.relativeVelocity, penetrationVal);
        foreach (RaycastHit2D hit in hits) {
            if (hit.transform.gameObject != maxAncestor(col.gameObject)) continue;
            if (hit.collider.transform.GetComponent<DamageModel>() != null) {
                DamageModel d = hit.collider.transform.GetComponent<DamageModel>();
                if (!(Random.Range(0f, 1f) < (d.getHitChance(Vector3.Angle(col.relativeVelocity, d.transform.right))))) continue;
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
        for (int i = 0; i < fragmentationAmt; i++) {
            Vector2 fragmentationVector = i == 0 ? new Vector2(0,0) : new Vector2(Random.Range(-fragmentationStrength, fragmentationStrength), Random.Range(-fragmentationStrength, fragmentationStrength));
            Vector2 fragVecPlusVel = fragmentationVector + (Vector2) relativeVel;
            RaycastHit2D[] hits = Physics2D.RaycastAll(beginningHitPos + relativeVel * postHitFuse, fragVecPlusVel, newPenVal);
            Debug.DrawRay(beginningHitPos + relativeVel * postHitFuse, fragVecPlusVel.normalized * newPenVal, Color.red, 10f);
            foreach (RaycastHit2D hit in hits) {
                if (hit.collider.transform.GetComponent<DamageModel>() != null) {
                    DamageModel d = hit.collider.transform.GetComponent<DamageModel>();
                    if (!(Random.Range(0f, 1f) < (d.getHitChance(Vector3.Angle(-fragVecPlusVel, d.transform.right))))) continue;
                    d.damage(Random.Range((1f - damageVariation), (1f + damageVariation)) * damage);
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
        GameObject newEffect = Instantiate(effect, effectPos, Quaternion.identity);
        var mainModule = newEffect.GetComponent<ParticleSystem>().main;
        if (mainModule.startSpeed.constantMax == 0) mainModule.startSpeed = new ParticleSystem.MinMaxCurve(0, explosionRad / mainModule.startLifetime.constant);
        Destroy(newEffect, effectLifeTime);
        Destroy(gameObject);
    }

    public void setPlaneFired(GameObject plane) {
        planeFired = plane;
    }

    void Start() {
        collisionToPlaneFired(false);
        Destroy(gameObject, lifeTime);
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
