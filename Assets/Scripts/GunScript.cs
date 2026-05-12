using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Utils;
using Unity.Netcode;

public class GunScript : NetworkBehaviour {

    [SerializeField] protected GameObject belt;
    protected GameObject bullet => belt.GetComponent<BeltScript>().getBullet(ammunition);
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

    private float screenShakeFactor = 10f;

    protected void Start() {
        ammunition = maxAmmunition;
    }
    
    protected virtual void shoot() {
        if (GameObject.Find("Camera").GetComponent<CamScript>().getControlledOrSpectatedVehicle() == maxAncestor(gameObject)) GameObject.Find("Camera").GetComponent<CamScript>().shakeScreen(.1f, bullet.GetComponent<Rigidbody2D>().mass / maxAncestor(gameObject).GetComponent<Rigidbody2D>().mass * bullet.GetComponent<BulletScript>().getInitSpeed() * screenShakeFactor);
        ammunition--;
        if (GameObject.Find("NetworkManager") != null) {
            float latency = NetworkManager.Singleton.NetworkConfig.NetworkTransport.GetCurrentRtt(NetworkManager.Singleton.NetworkConfig.NetworkTransport.ServerClientId) / 1000f;

            pewPewRpc((transform.childCount == 0 ? transform.position : transform.Find("BulletSpawnArea").position) + baseVel * latency * 2f, bullet.GetComponent<BulletScript>().getInitSpeed() * transform.right + baseVel, bulletFuse);
        } else {
            GameObject newBullet = Instantiate(bullet, (transform.childCount == 0 ? transform.position : transform.Find("BulletSpawnArea").position) + baseVel * timeDif(), transform.rotation);
            newBullet.GetComponent<Rigidbody2D>().linearVelocity = newBullet.GetComponent<BulletScript>().getInitSpeed() * transform.right + baseVel;
            newBullet.GetComponent<BulletScript>().setPlaneFired(maxAncestor(gameObject));
            newBullet.GetComponent<BulletScript>().setFuseTime(bulletFuse);
            maxAncestor(gameObject).GetComponent<Rigidbody2D>().AddForceAtPosition(-transform.right * .5f *  Mathf.Pow(bullet.GetComponent<BulletScript>().getInitSpeed(), 2f) * bullet.GetComponent<Rigidbody2D>().mass, transform.position, ForceMode2D.Force);
        }
    }
    
    [Rpc(SendTo.Server)]
    void pewPewRpc(Vector3 where, Vector3 vel, float bulletFuse) {
        GameObject newBullet = Instantiate(bullet, where, transform.rotation);
        newBullet.GetComponent<Rigidbody2D>().linearVelocity = vel;
        
        newBullet.GetComponent<BulletScript>().setFuseTime(bulletFuse);

        NetworkObject newBulletNetwork = newBullet.GetComponent<NetworkObject>();
        newBulletNetwork.Spawn();

        newBullet.GetComponent<BulletScript>().setPlaneFired(maxAncestor(gameObject));

        maxAncestor(gameObject).GetComponent<Rigidbody2D>().AddForceAtPosition(-transform.right * .5f *  Mathf.Pow(bullet.GetComponent<BulletScript>().getInitSpeed(), 2f) * bullet.GetComponent<Rigidbody2D>().mass, transform.position, ForceMode2D.Force);

        setBulletRpc(newBulletNetwork.NetworkObjectId, vel, bulletFuse);
    }

    [Rpc(SendTo.Everyone)]
    void setBulletRpc(ulong idOfNewBullet, Vector3 vel, float bulletFuse) {
        GameObject localBullet = NetworkManager.SpawnManager.SpawnedObjects[idOfNewBullet].gameObject;
        localBullet.GetComponent<BulletScript>().setPlaneFired(maxAncestor(gameObject));

        localBullet.GetComponent<Rigidbody2D>().linearVelocity = vel;
        
        localBullet.GetComponent<BulletScript>().setFuseTime(bulletFuse);
    }   
    
    float updateTimer;
    protected void Update() {
        timer += Time.deltaTime;
        if (timer > fireRate && shooting && ammunition > 0) {
            timer = 0;
            shoot();
        }

        updateTimer += Time.deltaTime;
    }

    float fixedUpdateTimer;
    private void FixedUpdate() {
        baseVel = (Vector3) maxAncestor(gameObject).GetComponent<Rigidbody2D>().linearVelocity;
        fixedUpdateTimer += Time.fixedDeltaTime;
    }

    public float timeDif() {
        return (bullet.GetComponent<Rigidbody2D>().interpolation == RigidbodyInterpolation2D.Interpolate) ? (fixedUpdateTimer - updateTimer) / 2 : 0f;
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

    public void fillAmmo() {
        ammunition = maxAmmunition;
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
