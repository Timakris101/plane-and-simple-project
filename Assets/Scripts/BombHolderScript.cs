using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BombHolderScript : GunScript {

    private Sprite origSprite;

    new void Start() {
        timer = fireRate;
        origSprite = transform.parent.GetComponent<SpriteRenderer>().sprite;
        base.Start();
        transform.parent.GetComponent<Rigidbody2D>().mass += bullet.GetComponent<Rigidbody2D>().mass * ammunition;
    }

    new void Update() {
        base.Update();
        GetComponent<SpriteRenderer>().sprite = (ammunition != 0 && transform.parent.GetComponent<SpriteRenderer>().sprite == origSprite ? bullet.GetComponent<SpriteRenderer>().sprite : null);
    }

    protected override void shoot() {
        base.shoot();
        transform.parent.GetComponent<Rigidbody2D>().mass -= bullet.GetComponent<Rigidbody2D>().mass;
    }
}
