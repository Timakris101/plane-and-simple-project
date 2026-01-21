using UnityEngine;
using UnityEngine.Experimental.Rendering;
using static Utils;
using System;
using System.Collections.Generic;

[ExecuteInEditMode]
public class SkyScript : MonoBehaviour {
    [SerializeField] private int size;
    [SerializeField] private Vector3 sunPosOnScreen;
    // private float earthRadius = 6371000f;
    // private float atmosphericHeight = 100000f;
    // private float stepSize = 10000f;
    // private float maxStepCount = 500f;
    // [SerializeField] private float baseScatter = 400f;
    [SerializeField] private AnimationCurve sunPath;
    [SerializeField] private AnimationCurve glareStrength;
    [SerializeField] private AnimationCurve sunSize;
    [SerializeField] private Gradient gradient;
    [SerializeField] private float dayLength;
    [SerializeField] private float time;
    [SerializeField] private float altitudeCoef;
    [SerializeField] private bool irlTime; 
    Camera camera => GameObject.Find("Camera").GetComponent<Camera>();

    //Vector3 earthCenter => new Vector3(0, -earthRadius, 0f);
    int baseSize = 100;

    void LateUpdate() {
        transform.parent = null;
        transform.localScale = new Vector3(100000 / size, 100000 / size, 1f);
        transform.position = camera.transform.position - (new Vector3(1, 1f, 0) * 10f * baseSize / 2f) - new Vector3(0, 0, camera.transform.position.z);

        float percentDay = (time % dayLength) / dayLength;
        transform.GetChild(0).localScale = new Vector3(sunSize.Evaluate(percentDay) / baseSize * size, sunSize.Evaluate(percentDay) / baseSize * size, 1);
        transform.GetChild(0).localPosition = sunPosOnScreen / baseSize;
    }

    void Update() {
        Texture2D texture = new Texture2D(size, size);
        GetComponent<Renderer>().sharedMaterial.mainTexture = texture;
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, size, size), Vector2.zero);
        GetComponent<SpriteRenderer>().sprite = sprite;

        if (irlTime) {
            time = System.DateTime.Now.Hour * 3600 + System.DateTime.Now.Minute * 60 + System.DateTime.Now.Second;
        } else {
            time += Time.deltaTime;
        }

        float percentDay = (time % dayLength) / dayLength;

        sunPosOnScreen = new Vector3(percentDay * texture.width, sunPath.Evaluate(percentDay) * texture.height - altitudeCoef * transform.position.y, 0f);

        for (int y = 0; y < texture.height; y++) {
            for (int x = 0; x < texture.width; x++) {
                float dist = Vector3.Distance(sunPosOnScreen, new Vector3(x, y, 0f));
                
                Color color = (gradient.Evaluate(sunPosOnScreen.x / texture.width) + new Color((texture.width - dist) * baseSize / size * glareStrength.Evaluate(sunPosOnScreen.x / texture.width), (texture.height - dist) * baseSize / size * glareStrength.Evaluate(sunPosOnScreen.x / texture.width), 0f, .1f));

                texture.SetPixel(x, y, color);
            }
        }

        texture.Apply();

        camera.transform.Find("Canvas").Find("Brightness").GetComponent<UnityEngine.UI.Image>().color = new Color(0f, 0f, 0f, Mathf.Max(0f, camera.transform.Find("Canvas").Find("GForceDisp").GetComponent<UnityEngine.UI.Image>().color.a - Time.deltaTime));
        camera.transform.Find("Canvas").Find("Brightness").GetComponent<RectTransform>().sizeDelta = camera.transform.Find("Canvas").GetComponent<RectTransform>().sizeDelta;
        camera.transform.Find("Canvas").Find("Brightness").GetComponent<UnityEngine.UI.Image>().color = new Color(0f, 0f, 0f, (.7f - (gradient.Evaluate(percentDay).maxColorComponent)));
    }
// not working
    // private Vector3 lightSeenInViewDir(Vector3 dir, float stepSize) {
    //     Camera camera = parentWithScript<Camera>(gameObject).GetComponent<Camera>();
    //     Vector3 startOfRay = camera.transform.position;

    //     Vector3 pos = startOfRay;

    //     Vector3 totalInScattering = Vector3.zero;
    //     for (int i = 0; i < maxStepCount; i++) {
    //         Vector3 scatterCoeffs = new Vector3(
    //                                 Mathf.Pow(baseScatter / 700f, 4f)   ,
    //                                 Mathf.Pow(baseScatter / 530f, 4f)   ,
    //                                 Mathf.Pow(baseScatter / 440f, 4f)
    //                                 );

    //         float baseOD = -(opticalDepth(pos, sunPos, stepSize) + opticalDepth(pos, startOfRay, stepSize));
    //         Vector3 transmittance = Mathf.Exp(baseOD) * scatterCoeffs;

    //         float altitude = Vector3.Distance(pos, earthCenter) - earthRadius;

    //         totalInScattering += stepSize * Aerodynamics.getAirDensity(altitude) * 
    //         new Vector3(
    //         transmittance.x * scatterCoeffs.x,
    //         transmittance.y * scatterCoeffs.y,
    //         transmittance.z * scatterCoeffs.z
    //         );

    //         pos += dir * stepSize;

    //         if (Vector3.Distance(pos, earthCenter) > earthRadius + atmosphericHeight) break;
    //         // if (Vector3.Distance(pos, earthCenter) < earthRadius) return Vector3.zero;
    //     }

    //     return totalInScattering;
    // }

    // private float opticalDepth(Vector3 pos, Vector3 posToGoTo, float stepSize) {
    //     Vector3 dir = posToGoTo - pos;

    //     Vector3 posToCheck = pos;

    //     float total = 0f;
    //     for (int i = 0; i < maxStepCount; i++) {
    //         float altitude = Vector3.Distance(posToCheck, earthCenter) - earthRadius;
            
    //         total += Aerodynamics.getAirDensity(altitude) * stepSize;

    //         posToCheck += dir * stepSize;

    //         if (Vector3.Distance(posToCheck, earthCenter) > earthRadius + atmosphericHeight) break;
    //         if (Vector3.Distance(posToCheck, earthCenter) < earthRadius) break;
    //     }
    //     return total;
    // }

    private bool isPixelOnScreen(int x, int y) {
        Vector3 pixelWorldPoint = pixelToWorldPoint(x, y);

        Vector3 w2sp = camera.WorldToScreenPoint(pixelWorldPoint);

        if (w2sp.x > camera.pixelWidth) return false;
        if (w2sp.y > camera.pixelHeight) return false;
        if (w2sp.x < -10f) return false;
        if (w2sp.y < -10f) return false;
        return true;
    }

    private Vector3 pixelToWorldPoint(int x, int y) {
        return transform.position + new Vector3(x * transform.lossyScale.x / baseSize, y * transform.lossyScale.y / baseSize, transform.position.z);
    }
}
