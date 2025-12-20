using UnityEngine;
using UnityEngine.Experimental.Rendering;
using static Utils;

public class SkyScript : MonoBehaviour {
    [SerializeField] private int size;
    [SerializeField] private Vector3 sunPos;

    void Update() {
        Texture2D texture = new Texture2D(size, size);
        GetComponent<Renderer>().material.mainTexture = texture;
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, size, size), Vector2.zero);
        GetComponent<SpriteRenderer>().sprite = sprite;

        for (int y = 0; y < texture.height; y++) {
            for (int x = 0; x < texture.width; x++) {
                //Vector3 sunRayContactWithAtm = 
                float distance = Vector3.Distance(/*sunRayContactWithAtm*/sunPos, pixelToWorldPoint(x, y));

                Color color = new Color(0f, 0f, 0f, distance / size);

                texture.SetPixel(x, y, color);
            }

        }

        texture.Apply();
    }

    private Vector3 pixelToWorldPoint(int x, int y) {
        return transform.position + new Vector3(x * transform.lossyScale.x / size, y * transform.lossyScale.y / size, 0f);
    }
}
