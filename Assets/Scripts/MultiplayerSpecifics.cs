using UnityEngine;
using Unity.Netcode;
using static Utils;

public class MultiplayerSpecifics : NetworkBehaviour {
    GameObject bullet;
    public void shoot(GameObject bullet, GameObject gun, Vector3 baseVel, float bulletFuse) {
        this.bullet = bullet;
        Debug.Log(bullet);

        float latency = NetworkManager.Singleton.NetworkConfig.NetworkTransport.GetCurrentRtt(NetworkManager.Singleton.NetworkConfig.NetworkTransport.ServerClientId) / 1000f;

        pewPewRpc((gun.transform.childCount == 0 ? gun.transform.position : gun.transform.Find("BulletSpawnArea").position) + baseVel * latency * 3f, bullet.GetComponent<BulletScript>().getInitSpeed() * transform.right + baseVel, bulletFuse);
    }
    
    [Rpc(SendTo.Server)]
    void pewPewRpc(Vector3 where, Vector3 vel, float bulletFuse) {
        Debug.Log(bullet);
        GameObject newBullet = Instantiate(bullet, where, transform.rotation);
        newBullet.GetComponent<Rigidbody2D>().linearVelocity = vel;
        
        newBullet.GetComponent<BulletScript>().setFuseTime(bulletFuse);

        NetworkObject m_SpawnedNetworkObject = newBullet.GetComponent<NetworkObject>();
        m_SpawnedNetworkObject.Spawn();

        newBullet.GetComponent<BulletScript>().setPlaneFired(gameObject);

        GetComponent<Rigidbody2D>().AddForceAtPosition(-transform.right * .5f *  Mathf.Pow(bullet.GetComponent<BulletScript>().getInitSpeed(), 2f) * bullet.GetComponent<Rigidbody2D>().mass, transform.position, ForceMode2D.Force);
    }
}
