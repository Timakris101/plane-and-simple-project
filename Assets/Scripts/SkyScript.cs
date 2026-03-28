using UnityEngine;
using UnityEngine.Experimental.Rendering;
using static Utils;
using System;
using System.Collections.Generic;
using UnityEngine.Rendering.Universal;

[ExecuteInEditMode]
public class SkyScript : MonoBehaviour {
    [SerializeField] private int size;
    [SerializeField] private Vector3 sunPosOnScreen;
    // private float earthRadius = 6371000f;
    // private float atmosphericHeight = 100000f;
    // private float stepSize = 10000f;
    // private float maxStepCount = 500f;
    // [SerializeField] private float baseScatter = 400f;
    [SerializeField] private float physicalSize;
    [SerializeField] private float distBack;
    [SerializeField] private float endarkeningFactor;
    [SerializeField] private bool takesMax;
    [SerializeField] private AnimationCurve sunPath;
    [SerializeField] private AnimationCurve glareStrength;
    [SerializeField] private AnimationCurve sunSize;
    [SerializeField] private Gradient gradient;
    [SerializeField] private float dayLength;
    [SerializeField] private float time;
    [SerializeField] private float altitudeCoef;
    [SerializeField] private bool irlTime; 
    [SerializeField] private GameObject star;
    [SerializeField] private int starCount;
    List<GameObject> stars;
    Camera camera => GameObject.Find("Camera").GetComponent<Camera>();
    //Vector3 earthCenter => new Vector3(0, -earthRadius, 0f);
    int baseSize = 100;

    void Start() {
        stars = new List<GameObject>();
        for (int i = 0; i < starCount; i++) {
            GameObject newStar = Instantiate(star, transform.position + new Vector3(UnityEngine.Random.Range(0f, physicalSize), UnityEngine.Random.Range(0f, physicalSize), transform.position.z), Quaternion.identity, transform);
            newStar.GetComponent<Light2D>().pointLightOuterRadius = UnityEngine.Random.Range(0f, star.GetComponent<Light2D>().pointLightOuterRadius);
            stars.Add(newStar);
        }
        foreach (GameObject g in progenyWithScript<Light2D>(gameObject)) {
            if (!stars.Contains(g)) DestroyImmediate(g);
        }
    }

    void LateUpdate() {
        if (GameObject.Find("Camera") == null) return;
        
        transform.parent = null;
        transform.localScale = new Vector3(physicalSize * 100 / size, physicalSize * 100 / size, 1f);
        transform.position = camera.transform.position - (new Vector3(1, 1f, 0) * physicalSize / 100f * baseSize / 2f) - new Vector3(0, 0, camera.transform.position.z - distBack);

        float percentDay = (time % dayLength) / dayLength;
        transform.GetChild(0).localScale = new Vector3(sunSize.Evaluate(percentDay) / baseSize * size, sunSize.Evaluate(percentDay) / baseSize * size, 1);
        transform.GetChild(0).localPosition = sunPosOnScreen / baseSize;
    }

    int frameCounter;
    void Update() {
        if (GameObject.Find("Camera") == null) return;

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

        sunPosOnScreen = new Vector3(percentDay * texture.width, sunPath.Evaluate(percentDay) * texture.height - altitudeCoef * transform.position.y * size / baseSize, 0f);

        for (int y = 0; y < texture.height; y++) {
            for (int x = 0; x < texture.width; x++) {
                float dist = Vector3.Distance(sunPosOnScreen, new Vector3(x, y, 0f));
                
                Color color = (gradient.Evaluate(sunPosOnScreen.x / texture.width) + new Color((texture.width - dist) * baseSize / size * glareStrength.Evaluate(sunPosOnScreen.x / texture.width), (texture.height - dist) * baseSize / size * glareStrength.Evaluate(sunPosOnScreen.x / texture.width), 0f, .1f));

                texture.SetPixel(x, y, color);
            }
        }

        texture.Apply();

        float maxEndarkening = .5f;

        float endarkening = 0f;
        float rStepCount = 3f;
        float maxR = 50f;
        float thetaStepCount = 3f;
        float counter = 0;
        float depthStep = 10f;

        float totalDarknessOfDepths = 0f;
        float maxDarknessOfAnyDepth = 0f; 

        LayerMask lm = ~LayerMask.GetMask("Bullet");
        for (float depth = 0; depth <= distBack; depth += depthStep) {
            float darknessOfCurDepth = 0f;
            for (float r = 0; r <= maxR; r += maxR / rStepCount) {
                for (float theta = 0; theta < 6.28f; theta += 6.28f / thetaStepCount) {
                    if (r == 0 && theta != 0) continue;
                    Vector3 posOnSun = new Vector3(r * Mathf.Cos(theta), r * Mathf.Sin(theta), 0f);
                    Collider2D[] hits = Physics2D.OverlapCircleAll(camera.ScreenToWorldPoint(new Vector3(camera.WorldToScreenPoint(transform.GetChild(0).position).x, camera.WorldToScreenPoint(transform.GetChild(0).position).y, -camera.transform.position.z + depth)) + posOnSun, 0.01f, lm, depth - depthStep / 2f, depth + depthStep / 2f);
                    float alpha = 0f;
                    foreach (Collider2D hit in hits) {
                        if (hit && hit.transform.GetComponent<Renderer>() != null) alpha += hit.transform.GetComponent<Renderer>().sharedMaterial.color.a;
                    }
                    darknessOfCurDepth += Mathf.Min(1f, alpha);
                    counter++;
                }
            }
            totalDarknessOfDepths += darknessOfCurDepth;
            if (darknessOfCurDepth > maxDarknessOfAnyDepth) maxDarknessOfAnyDepth = darknessOfCurDepth;
        }
        if (!takesMax) {
            endarkening = Mathf.Min(totalDarknessOfDepths / (counter / (distBack / depthStep + 1f)) * endarkeningFactor, 1f);
        } else {
            endarkening = maxDarknessOfAnyDepth / (counter / (distBack / depthStep + 1f)) * endarkeningFactor;
        }

        GameObject.Find("Canvas").transform.Find("Brightness").GetComponent<RectTransform>().sizeDelta = GameObject.Find("Canvas").GetComponent<RectTransform>().sizeDelta;
        GameObject.Find("Canvas").transform.Find("Brightness").GetComponent<UnityEngine.UI.Image>().color = new Color(0f, 0f, 0f, Mathf.Max((.7f - (gradient.Evaluate(percentDay).maxColorComponent)), Mathf.Min(endarkening, maxEndarkening)));

        frameCounter++;
        int index = 0;
        int groupCount = 3;
        foreach (GameObject star in stars) {
            index++;
            if (index % groupCount != frameCounter % groupCount) continue;
            star.GetComponent<Light2D>().intensity = 1f - texture.GetPixel((int) worldToPixel(star.transform.position).x, (int) worldToPixel(star.transform.position).y).maxColorComponent;
            for (float depth = 0; depth <= distBack; depth += depthStep) {
                Collider2D[] hits = Physics2D.OverlapCircleAll(camera.ScreenToWorldPoint(new Vector3(camera.WorldToScreenPoint(star.transform.position).x, camera.WorldToScreenPoint(star.transform.position).y, -camera.transform.position.z + depth)), star.GetComponent<Light2D>().pointLightOuterRadius / 5f, lm, depth - depthStep / 2f, depth + depthStep / 2f);
                float alpha = 0f;
                foreach (Collider2D hit in hits) {
                    if (hit && hit.transform.GetComponent<Renderer>() != null) alpha += hit.transform.GetComponent<Renderer>().sharedMaterial.color.a;
                }
                if (alpha != 0f) star.GetComponent<Light2D>().intensity = 1f - Mathf.Min(alpha, 1f);
            }
        }
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

    private Vector2 worldToPixel(Vector3 pos) {
        return new Vector2((int) (pos.x * baseSize / transform.lossyScale.x), (int) (pos.y * baseSize / transform.lossyScale.y)) - new Vector2((int) (transform.position.x * baseSize / transform.lossyScale.x), (int) (transform.position.y * baseSize / transform.lossyScale.x));
    }
}
