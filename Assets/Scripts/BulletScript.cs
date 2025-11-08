using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Utils;

public class BulletScript : MonoBehaviour {
    private static float explosiveRangeOfCertainHit = 5f;

    [Header("Base Stats")]
    [SerializeField] private float initSpeed;
    [SerializeField] private float damage;
    [SerializeField] private float explosionRad;
    [SerializeField] private float penetrationVal;
    [SerializeField] private float armorPenMeters;
    [SerializeField] private float maxFlyPastDist;
    [SerializeField] private float armingDist;
    [SerializeField] private float fuseTimeSec;
    [SerializeField] private GameObject effect;
    [SerializeField] private float lifeTime;
    [SerializeField] private float damageVariation;
    private float effectLifeTime = .2f;
    private float timer;
    
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
        Vector3 beginningHitPos = transform.position - prevVel * Time.fixedDeltaTime;
        RaycastHit2D[] hits = Physics2D.RaycastAll(beginningHitPos, -col.relativeVelocity);
        float newPenVal = penetrationVal;
        int armorHitCount = 0;
        int index = 0;
        float effectiveArmorPen = armorPenMeters * col.relativeVelocity.magnitude / initSpeed;
        GameObject objClosestToBullet = null;
        foreach (RaycastHit2D hit in hits) {
            if (hit.transform == transform) continue;
            if (hit.transform.gameObject != maxAncestor(col.gameObject)) continue;

            if (objClosestToBullet == null) objClosestToBullet = hit.collider.gameObject;
            if ((hit.point - (Vector2) transform.position).magnitude < (objClosestToBullet.transform.position - transform.position).magnitude) {
                objClosestToBullet = hit.collider.gameObject;
            }

            if (hit.collider.transform.GetComponent<ArmorScript>() != null) {
                float effArmorThickness = hit.collider.transform.GetComponent<BoxCollider2D>().size.x / Mathf.Cos(Mathf.Deg2Rad * Vector3.Angle(col.relativeVelocity, hit.normal));
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
        if (objClosestToBullet == null) {
            if (col.transform.GetComponent<BoxCollider2D>() != null) {
                GetComponent<Rigidbody2D>().linearVelocity = prevVel;
                Physics2D.IgnoreCollision(gameObject.GetComponent<Collider2D>(), col.transform.GetComponent<Collider2D>());
                return;
            }
        }
        if (explosionRad < explosiveRangeOfCertainHit && armorHitCount == 1 && effectiveArmorPen <= 0f && objClosestToBullet.GetComponent<ArmorScript>() != null) return;
        hits = Physics2D.CircleCastAll(beginningHitPos - (Vector3) col.relativeVelocity.normalized * Random.Range(0f, maxFlyPastDist), explosionRad == 0 ? transform.localScale.x : explosionRad, -col.relativeVelocity, newPenVal);
        int counter = 0;
        foreach (RaycastHit2D hit in hits) {
            if (explosionRad < explosiveRangeOfCertainHit && hit.transform.gameObject != maxAncestor(col.gameObject)) continue;
            if (hit.collider.transform.GetComponent<DamageModel>() != null) {
                DamageModel d = hit.collider.transform.GetComponent<DamageModel>();
                if (!(Random.Range(0f, 1f) < (explosionRad < explosiveRangeOfCertainHit ? d.getHitChance(Vector3.Angle(col.relativeVelocity, d.transform.right)) : 1f))) continue;
                counter++;
                d.damage(Random.Range((1f - damageVariation) * damage, (1f + damageVariation) * damage));
            }
        }
        if (counter == 0 && col.transform.GetComponent<BoxCollider2D>() != null) {
            GetComponent<Rigidbody2D>().linearVelocity = prevVel;
            Physics2D.IgnoreCollision(gameObject.GetComponent<Collider2D>(), col.transform.GetComponent<Collider2D>());
            return;
        }
        makeEffectAndDestroyObj(transform.position);
    }

    void dealDamage() {
        Vector3 beginningHitPos = transform.position + (Vector3) GetComponent<Rigidbody2D>().linearVelocity.normalized * Random.Range(0f, maxFlyPastDist) - prevVel * Time.fixedDeltaTime;
        RaycastHit2D[] hits = Physics2D.CircleCastAll(beginningHitPos, explosionRad == 0 ? transform.localScale.x : explosionRad, GetComponent<Rigidbody2D>().linearVelocity.normalized, penetrationVal);
        foreach (RaycastHit2D hit in hits) {
            if (hit.collider.transform.GetComponent<DamageModel>() != null) {
                DamageModel d = hit.collider.transform.GetComponent<DamageModel>();
                if (!(Random.Range(0f, 1f) < (explosionRad < explosiveRangeOfCertainHit ? d.getHitChance(Vector3.Angle(GetComponent<Rigidbody2D>().linearVelocity, d.transform.right)) : 1f))) continue;
                d.damage(Random.Range((1f - damageVariation) * damage, (1f + damageVariation) * damage));
            }
        }
        makeEffectAndDestroyObj(transform.position);
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
