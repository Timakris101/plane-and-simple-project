using UnityEngine;

[ExecuteInEditMode]
public class PanelScript : MonoBehaviour {
    void Update() {
        GetComponent<RectTransform>().offsetMax = new Vector2((Screen.width - (Screen.safeArea.x + Screen.safeArea.width)) / (Screen.width / transform.parent.parent.GetComponent<RectTransform>().sizeDelta.x), 
                                                              (Screen.height - (Screen.safeArea.y + Screen.safeArea.height)) / (Screen.width / transform.parent.parent.GetComponent<RectTransform>().sizeDelta.x));
        GetComponent<RectTransform>().offsetMin = new Vector2(transform.parent.parent.GetComponent<RectTransform>().sizeDelta.x / 3f * 2f, 
                                                              ((-Screen.safeArea.y) / (Screen.width / transform.parent.parent.GetComponent<RectTransform>().sizeDelta.x)));
    }
}
