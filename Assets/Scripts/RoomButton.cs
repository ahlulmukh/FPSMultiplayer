using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Photon.Realtime;

public class RoomButton : MonoBehaviour
{
    [SerializeField] private TMP_Text buttonText;
    private RoomInfo _info;


    public void SetButtonDetails(RoomInfo inputInfo)
    {
        _info = inputInfo;

        buttonText.text = _info.Name;
    }

    public void OpenRoom()
    {
        Launcher.instance.JoinRoom(_info);
    }

}
