using UnityEngine;

public class BeltScript : MonoBehaviour {
    [SerializeField] private GameObject[] bullets;

    public GameObject getBullet(int ammoCount) {
        return bullets[ammoCount % bullets.Length];
    }
}
