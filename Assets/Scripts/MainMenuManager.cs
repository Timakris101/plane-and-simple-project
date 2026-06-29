using UnityEngine;
using UnityEngine.Audio;
using TMPro;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour {
    [Header("Scenes")]
    [SerializeField] private GameObject scenesPackage;

    [Header("Options")]
    [SerializeField] private GameObject optionsMenuOpeningButton;
    [SerializeField] private GameObject optionsMenuClosingButton;

    [Header("Options/ControlMode")]
    [SerializeField] private GameObject[] selectableControlModes;

    [Header("Options/Volume")]
    [SerializeField] private GameObject sfxSlider;
    [SerializeField] private GameObject musicSlider;
    [SerializeField] private AudioMixerGroup sfxMixer;
    [SerializeField] private AudioMixerGroup musicMixer;
    
    void Start() {
        openOptionsMenu(); //set mixervals
        closeOptionsMenu();
    }

    public void updateMixerGroups() {
        musicMixer.audioMixer.SetFloat("musicVolume", Mathf.Log10(musicSlider.GetComponent<Slider>().value) * 20f);
        sfxMixer.audioMixer.SetFloat("sfxVolume", Mathf.Log10(sfxSlider.GetComponent<Slider>().value) * 20f);
        PlayerPrefs.SetFloat("musicVolume", Mathf.Log10(musicSlider.GetComponent<Slider>().value) * 20f);
        PlayerPrefs.SetFloat("sfxVolume", Mathf.Log10(sfxSlider.GetComponent<Slider>().value) * 20f);
    }

    public void openOptionsMenu() {
        scenesPackage.SetActive(false);

        optionsMenuOpeningButton.SetActive(false);
        optionsMenuClosingButton.SetActive(true);

        int amountActive = 0;
        foreach (GameObject g in selectableControlModes) {
            if (g.name == PlayerPrefs.GetString(g.GetComponent<PlayerPrefHolder>().getKey())) amountActive++;
            g.SetActive(g.name == PlayerPrefs.GetString(g.GetComponent<PlayerPrefHolder>().getKey()));
        }
        if (amountActive == 0) selectableControlModes[0].SetActive(true);

        musicSlider.SetActive(true);
        sfxSlider.SetActive(true);
        
        float musicVal = 0f;
        if (!PlayerPrefs.HasKey("musicVolume")) {
            musicMixer.audioMixer.GetFloat("musicVolume", out musicVal);
        } else {
            musicVal = PlayerPrefs.GetFloat("musicVolume");
        }
        float sfxVal = 0f;
        if (!PlayerPrefs.HasKey("sfxVolume")) {
            sfxMixer.audioMixer.GetFloat("sfxVolume", out sfxVal);
        } else {
            sfxVal = PlayerPrefs.GetFloat("sfxVolume");
        }

        musicSlider.GetComponent<Slider>().value = Mathf.Pow(10f, musicVal / 20f);
        sfxSlider.GetComponent<Slider>().value = Mathf.Pow(10f, sfxVal / 20f);
    }

    public void closeOptionsMenu() {
        scenesPackage.SetActive(true);

        optionsMenuOpeningButton.SetActive(true);
        optionsMenuClosingButton.SetActive(false);

        foreach (GameObject g in selectableControlModes) {
            g.SetActive(false);
        }

        musicSlider.SetActive(false);
        sfxSlider.SetActive(false);
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
