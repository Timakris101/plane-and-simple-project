using UnityEngine;

public class MainMenuManager : MonoBehaviour {
    [Header("Scenes")]
    [SerializeField] private GameObject scenesPackage;

    [Header("Options")]
    [SerializeField] private GameObject optionsMenuOpeningButton;
    [SerializeField] private GameObject optionsMenuClosingButton;

    [Header("Options/ControlMode")]
    [SerializeField] private GameObject[] selectableControlModes;
    
    void Start() {
        optionsMenuOpeningButton.SetActive(true);
        optionsMenuClosingButton.SetActive(false);
        foreach (GameObject g in selectableControlModes) {
            g.SetActive(false);
        }
    }

    public void openOptionsMenu() {
        scenesPackage.SetActive(false);

        optionsMenuOpeningButton.SetActive(false);
        optionsMenuClosingButton.SetActive(true);

        foreach (GameObject g in selectableControlModes) {
            g.SetActive(g.name == PlayerPrefs.GetString(g.GetComponent<PlayerPrefHolder>().getKey()));
        }
    }

    public void closeOptionsMenu() {
        scenesPackage.SetActive(true);

        optionsMenuOpeningButton.SetActive(true);
        optionsMenuClosingButton.SetActive(false);

        foreach (GameObject g in selectableControlModes) {
            g.SetActive(false);
        }
    }

    public void clickThroughSelectableControlModes() {
        for (int i = 0; i < selectableControlModes.Length; i++) {
            if (selectableControlModes[i].activeInHierarchy) {
                selectableControlModes[i].SetActive(false);
                selectableControlModes[(i + 1) % selectableControlModes.Length].SetActive(true);
                selectableControlModes[i].GetComponent<PlayerPrefHolder>().setPlayerPref(selectableControlModes[(i + 1) % selectableControlModes.Length].name);
                break;
            }
        }
    }
}
