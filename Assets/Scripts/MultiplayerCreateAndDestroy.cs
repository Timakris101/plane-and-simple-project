using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class MultiplayerCreateAndDestroy : NetworkBehaviour {
    List<FiniteGameObject> hitList = new List<FiniteGameObject>();

    void Start() {
        DontDestroyOnLoad(gameObject);
    }

    void Update() {
        if (!IsClient) return;

        for (int i = 0; i < hitList.Count; i++) {
            if (hitList[i].gameObject == null) {
                hitList.Remove(hitList[i]);
                i--;
                continue;
            }

            hitList[i] = new FiniteGameObject(hitList[i].gameObject, hitList[i].lifeTime - Time.deltaTime);
            if (hitList[i].lifeTime < 0f) {
                killServerRpc(i);
                hitList.Remove(hitList[i]);
                i--;
            }
        }
    }

    [Rpc(SendTo.Server)]
    public void killServerRpc(int i) {
        if (i >= hitList.Count) return;
        if (hitList[i].gameObject == null) return;

        NetworkObject m_NetworkObject = hitList[i].gameObject.GetComponent<NetworkObject>();
        if (m_NetworkObject != null) m_NetworkObject.Despawn(true);
    }

    public void destroy(GameObject g) {
        destroy(g, 0f);
    }

    public void destroy(GameObject g, float life) {
        if (!IsClient) {
            Destroy(g, life);
        } else {
            hitList.Add(new FiniteGameObject(g, life));
        }
    }

//----------------------------------------------------------------------

    GameObject lilManToSpawn;
    GameObject lilManSpawned;

    public GameObject create(GameObject obj, Vector3 pos, Quaternion rot) {
        lilManToSpawn = obj;
        createServerRpc(pos, rot);
        return lilManSpawned;
    }

    [Rpc(SendTo.Server)]
    public void createServerRpc(Vector3 pos, Quaternion rot) {
        lilManSpawned = Instantiate(lilManToSpawn, pos, rot);
        NetworkObject m_SpawnedNetworkObject = lilManSpawned.GetComponent<NetworkObject>();
        if (m_SpawnedNetworkObject != null) m_SpawnedNetworkObject.Spawn();
    }
}

public class FiniteGameObject {
    public float lifeTime;
    public GameObject gameObject;

    public FiniteGameObject(GameObject gameObject, float lifeTime) {
        this.gameObject = gameObject;
        this.lifeTime = lifeTime;
    }
}
