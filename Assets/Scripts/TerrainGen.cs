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

    [SerializeField] private bool funkyGen;
    void Awake() {
        if (funkyGen) {
            Vector2[] terrainVecs = new Vector2[(int) terrainPointAmt];
            GetComponent<SpriteShapeController>().spline.Clear();
            float terrainStepSize = terrainLength / terrainPointAmt;

            for (int i = 0; i < terrainPointAmt; i++) {
                if (i != 0) {
                    int counter = 0;
                    bool selfIntersects = false;
                    terrainVecs[i] = terrainVecs[i - 1] + (new Vector2(Random.Range(-.5f, 1f), Random.Range(-2f, 1f))).normalized * 5 * terrainStepSize;
                    if (terrainVecs[i].y < 0f) terrainVecs[i] = new Vector2(terrainVecs[i].x, -terrainVecs[i].y);
                }

                if (terrainVecs[i].x > terrainLength / 2) terrainVecs[i] = terrainVecs[i - 1] + new Vector2(.1f, 0);

                terrainVecs[0] = new Vector2(-terrainLength / 2, 0f);

                terrainVecs[(int) terrainPointAmt - 1] = new Vector2(terrainLength / 2, 0f);

                GetComponent<SpriteShapeController>().spline.InsertPointAt(i, new Vector2(terrainVecs[i].x, i == 0 || i == terrainPointAmt - 1 ? 0f : terrainVecs[i].y));
            }

            GetComponent<PolygonCollider2D>().SetPath(0, terrainVecs);

            GetComponent<SpriteShapeRenderer>().localBounds = new Bounds(Vector3.zero, new Vector3(terrainLength * transform.localScale.x, transform.localScale.y * maxTerrHeight(terrainVecs), 0)); 

            if (transform.childCount != 0) {
                transform.GetChild(0).GetComponent<SpriteRenderer>().size = new Vector2(terrainLength * 2f, Constants.Water.seaLevel * 2f);
                transform.GetChild(0).GetComponent<BoxCollider2D>().size = new Vector2(terrainLength, Constants.Water.seaLevel * 2f);
            }
        } else {
            Vector2[] terrainVecs = new Vector2[(int) terrainPointAmt];
            GetComponent<SpriteShapeController>().spline.Clear();
            for (int i = 0; i < terrainPointAmt; i++) {
                terrainVecs[i] = new Vector2(-(terrainLength / 2) + i * (terrainLength / terrainPointAmt), Random.Range(maxHeight / 2f, maxHeight) + baseTerrHeight.Evaluate(i / terrainPointAmt));
                GetComponent<SpriteShapeController>().spline.InsertPointAt(i, new Vector2(terrainVecs[i].x, i == 0 || i == terrainPointAmt - 1 ? 0f : terrainVecs[i].y));
            }
            terrainVecs[0] = new Vector2(-terrainLength / 2, 0f);

            terrainVecs[(int) terrainPointAmt - 1] = new Vector2(terrainLength / 2, 0f);

            GetComponent<PolygonCollider2D>().SetPath(0, terrainVecs);

            GetComponent<SpriteShapeRenderer>().localBounds = new Bounds(Vector3.zero, new Vector3(terrainLength * transform.localScale.x, maxHeight * transform.localScale.y + maxTerrHeight(terrainVecs), 0)); 

            if (transform.childCount != 0) {
                transform.GetChild(0).GetComponent<SpriteRenderer>().size = new Vector2(terrainLength * 2f, Constants.Water.seaLevel * 2f);
                transform.GetChild(0).GetComponent<BoxCollider2D>().size = new Vector2(terrainLength, Constants.Water.seaLevel * 2f);
            }
        }
    }

    public float maxTerrHeight(Vector2[] terrainVecs) {
        float max = 0f;
        for (int i = 0; i < terrainPointAmt; i++) {
            if (terrainVecs[i].y > max) max = terrainVecs[i].y;
        }
        return max;
    }
}
