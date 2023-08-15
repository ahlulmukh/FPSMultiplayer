using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;

public class UIController : MonoBehaviour
{
    public static UIController instance;

    public Slider healthSlider;

    public TextMeshProUGUI ammoText;

    public GameObject deathScreen;
    public TMP_Text deathText;
    public FixedJoystick joystick;


    public TMP_Text killsText;
    public TMP_Text deathsText;

    public GameObject leaderboard;
    public LeaderboardPlayer leaderboardPlayerDisplay;


    public GameObject endScreen;

    public TMP_Text timerText;

    public TextMeshProUGUI FpsText;
    private float pollingTime = 1f;
    private float time;
    private int frameCount;

    public Image damageScreen;

    public TextMeshProUGUI PingText;
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
        if(SimpleInput.GetButtonDown("Pause"))
        {
            ShowHideOptions();
        }

        if(optionsScreen.activeInHierarchy && Cursor.lockState != CursorLockMode.None)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        FpsDisplay();
    }

    void FpsDisplay()
    {
        time += Time.deltaTime;

        frameCount++;

        if(time >= pollingTime)
        {
            int frameRate =  Mathf.RoundToInt(frameCount / time);
            FpsText.text = frameRate.ToString() + " FPS";

            time -= pollingTime;
            frameCount = 0;
        }

        if (PhotonNetwork.IsConnected)
        {
            int ping = PhotonNetwork.GetPing();
            PingText.text = "Ping: " + ping + " ms"; // Update teks elemen UI dengan nilai ping
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
