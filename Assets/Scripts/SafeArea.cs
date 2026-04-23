using UnityEngine;

[ExecuteInEditMode]
public class SafeArea : MonoBehaviour {
    void Update() {
        // Debug.Log(Screen.safeArea);
        // Debug.Log(Screen.width);
        // Debug.Log(transform.parent.GetComponent<RectTransform>().sizeDelta);
        GetComponent<RectTransform>().sizeDelta = new Vector2(Screen.safeArea.width, Screen.safeArea.height) / (Screen.width / transform.parent.GetComponent<RectTransform>().sizeDelta.x);
        GetComponent<RectTransform>().position = new Vector2(Screen.safeArea.x + Screen.safeArea.width / 2f, Screen.safeArea.y + Screen.safeArea.height / 2f);
    }
}
