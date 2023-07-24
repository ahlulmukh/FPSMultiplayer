using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;

public class UIController : MonoBehaviour
{
    public static UIController instance;

    public Slider weaponTempSlider;
    public TMP_Text overheatedMessage;

    public Slider healthSlider;


    public GameObject deathScreen;
    public TMP_Text deathText;


    public TMP_Text killsText;
    public TMP_Text deathsText;

    public GameObject leaderboard;
    public LeaderboardPlayer leaderboardPlayerDisplay;


    public GameObject endScreen;

    public TMP_Text timerText;

    
    public GameObject optionsScreen;


    void Awake()
    {
        instance = this;
    }


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Escape))
        {
            ShowHideOptions();
        }

        if(optionsScreen.activeInHierarchy && Cursor.lockState != CursorLockMode.None)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        } 
    }

    public void ShowHideOptions()
    {
        if(!optionsScreen.activeInHierarchy)
        {
            optionsScreen.SetActive(true);
        }
        else
        {
            optionsScreen.SetActive(false);
        }
    }

    public void ReturnToMainMenu()
    {
        PhotonNetwork.AutomaticallySyncScene = false;
        PhotonNetwork.LeaveRoom();
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
