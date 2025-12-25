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
    [SerializeField] private GameObject tree;
    [SerializeField] private float treeDensity;

    [SerializeField] private bool funkyGen;
    void Awake() {
        Vector2[] terrainVecs = new Vector2[(int) terrainPointAmt];
        if (funkyGen) {
            terrainVecs = new Vector2[(int) terrainPointAmt];
            GetComponent<SpriteShapeController>().spline.Clear();
            float terrainStepSize = terrainLength / terrainPointAmt;

            for (int i = 0; i < terrainPointAmt; i++) {
                if (i != 0) {
                    int counter = 0;
                    bool selfIntersects = false;
                    terrainVecs[i] = terrainVecs[i - 1] + (new Vector2(Random.Range(-.5f, 1f), Random.Range(-2f, 1f))).normalized * 5 * terrainStepSize;
                    if (terrainVecs[i].y < 0f) terrainVecs[i] = new Vector2(terrainVecs[i].x, -terrainVecs[i].y);

                    if (terrainVecs[i - 1].x > terrainLength / 2) terrainVecs[i] = terrainVecs[i - 1] + new Vector2(.1f, 0);
                }

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
            terrainVecs = new Vector2[(int) terrainPointAmt];
            GetComponent<SpriteShapeController>().spline.Clear();
            for (int i = 0; i < terrainPointAmt; i++) {
                terrainVecs[i] = new Vector2(-(terrainLength / 2) + i * (terrainLength / terrainPointAmt), Random.Range(maxHeight / 2f, maxHeight) + baseTerrHeight.Evaluate(i / terrainPointAmt));
                GetComponent<SpriteShapeController>().spline.InsertPointAt(i, new Vector2(terrainVecs[i].x, i == 0 || i == terrainPointAmt - 1 ? 0f : terrainVecs[i].y));
            }
            terrainVecs[0] = new Vector2(-terrainLength / 2, 0f);

            terrainVecs[(int) terrainPointAmt - 1] = new Vector2(terrainLength / 2, 0f);

            GetComponent<PolygonCollider2D>().SetPath(0, terrainVecs);

            GetComponent<SpriteShapeRenderer>().localBounds = new Bounds(Vector3.zero, new Vector3(terrainLength * transform.localScale.x, transform.localScale.y * maxTerrHeight(terrainVecs), 0)); 

            if (transform.childCount != 0) {
                transform.GetChild(0).GetComponent<SpriteRenderer>().size = new Vector2(terrainLength * 2f, Constants.Water.seaLevel * 2f);
                transform.GetChild(0).GetComponent<BoxCollider2D>().size = new Vector2(terrainLength, Constants.Water.seaLevel * 2f);
            }
        }

        if (!Application.isPlaying || treeDensity <= 0) return;

        for (float x = -terrainLength / 2f; x <= terrainLength / 2f; x += ((1f / treeDensity) + Random.Range(-.5f / treeDensity, .5f / treeDensity))) {
            GameObject newTree = Instantiate(tree, new Vector3(x, transform.localScale.y * maxTerrHeight(terrainVecs), transform.position.z), Quaternion.identity);
            newTree.transform.localScale *= Random.Range(.5f, 1.5f);
            RaycastHit2D[] hits = Physics2D.RaycastAll(newTree.transform.position, Vector3.down);
            foreach (RaycastHit2D hit in hits) {
                if (hit.transform.gameObject == gameObject) {
                    newTree.transform.position = (Vector3) hit.point + Vector3.up * 10f;
                    if (newTree.transform.position.y < Constants.Water.seaLevel + 10f || Vector3.Dot(hit.normal, Vector3.up) < .95f) Destroy(newTree);
                    break;
                }
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
