using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;

[ExecuteInEditMode]
public class TerrainGen : MonoBehaviour {
    [SerializeField] private float terrainPointAmt;
    [SerializeField] private float terrainLength;
    [SerializeField] private float maxHeight;
    [SerializeField] private AnimationCurve baseTerrHeight;
    void Awake() {
        Vector2[] terrainVecs = new Vector2[(int) terrainPointAmt];
        GetComponent<SpriteShapeController>().spline.Clear();
        for (int i = 0; i < terrainPointAmt; i++) {
            terrainVecs[i] = new Vector2(-(terrainLength / 2) + i * (terrainLength / terrainPointAmt), Random.Range(maxHeight / 2f, maxHeight) + baseTerrHeight.Evaluate(i / terrainPointAmt));
            GetComponent<SpriteShapeController>().spline.InsertPointAt(i, new Vector2(terrainVecs[i].x, i == 0 || i == terrainPointAmt - 1 ? 0f : terrainVecs[i].y));
        }
        terrainVecs[0] = new Vector2(-terrainLength / 2, 0f);

        terrainVecs[(int) terrainPointAmt - 1] = new Vector2(terrainLength / 2, 0f);

        GetComponent<PolygonCollider2D>().SetPath(0, terrainVecs);

        GetComponent<SpriteShapeRenderer>().localBounds = new Bounds(Vector3.zero, new Vector3(terrainLength * transform.localScale.x, maxHeight * transform.localScale.y + maxBaseTerrHeight(0.01f), 0)); 

        if (transform.childCount != 0) {
            transform.GetChild(0).GetComponent<SpriteRenderer>().size = new Vector2(terrainLength, Constants.Water.seaLevel * 2f);
            transform.GetChild(0).GetComponent<BoxCollider2D>().size = new Vector2(terrainLength, Constants.Water.seaLevel * 2f);
        }
    }

    public float maxBaseTerrHeight(float precision) {
        float max = 0f;
        for (float i = 0f; i <= 1f; i += precision) {
            if (baseTerrHeight.Evaluate(i) > max) max = baseTerrHeight.Evaluate(i);
        }
        return max;
    }
}
