using UnityEngine;

public class AllianceHolder : MonoBehaviour {
    [SerializeField] private string alliance;

    public string getAlliance() {
        return alliance;
    }

    public void setAlliance(string alliance) {
        this.alliance = alliance;
    }
}
