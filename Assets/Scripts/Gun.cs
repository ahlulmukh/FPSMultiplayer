using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gun : MonoBehaviour
{
    public GameObject muzzleFlash;
    public bool isAutomatic;
    public float timeBetweenShots = .1f;
    public float heatPerShot = 1f;

    public float adsZoom;
    public int shotDamage;

    public AudioSource shotSound;

}
