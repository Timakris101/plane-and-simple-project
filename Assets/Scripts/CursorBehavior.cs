using UnityEngine;

public class CursorBehavior : MonoBehaviour {
    [SerializeField] private Texture2D cursorVehicle;
    [SerializeField] private Vector2 hotspotCursorVehicle;
    [SerializeField] private Texture2D cursorNoVehicle;
    [SerializeField] private Vector2 hotspotCursorNoVehicle;
    void Update() {
        if (GetComponent<CamScript>().getControlledOrSpectatedVehicle() != null) {
            Cursor.SetCursor(cursorVehicle, hotspotCursorVehicle, CursorMode.Auto);
        } else {
            Cursor.SetCursor(cursorNoVehicle, hotspotCursorNoVehicle, CursorMode.Auto);
        }
    }
}
