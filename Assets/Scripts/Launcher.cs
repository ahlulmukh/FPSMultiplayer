using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using TMPro;
using Photon.Realtime;

public class Launcher : MonoBehaviourPunCallbacks
{
    public static Launcher instance;


    [SerializeField] private GameObject loadingScreen;
    [SerializeField] private GameObject creditScreen;
    [SerializeField] private GameObject makeRoomPanel;
    [SerializeField] private TMP_Text loadingText;

    [SerializeField] private GameObject menuButtons;
    [SerializeField] private GameObject createRoomScreen;

    [SerializeField] private GameObject roomBrowserScreen;
    [SerializeField] private RoomButton theRoomButton;
    private List<RoomButton> _allRoomButtons = new List<RoomButton>();


    [SerializeField] private TMP_InputField roomNameInput;
    [SerializeField] private GameObject roomScreen;
    [SerializeField] private TMP_Text roomNameText;
    [SerializeField] private TMP_Text playerNameLabel;
    private List<TMP_Text> _allPlayerNames = new List<TMP_Text>();

    [SerializeField] private GameObject errorScreen;
    [SerializeField] private TMP_Text errorText;

    [SerializeField] private GameObject nameInputScreen;
    [SerializeField] private TMP_InputField nameInput;
    public static bool hasSetNickName;


    [SerializeField] private string levelToPlay;
    [SerializeField] private GameObject startButton;

    [SerializeField] private GameObject roomTestButton;

    public string[] allMaps;
    public bool changeMapBetweenRouns = true;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        Application.targetFrameRate = 60;
        CloseMenus();

        loadingScreen.SetActive(true);
        loadingText.text = "Connecting To Network...";

        if(!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.ConnectUsingSettings();
        }

#if UNITY_EDITOR
        roomTestButton.SetActive(true);
#endif

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }


    public override void OnConnectedToMaster()
    {
        PhotonNetwork.JoinLobby();

        PhotonNetwork.AutomaticallySyncScene = true;

        loadingText.text = "Joining Lobby...";
    }

    public override void OnJoinedLobby()
    {
        CloseMenus();
        menuButtons.SetActive(true);

        PhotonNetwork.NickName = Random.Range(0, 1000).ToString();
        ListAllPlayers();

        if(!hasSetNickName)
        {
            CloseMenus();
            nameInputScreen.SetActive(true);
            if(PlayerPrefs.HasKey("playerName"))
            {
                nameInput.text = PlayerPrefs.GetString("playerName");
            }
        }
        else
        {
            PhotonNetwork.NickName = PlayerPrefs.GetString("playerName");
        }
    }

    void CloseMenus()
    {
        loadingScreen.SetActive(false);
        menuButtons.SetActive(false);
        createRoomScreen.SetActive(false);
        roomScreen.SetActive(false);
        errorScreen.SetActive(false);
        roomBrowserScreen.SetActive(false);
        nameInputScreen.SetActive(false);
        creditScreen.SetActive(false);
        makeRoomPanel.SetActive(false);
    }

    public void OpenRoomCreate()
    {
        CloseMenus();
        createRoomScreen.SetActive(true);
    }

    public void CreditRoom()
    {
        CloseMenus();
        creditScreen.SetActive(true);
    }

    public void showRoomPanel()
    {
        CloseMenus();
        makeRoomPanel.SetActive(true);
    }

    public void CreateRoom()
    {
        if(!string.IsNullOrEmpty(roomNameInput.text))
        {

            RoomOptions options = new RoomOptions();
            options.MaxPlayers = 8;

            PhotonNetwork.CreateRoom(roomNameInput.text, options);

            CloseMenus();

            loadingText.text = "Creating Room...";
            loadingScreen.SetActive(true);
        }
    }

    public override void OnJoinedRoom()
    {
        CloseMenus();
        roomScreen.SetActive(true);

        roomNameText.text = PhotonNetwork.CurrentRoom.Name;

        ListAllPlayers();

        if(PhotonNetwork.IsMasterClient)
        {
            startButton.SetActive(true);    
        }
        else
        {
            startButton.SetActive(false);  
        }
    }

    private void ListAllPlayers()
    {
        foreach(TMP_Text player in _allPlayerNames)
        {
            Destroy(player.gameObject);
        }
        _allPlayerNames.Clear();

        Player[] players = PhotonNetwork.PlayerList;
        for(var i = 0; i < players.Length; i++)
        {
            TMP_Text newPlayerLabel = Instantiate(playerNameLabel, playerNameLabel.transform.parent);
            newPlayerLabel.text = players[i].NickName;
            newPlayerLabel.gameObject.SetActive(true);

            _allPlayerNames.Add(newPlayerLabel);
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
         TMP_Text newPlayerLabel = Instantiate(playerNameLabel, playerNameLabel.transform.parent);
        newPlayerLabel.text = newPlayer.NickName;
        newPlayerLabel.gameObject.SetActive(true);

        _allPlayerNames.Add(newPlayerLabel);
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        ListAllPlayers();

    }


    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        errorText.text = $"Failed To Create Room: {message}";
        CloseMenus();
        errorScreen.SetActive(true);
    }

    public void CloseErrorScreen(){
        CloseMenus();
        menuButtons.SetActive(true);
    }

    public void LeaveRoom()
    {
        PhotonNetwork.LeaveRoom();
        CloseMenus();

        loadingText.text = "Leaving Room...";
        loadingScreen.SetActive(true);
    }

    public override void OnLeftRoom()
    {
        CloseMenus();
        menuButtons.SetActive(true);
    }

    public void OpenRoomBrowser()
    {
        CloseMenus();
        roomBrowserScreen.SetActive(true);


    }

    public void CloseRoomBrowser()
    {
        CloseMenus();
        menuButtons.SetActive(true);
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        foreach(RoomButton rb in _allRoomButtons)
        {
            Destroy(rb.gameObject);
        }
        _allRoomButtons.Clear();
        theRoomButton.gameObject.SetActive(false);

        for(var i = 0; i < roomList.Count; i++)
        {
            if(roomList[i].PlayerCount != roomList[i].MaxPlayers && !roomList[i].RemovedFromList)
            {
                RoomButton newButton =  Instantiate(theRoomButton, theRoomButton.transform.parent);
                newButton.SetButtonDetails(roomList[i]);
                newButton.gameObject.SetActive(true);

                _allRoomButtons.Add(newButton);
            }
        }
    }

    public void JoinRoom(RoomInfo inputInfo)
    {
        PhotonNetwork.JoinRoom(inputInfo.Name);
        CloseMenus();
        loadingText.text = "Joining Room";
        loadingScreen.SetActive(true);

    }


    public void SetNickname()
    {
        if(!string.IsNullOrEmpty(nameInput.text))
        {
            PhotonNetwork.NickName = nameInput.text;

            PlayerPrefs.SetString("playerName", nameInput.text);

            CloseMenus();
            menuButtons.SetActive(true);

            hasSetNickName = true;
        }
    }


    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        if(PhotonNetwork.IsMasterClient)
        {
            startButton.SetActive(true);    
        }
        else
        {
            startButton.SetActive(false);  
        }
    }

    public void StartGame()
    {
        //PhotonNetwork.LoadLevel(levelToPlay);
        PhotonNetwork.LoadLevel(allMaps[Random.Range(0, allMaps.Length)]);
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void QuickJoin()
    {
        RoomOptions options = new RoomOptions();
        options.MaxPlayers = 8;

        PhotonNetwork.CreateRoom("Test");
        CloseMenus();

        loadingText.text = "Creating Room";
        loadingScreen.SetActive(true);
    }

}
