using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Utils;

public class GunScript : MonoBehaviour {

    [SerializeField] protected GameObject bullet;
    [SerializeField] protected float fireRate;
    protected float timer;
    [SerializeField] private int maxAmmunition;
    [SerializeField] protected int ammunition;
    [SerializeField] private float bulletFuse;
    [SerializeField] public float minDeflection;
    [SerializeField] public float maxDeflection;
    [SerializeField] public bool fixedToOtherGun;
    private bool shooting;
    private Vector3 baseVel;

    protected void Start() {
        ammunition = maxAmmunition;
    }

    protected virtual void shoot() {
        ammunition--;
        GameObject newBullet = Instantiate(bullet, (transform.childCount == 0 ? transform.position : transform.Find("BulletSpawnArea").position), transform.rotation);
        newBullet.GetComponent<Rigidbody2D>().linearVelocity = newBullet.GetComponent<BulletScript>().getInitSpeed() * transform.right + baseVel;
        newBullet.GetComponent<BulletScript>().setPlaneFired(maxAncestor(gameObject));
        newBullet.GetComponent<BulletScript>().setFuseTime(bulletFuse);
        parentWithScript<Rigidbody2D>(gameObject).GetComponent<Rigidbody2D>().AddForceAtPosition(-transform.right * newBullet.GetComponent<BulletScript>().getInitSpeed() * newBullet.GetComponent<Rigidbody2D>().mass, transform.position, ForceMode2D.Impulse);
    }

    protected void Update() {
        timer += Time.deltaTime;
        if (timer > fireRate && shooting && ammunition > 0) {
            timer = 0;
            shoot();
        }
    }

    private void FixedUpdate() {
        baseVel = (Vector3) maxAncestor(gameObject).GetComponent<Rigidbody2D>().linearVelocity;
    }

    public void setFuseOfBullets(float sec) {
        bulletFuse = sec;
    }

    public void setFuseOfBullets(Vector3 target) {
        bulletFuse = Mathf.Abs((target - transform.position).x / (Mathf.Cos(Vector3.Angle(target - transform.position, Vector3.right) * Mathf.Deg2Rad) * (bullet.GetComponent<BulletScript>().getInitSpeed())));
    }

    public void setShooting(bool b) {
        shooting = b;
    }

    public GameObject getBullet() {
        return bullet;
    }

    public int getAmmo() {
        return ammunition;
    }

    public void setTimer(float val) {
        timer = val;
    }
}
