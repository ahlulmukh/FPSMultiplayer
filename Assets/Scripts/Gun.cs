using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gun : MonoBehaviour
{
    public GameObject muzzleFlash;
    public bool isAutomatic;
    public bool isMale;
    public float timeBetweenShots = .1f;
    public float heatPerShot = 1f;
    public int ammo;
    public int currentAmmo;
    public float reloadDuration;
    public float adsZoom;
    public int shotDamage;

    [HideInInspector]
    public bool isMuzzleFlashActive;

    public AudioSource shotSound;

    private void Start()
    {
        currentAmmo = ammo;
    }

    public void ResetCurrentAmmo()
    {
        currentAmmo = ammo;
    }

    public void ReduceCurrentAmmo()
    {
        currentAmmo--;
    }

}
