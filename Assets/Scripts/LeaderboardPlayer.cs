using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class LeaderboardPlayer : MonoBehaviour
{
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private TMP_Text playerKillsText;
    [SerializeField] private TMP_Text playerDeathsText;


    public void SetDetails(string name, int kills, int deaths)
    {
        playerNameText.text = name;
        playerKillsText.text = kills.ToString();
        playerDeathsText.text = deaths.ToString();
    }


}
