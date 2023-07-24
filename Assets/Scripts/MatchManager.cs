using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;

public class MatchManager : MonoBehaviourPunCallbacks, IOnEventCallback
{
    public enum EventCodes : byte
    {
        NewPlayer,
        ListPlayers,
        UpdateStat,
        NextMatch,
        TimerSync
    }

    public enum GameState
    {
        Waiting,
        Playing,
        Ending
    }



    public static MatchManager instance;

    public List<PlayerInfo> allPlayers = new List<PlayerInfo>();
    private int index;
 
    private List<LeaderboardPlayer> lboardPlayers = new List<LeaderboardPlayer>();

    public int killsToWin = 10;

    public Transform mapCamPoint;
    public GameState state = GameState.Waiting;
    public float waitAfterEnding = 5f;

    public bool perpetual;

    public float matchLenght = 180f;
    private float currentMatchTime;

    private float sendTimer;
    private void Awake() 
    {
        instance = this;
    }

    void Start()
    {
        if(!PhotonNetwork.IsConnected)
        {
            SceneManager.LoadScene(0);
        }
        else
        {
            NewPlayerSend(PhotonNetwork.NickName);
            state = GameState.Playing;

            SetupTimer();

            if(!PhotonNetwork.IsMasterClient)
            {
                UIController.instance.timerText.gameObject.SetActive(false);
            }
        }

    }

    void Update()
    {
        if(Input.GetKey(KeyCode.Tab) && state != GameState.Ending)
        {
            ShowLeaderboard();
        }
        else
        {
            UIController.instance.leaderboard.SetActive(false);
        }

        if(PhotonNetwork.IsMasterClient)
        {

            if(currentMatchTime > 0f && state == GameState.Playing)
            {
                currentMatchTime -= Time.deltaTime;

                if(currentMatchTime <= 0f)
                {
                    currentMatchTime = 0;

                    state = GameState.Ending;
                    ListPlayersSend();

                    StateCheck();
                }

                UpdateTimerDisplay();

                sendTimer -= Time.deltaTime;

                if(sendTimer <= 0)
                {
                    sendTimer += 1f;

                    TimerSend();
                }
            }

        }
    }

    public void OnEvent(EventData photonEvent)
    {
        if(photonEvent.Code < 200)
        {
            EventCodes theEvent = (EventCodes)photonEvent.Code;
            object[] data = (object[])photonEvent.CustomData;

            switch(theEvent)
            {
                case EventCodes.NewPlayer:
                    NewPlayerReceive(data);
                    break;

                case EventCodes.ListPlayers:
                    ListPlayersReceive(data);
                    break;

                case EventCodes.UpdateStat:
                    UpdateStatsReceive(data);
                    break;

                case EventCodes.NextMatch:
                    NextMatchReceive();
                    break;

                case EventCodes.TimerSync:
                    TimerReceive(data);
                    break;
            }
        }
    }

    public override void OnEnable() 
    {
        PhotonNetwork.AddCallbackTarget(this);
    }

    public override void OnDisable()
    {
        PhotonNetwork.RemoveCallbackTarget(this);
    }


    public void NewPlayerSend(string username)
    {
        object[] package = new object[4];
        package[0] = username;
        package[1] = PhotonNetwork.LocalPlayer.ActorNumber;
        package[2] = 0;
        package[3] = 0;

        PhotonNetwork.RaiseEvent(
            (byte)EventCodes.NewPlayer,
            package,
            new RaiseEventOptions{ Receivers = ReceiverGroup.MasterClient },
            new SendOptions { Reliability = true }
        );
    }

    public void NewPlayerReceive(object[] dataReceived)
    {
        PlayerInfo player = new PlayerInfo(
            (string)dataReceived[0], 
            (int)dataReceived[1], 
            (int)dataReceived[2], 
            (int)dataReceived[3]
        );

        allPlayers.Add(player);

        ListPlayersSend();
    }

    public void ListPlayersSend()
    {
        object[] package = new object[allPlayers.Count + 1];

        package[0] = state;

        for(var i = 0; i < allPlayers.Count; i++)
        {
            object[] piece = new object[4];
            piece[0] = allPlayers[i].name;
            piece[1] = allPlayers[i].actor;
            piece[2] = allPlayers[i].kills;
            piece[3] = allPlayers[i].death;

            package[i + 1] = piece;
        }

        PhotonNetwork.RaiseEvent(
            (byte)EventCodes.ListPlayers,
            package,
            new RaiseEventOptions{ Receivers = ReceiverGroup.All },
            new SendOptions { Reliability = true }
        );
    }

    public void ListPlayersReceive(object[] dataReceived)
    {
        allPlayers.Clear();

        state = (GameState)dataReceived[0];

        for(var i = 1; i < dataReceived.Length; i++)
        {
            object[] piece = (object[])dataReceived[i];

            PlayerInfo player = new PlayerInfo(
                (string)piece[0], 
                (int)piece[1], 
                (int)piece[2], 
                (int)piece[3]
            );

            allPlayers.Add(player);

            if(PhotonNetwork.LocalPlayer.ActorNumber == player.actor)
            {
                index = i - 1;
            }
        }

        StateCheck();
    }

    public void UpdateStatsSend(int actorSending, int statToUpdate, int amountToChange)
    {
        object[] package = new object[] 
        {
            actorSending, 
            statToUpdate,
            amountToChange
        };

        PhotonNetwork.RaiseEvent(
            (byte)EventCodes.UpdateStat,
            package,
            new RaiseEventOptions{ Receivers = ReceiverGroup.All },
            new SendOptions { Reliability = true }
        );
    }

    public void UpdateStatsReceive(object[] dataReceived)
    {
        int actor = (int)dataReceived[0];
        int statType = (int)dataReceived[1];
        int amount = (int)dataReceived[2];

        for(var i = 0; i < allPlayers.Count; i++)
        {
            if(allPlayers[i].actor == actor)
            {
                switch(statType)
                {
                    case 0:
                        allPlayers[i].kills += amount;
                        break;
                    case 1:
                        allPlayers[i].death += amount;
                        break;
                }

                if(i == index)
                {
                    UpdateStatsDisplay();
                }
                if(UIController.instance.leaderboard.activeInHierarchy)
                {
                    ShowLeaderboard();
                }

                break;
            }
        }

        ScoreCheck();
    }

    public void UpdateStatsDisplay()
    {
        if(allPlayers.Count > index)
        {
            UIController.instance.killsText.text = $"Kills: {allPlayers[index].kills}";
            UIController.instance.deathsText.text = $"Deaths: {allPlayers[index].death}";
        }
        else
        {
            UIController.instance.killsText.text = "Kills: 0}";
            UIController.instance.deathsText.text = "Deaths: 0}";
        }
    }
    
    void ShowLeaderboard()
    {
        UIController.instance.leaderboard.SetActive(true);

        foreach(LeaderboardPlayer lp in lboardPlayers)
        {
            Destroy(lp.gameObject);
        }
        lboardPlayers.Clear();

        UIController.instance.leaderboardPlayerDisplay.gameObject.SetActive(false);

        List<PlayerInfo> sorted = SortPlayers(allPlayers);

        foreach (PlayerInfo player in sorted)
        {
            LeaderboardPlayer newPlayerDisplay = Instantiate(UIController.instance.leaderboardPlayerDisplay, UIController.instance.leaderboardPlayerDisplay.transform.parent);
        
            newPlayerDisplay.SetDetails(player.name, player.kills, player.death);

            newPlayerDisplay.gameObject.SetActive(true);

            lboardPlayers.Add(newPlayerDisplay);
        }
    }

    private List<PlayerInfo> SortPlayers(List<PlayerInfo> players)
    {
        List<PlayerInfo> sorted = new List<PlayerInfo>();

        while(sorted.Count < players.Count)
        {
            int highest = -1;
            PlayerInfo selectedPlayer = players[0];

            foreach(PlayerInfo player in players)
            {
                if(!sorted.Contains(player))
                {
                    if(player.kills > highest)
                    {
                        selectedPlayer = player;
                        highest = player.kills;
                    }
                }
            }
            sorted.Add(selectedPlayer);
        }
        return sorted;
    }


    public override void OnLeftRoom()
    {
        base.OnLeftRoom();

        SceneManager.LoadScene(0);
    }

    void ScoreCheck()
    {
        Debug.Log("147");
        bool winnerFound = false;

        foreach(PlayerInfo player in allPlayers)
        {
            if(player.kills >= killsToWin && killsToWin > 0)
            {
                winnerFound = true;
                break;
            }
        }

        if(winnerFound)
        {
            if(PhotonNetwork.IsMasterClient && state != GameState.Ending)
            {
                state = GameState.Ending;
                ListPlayersSend();
            }
        }
    }

    void StateCheck()
    {
        Debug.Log("146");
        if(state == GameState.Ending)
        {
            EndGame();
        }
    }

    void EndGame()
    {
        Debug.Log("145");
        state = GameState.Ending;

        if(PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.DestroyAll();
        }

        UIController.instance.endScreen.SetActive(true);
        ShowLeaderboard();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Camera.main.transform.position = mapCamPoint.position;
        Camera.main.transform.rotation = mapCamPoint.rotation;

        StartCoroutine(EndCo());
        
    }

    private IEnumerator EndCo()
    {
        yield return new WaitForSeconds(waitAfterEnding);


        if(!perpetual)
        {
            PhotonNetwork.AutomaticallySyncScene = false;
            PhotonNetwork.LeaveRoom();
        }
        else
        {
            if(PhotonNetwork.IsMasterClient)
            {
                if(!Launcher.instance.changeMapBetweenRouns)
                {
                    NextMatchSend();
                }
                else
                {
                    int newLevel = Random.Range(0, Launcher.instance.allMaps.Length);

                    if(Launcher.instance.allMaps[newLevel] == SceneManager.GetActiveScene().name)
                    {
                        NextMatchSend();
                    }
                    else
                    {
                        PhotonNetwork.LoadLevel(Launcher.instance.allMaps[newLevel]);
                    }
                }
            }
        }
    }

    public void NextMatchSend()
    {
        PhotonNetwork.RaiseEvent(
            (byte)EventCodes.NextMatch,
            null,
            new RaiseEventOptions{ Receivers = ReceiverGroup.All },
            new SendOptions { Reliability = true }
        );
    }

    public void NextMatchReceive()
    {
        state = GameState.Playing;

        UIController.instance.endScreen.SetActive(false);
        UIController.instance.leaderboard.SetActive(false);

        foreach(PlayerInfo player in allPlayers)
        {
            player.kills = 0;
            player.death = 0;
        }

        UpdateStatsDisplay();

        PlayerSpawner.instance.SpawnPlayer();
        SetupTimer();
    }

    public void SetupTimer()
    {
        if(matchLenght > 0)
        {
            currentMatchTime = matchLenght;

            UpdateTimerDisplay();
        }
    }

    public void UpdateTimerDisplay()
    {
        var timeToDisplay = System.TimeSpan.FromSeconds(currentMatchTime); 

        UIController.instance.timerText.text = timeToDisplay.Minutes.ToString("00") + ":" + timeToDisplay.Seconds.ToString("00");
    }

    public void TimerSend()
    {
        object[] package = new object[] { (int)currentMatchTime, state };

        PhotonNetwork.RaiseEvent(
            (byte)EventCodes.TimerSync,
            package,
            new RaiseEventOptions{ Receivers = ReceiverGroup.All },
            new SendOptions { Reliability = true }
        );
    }

    public void TimerReceive(object[] dataReceived)
    {
        currentMatchTime = (int)dataReceived[0];
        state = (GameState)dataReceived[1];

        UpdateTimerDisplay();

        UIController.instance.timerText.gameObject.SetActive(true);

    }
}

[System.Serializable]
public class PlayerInfo 
{
    public string name;
    public int actor, kills, death;

    public PlayerInfo(string name, int actor, int kills, int death)
    {
        this.name = name;
        this.actor = actor;
        this.kills = kills;
        this.death = death;
    }
    
}