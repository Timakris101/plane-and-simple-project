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
    private bool shooting;
    private Vector3 baseVel;

    protected void Start() {
        ammunition = maxAmmunition;
    }

    protected virtual void shoot() {
        ammunition--;
        GameObject newBullet = Instantiate(bullet, (transform.childCount == 0 ? transform.position : transform.Find("BulletSpawnArea").position), transform.rotation);
        newBullet.GetComponent<Rigidbody2D>().linearVelocity = newBullet.GetComponent<BulletScript>().getInitSpeed() * transform.right + baseVel;
        newBullet.GetComponent<BulletScript>().setPlaneFired(transform.parent.GetComponent<Aerodynamics>() == null ? transform.parent.parent.gameObject : transform.parent.gameObject);
        newBullet.GetComponent<BulletScript>().setFuseTime(bulletFuse);
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
        bulletFuse = (target - transform.position).magnitude / (bullet.GetComponent<BulletScript>().getInitSpeed());
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
