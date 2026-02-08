using UnityEngine;

public class ArmorScript : DamageModel
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        maxHealth = 100f;
        screenShakeFactor = 1f / 20f;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
