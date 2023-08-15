using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using TMPro;
using UnityEngine.InputSystem;
using UnityStandardAssets.CrossPlatformInput;
using UnityEngine.EventSystems;

public class PlayerController : MonoBehaviourPunCallbacks
{
    [SerializeField] private Transform viewPoint;
    [SerializeField] private Transform groundCheckPoint;
    [SerializeField] private GameObject bulletImpact;
    [SerializeField] private LayerMask groundLayers; 
    [SerializeField] private CharacterController charController;

    [SerializeField] private List<Gun> allGuns;

    [SerializeField] private float mouseSensitivity = 1f;
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float runSpeed = 7f;
    [SerializeField] private float jumpForce = 11f;
    [SerializeField] private float gravityMod = 7f;
//    [SerializeField] private float timeBetweenShots = .1f;
//    [SerializeField] private float heatPerShot = 1f;
    [SerializeField] private float coolRate = 4f;
    [SerializeField] private float overheatCoolRate = 5f;
    [SerializeField] private bool invertLook;
    [SerializeField] private float knifeAttackRange = 2f; // Jarak serangan pisau
    [SerializeField] private int knifeAttackDamage = 25;

    [SerializeField] private GameObject playerHitImpact;

    [SerializeField] private float muzzleDisplayTime;
    [SerializeField] private GameObject knifeObject;
    [SerializeField] private GameObject handgunObject;
    private Animator knifeAnimator;
    private Animator handgunAnimator;
    private float _muzzleCounter;



    private int _selectedGun;
    private float _shotCounter;
    private float fireCounter;
    private float _verticalRotStore;
    private float _activeMoveSpeed;
    private float _heatCouner;
    private bool _isGrounded;



    
    private Vector2 _mouseInput;
    private Vector3 _moveDirection;
    private Vector3 _movement;
    private Camera _cam;

    [SerializeField] private int maxHealth = 100;

    [SerializeField] private Animator anim;
    [SerializeField] private GameObject playerModel;
    [SerializeField] private GameObject armsHandgun;
    [SerializeField] private GameObject armsRifle;
    [SerializeField] private GameObject armsKnife;
    [SerializeField] private Transform modelGunPoint;
    [SerializeField] private Transform gunHolder;

    [SerializeField] private Animator animMale;

    [SerializeField] private Animator animHandgun;

    [SerializeField] private Animator animRifle;


    public Material[] allSkins;
    public float adsSpeed = 5f;
    private int _currentHealth;
    public Transform adsOutPoint, adsInPoint;


    [Header("Sound Step Settings")]
    public AudioSource slow, fast;


    [Header("Recoil Settings")]
    //[Range(0, 1)]
    //public float recoilPercent = 0.3f;
    [Range(0, 2)]
    public float recoverPercent = 0.7f;
    [Space]
    public float recoilUp = 1f;
    public float recoilBack = 0;


    private Vector3 originalPosition;
    private Vector3 recoilVelocity = Vector3.zero;

    private float recoilLength;
    private float recoverLenth;

    private bool recoiling;
    public bool recovering;

    [Header("Weapon Settings")]
    private bool isReloading;
    private TextMeshProUGUI ammoText;
    private Coroutine reloadCo;
    private Coroutine reloadAnimationCo;

    private FixedJoystick joystick;



    void Start()
    {
        joystick = UIController.instance.joystick;

        if (knifeObject != null)
        {
            knifeAnimator = knifeObject.GetComponent<Animator>();
        }

        //Cursor.lockState = CursorLockMode.Locked;
        _cam = Camera.main;
        //SwitchGun();
        photonView.RPC("SetGun", RpcTarget.All, _selectedGun);
        _currentHealth = maxHealth;


        if(photonView.IsMine)
        {
            playerModel.SetActive(false);
            ammoText = UIController.instance.ammoText;
            _shotCounter = 0f;
            allGuns[_selectedGun].currentAmmo = allGuns[_selectedGun].ammo;

            UIController.instance.healthSlider.maxValue = maxHealth;
            UIController.instance.healthSlider.value = _currentHealth;

            UpdateUI();
        }
        else{
            armsHandgun.SetActive(false);
            armsRifle.SetActive(false);
            armsKnife.SetActive(false);
            gunHolder.parent = modelGunPoint;
            gunHolder.localPosition = Vector3.zero;
            gunHolder.localRotation = Quaternion.identity;
        }

        int skinIndex = (photonView.Owner.ActorNumber - 2 + allSkins.Length) % allSkins.Length;
        playerModel.GetComponent<Renderer>().material = allSkins[skinIndex];
        originalPosition = allGuns[_selectedGun].transform.localPosition;
        recoilLength = 0;
        recoverLenth = 1 / _shotCounter * recoverPercent;

    }


    private void LateUpdate() {

        if(photonView.IsMine)
        {
            if(MatchManager.instance.state == MatchManager.GameState.Playing)
            {
                _cam.transform.position = viewPoint.transform.position;
                _cam.transform.rotation = viewPoint.transform.rotation;
            }
            else
            {
                _cam.transform.position = MatchManager.instance.mapCamPoint.position;
                _cam.transform.rotation = MatchManager.instance.mapCamPoint.rotation;
            }



        }

    }

    void Update()
    {

        if(photonView.IsMine)
        {

            if (SimpleInput.GetButtonDown("Shoot"))
             {
                  if (_selectedGun == 2) // Pemeriksaan jika senjata yang dipilih adalah pisau
                {
                      KnifeAttack();
             }
             }


            _mouseInput = new Vector2(SimpleInput.GetAxis("Look X"), SimpleInput.GetAxis("Look Y")) * mouseSensitivity;

           transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.x,
               transform.rotation.eulerAngles.y + _mouseInput.x, transform.rotation.eulerAngles.z);

            _verticalRotStore += _mouseInput.y;
            _verticalRotStore = Mathf.Clamp(_verticalRotStore, -60f, 60f);

            if (invertLook)
            {
                viewPoint.rotation = Quaternion.Euler(_verticalRotStore,
                    viewPoint.rotation.eulerAngles.y, viewPoint.rotation.eulerAngles.z);
            }
            else
            {
                viewPoint.rotation = Quaternion.Euler(-_verticalRotStore,
                    viewPoint.rotation.eulerAngles.y, viewPoint.rotation.eulerAngles.z);
            } 
          

            // Memproses input untuk pergerakan karakter dari joystick
            _moveDirection = new Vector3(joystick.Horizontal, 0f, joystick.Vertical);
    

            if (Input.GetKey(KeyCode.LeftShift))
            {
                _activeMoveSpeed = runSpeed;
                if (!fast.isPlaying && _moveDirection != Vector3.zero)
                {
   
                    fast.Play();
                    slow.Stop();
                }
            }
            else
            {
                _activeMoveSpeed = moveSpeed;
               
                if (!slow.isPlaying && _moveDirection != Vector3.zero)
                {
                    fast.Stop();
                    slow.Play();
                }
            }


            if(_moveDirection == Vector3.zero || !_isGrounded)
            {
                fast.Stop();
                slow.Stop();
            }


            float yVal = _movement.y;
            
            _movement = ((transform.forward * _moveDirection.z) + (transform.right * _moveDirection.x)).normalized * _activeMoveSpeed;

            if(!charController.isGrounded)
            {
                _movement.y = yVal;
            }

             _isGrounded = Physics.Raycast(groundCheckPoint.position, Vector3.down, .25f, groundLayers);

             if(SimpleInput.GetButtonDown("Jump") && _isGrounded)
             {

                 _movement.y = jumpForce;
             }
           

            _movement.y += Physics.gravity.y * Time.deltaTime * gravityMod;

            charController.Move(_movement * Time.deltaTime);


            if(allGuns[_selectedGun].muzzleFlash.activeInHierarchy)
            {
                _muzzleCounter -= Time.deltaTime;

                if(_muzzleCounter <= 0)
                {
                    allGuns[_selectedGun].muzzleFlash.SetActive(false);
                }
            }

            CheckForShot();
            CheckForReload();
            CountdownTimeBetweenShots();


/*            if (Input.GetAxisRaw("Mouse ScrollWheel") > 0)
            {
                _selectedGun++;
                if(_selectedGun >= allGuns.Count)
                {
                    _selectedGun = 0;
                }

                //SwitchGun();
                photonView.RPC("SetGun", RpcTarget.All, _selectedGun);
                UpdateUI();

            } 
            else if(Input.GetAxisRaw("Mouse ScrollWheel") < 0)
            {
                _selectedGun--;
                if(_selectedGun < 0)
                {
                    _selectedGun = allGuns.Count - 1;
                }
                //SwitchGun();
                photonView.RPC("SetGun", RpcTarget.All, _selectedGun);
                UpdateUI();
            }*/


            if (SimpleInput.GetButtonDown("Weapon"))
            {
                _selectedGun = (_selectedGun + 1) % allGuns.Count;
                //SwitchGun();
                photonView.RPC("SetGun", RpcTarget.All, _selectedGun);
                UpdateUI();
            }

            /*     for (var i = 0; i < allGuns.Count; i++)
                 {
                     if(SimpleInput.GetButtonDown("Weapon"))
                     {
                         _selectedGun = i;
                         //SwitchGun();
                         photonView.RPC("SetGun", RpcTarget.All, _selectedGun);
                         UpdateUI();
                     }
                 }*/


            anim.SetBool("grounded", _isGrounded);
            anim.SetFloat("speed", _moveDirection.magnitude);

            // Male Animasi
            animMale.SetBool("grounded", _isGrounded);
            animMale.SetFloat("speed", _moveDirection.magnitude);

            // Handgun Animasi
            animHandgun.SetBool("grounded", _isGrounded);
            animHandgun.SetFloat("speed", _moveDirection.magnitude);

            // Handgun Animasi
            animRifle.SetBool("grounded", _isGrounded);
            animRifle.SetFloat("speed", _moveDirection.magnitude);


            if (SimpleInput.GetButton("Scope"))
            {
                _cam.fieldOfView =  Mathf.Lerp(_cam.fieldOfView, allGuns[_selectedGun].adsZoom, adsSpeed * Time.deltaTime);
                gunHolder.position = Vector3.Lerp(gunHolder.position, adsInPoint.position, adsSpeed * Time.deltaTime);
            }
            else 
            {
                _cam.fieldOfView =  Mathf.Lerp(_cam.fieldOfView, 60f, adsSpeed * Time.deltaTime);
                gunHolder.position = Vector3.Lerp(gunHolder.position, adsOutPoint.position, adsSpeed * Time.deltaTime);
            }



            if(SimpleInput.GetButtonDown("Pause"))
            {
                Cursor.lockState = CursorLockMode.None;
                
            }
            else if(Cursor.lockState == CursorLockMode.None)
            {
                if(Input.GetMouseButtonDown(0) && !UIController.instance.optionsScreen.activeInHierarchy)
                {
                    //Cursor.lockState = CursorLockMode.Locked;
                }
            }

        }
    }


    private void CheckForShot()
    {

        if (allGuns[_selectedGun].isMale)
        {
            if (SimpleInput.GetButtonDown("Shoot"))
            {
                KnifeAttack();
            }
        }
        else
        {
            if (isReloading) { return; }

            if (SimpleInput.GetButtonDown("Shoot"))
            {
                animHandgun.CrossFadeInFixedTime("Fire", 0.01f);
                Shoot();
                recoiling = false;
            }

           // Input.GetMouseButton(0)
              if (SimpleInput.GetButton("Shoot") && allGuns[_selectedGun].isAutomatic)
              {

                _shotCounter -= Time.deltaTime;
                  if (_shotCounter <= 0)
                  {
                      animRifle.CrossFadeInFixedTime("Fire", 0.01f);
                      Shoot();
                      recoiling = false;
                }
              }


        }
    }
 


    public void KnifeAttack()
    {
        if (_selectedGun == 2) // Pemeriksaan jika senjata yang dipilih adalah pisau
        {
            if (knifeAnimator != null)
            {
                // Jalankan animasi serangan pisau
                knifeAnimator.SetTrigger("attack");
            }

            // Periksa jika pisau berhasil mengenai pemain dari jarak dekat
            RaycastHit hit;
            if (Physics.Raycast(viewPoint.position, viewPoint.forward, out hit, knifeAttackRange))
            {
                if (hit.collider.gameObject.tag == "Player")
                {
                    // Kurangi kesehatan pemain
                    hit.collider.gameObject.GetPhotonView().RPC("DealDamage", RpcTarget.All, photonView.Owner.NickName, knifeAttackDamage, PhotonNetwork.LocalPlayer.ActorNumber);
                }
            }
        }
    }



    [PunRPC]
    private void ShowMuzzleFlashRPC()
    {
        allGuns[_selectedGun].muzzleFlash.SetActive(true);
        _muzzleCounter = muzzleDisplayTime;
        StartCoroutine(DeactivateMuzzleFlash());
    }

    private IEnumerator DeactivateMuzzleFlash()
    {
        yield return new WaitForSeconds(muzzleDisplayTime);
        allGuns[_selectedGun].muzzleFlash.SetActive(false);
    }


    private void Shoot()
    {


        /*  if (_selectedGun != 2) // Pemeriksaan jika senjata yang dipilih bukan pisau
          {
              allGuns[_selectedGun].muzzleFlash.SetActive(true);
              _muzzleCounter = muzzleDisplayTime;
          }*/

        if (_shotCounter > 0) { return; }
        if (isReloading) { return; }

        _shotCounter = allGuns[_selectedGun].timeBetweenShots;

        if (allGuns[_selectedGun].currentAmmo <= 0) { ReloadWeapon(); return; }

        Ray ray = _cam.ViewportPointToRay(new Vector3(.5f, .5f, 0f));
        ray.origin = _cam.transform.position;

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.collider.gameObject.tag == "Player")
            {
                hit.collider.gameObject.GetPhotonView().RPC("DealDamage", RpcTarget.All, photonView.Owner.NickName, allGuns[_selectedGun].shotDamage, PhotonNetwork.LocalPlayer.ActorNumber);
            }
            else
            {
                if (_selectedGun != 2) // Pemeriksaan jika senjata yang dipilih bukan pisau
                {
                    GameObject bulletImpactObject = Instantiate(bulletImpact, hit.point + (hit.normal * .002f), Quaternion.LookRotation(hit.normal, Vector3.up));
                    Destroy(bulletImpactObject, 10f);
                }
            }
        }


        /* if (_selectedGun != 2) // Pemeriksaan jika senjata yang dipilih bukan pisau
         {
             allGuns[_selectedGun].muzzleFlash.SetActive(true);
         }*/

        // _muzzleCounter = muzzleDisplayTime;
        photonView.RPC("ShowMuzzleFlashRPC", RpcTarget.All);
        ShowMuzzleFlashRPC();
        _shotCounter = allGuns[_selectedGun].timeBetweenShots;

        allGuns[_selectedGun].ReduceCurrentAmmo();
        allGuns[_selectedGun].shotSound.Stop();
        allGuns[_selectedGun].shotSound.Play();
        UpdateUI();
    }

    private void CountdownTimeBetweenShots()
    {
        if (_shotCounter > 0)
        {
            _shotCounter -= Time.deltaTime;
        }
    }

    private void CheckForReload()
    {
        if (isReloading) { return; }
        if (allGuns[_selectedGun].currentAmmo == allGuns[_selectedGun].ammo) { return; }
        if (Input.GetKeyDown(KeyCode.R))
        {
            ReloadWeapon();
        }
    }


    private void ReloadWeapon()
    {
        isReloading = true;
        if (reloadCo != null) { StopCoroutine(reloadCo); }
        reloadCo = StartCoroutine(ReloadWeaponOverTime());
    }

    IEnumerator ReloadWeaponOverTime()
    {
        if (reloadAnimationCo != null) { StopCoroutine(reloadAnimationCo); }
        //Less bullets to reload less time it takes
        float totalReloadTime = (allGuns[_selectedGun].reloadDuration / allGuns[_selectedGun].ammo) * (allGuns[_selectedGun].ammo - allGuns[_selectedGun].currentAmmo);
        reloadAnimationCo = StartCoroutine(ReloadAmmoUIAnimation(totalReloadTime));
        yield return new WaitForSeconds(totalReloadTime);
        allGuns[_selectedGun].ResetCurrentAmmo();
        UpdateUI();
        isReloading = false;
    }


    IEnumerator ReloadAmmoUIAnimation(float duration)
    {
        ammoText.color = Color.red;
        int startAmmo = allGuns[_selectedGun].currentAmmo;
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            ammoText.text = ((int)Mathf.Lerp(startAmmo, allGuns[_selectedGun].ammo, t / duration)).ToString() + " / " + allGuns[_selectedGun].ammo.ToString();
            yield return null;
        }
    }

    private void UpdateUI()
    {
        ammoText.color = Color.white;
        ammoText.text = allGuns[_selectedGun].currentAmmo.ToString() + " / " + allGuns[_selectedGun].ammo.ToString();
    }

    

    [PunRPC]
    public void DealDamage(string damager, int damageAmount, int actor)
    {
        TakeDamage(damager, damageAmount, actor);
    }

    public void TakeDamage(string damager, int damageAmount, int actor)
    {
        if (photonView.IsMine)
        {
            _currentHealth -= damageAmount;

            if (_currentHealth <= 0)
            {
                _currentHealth = 0;
                PlayerSpawner.instance.Die(damager);
                MatchManager.instance.UpdateStatsSend(actor, 0, 1);
            }

            UIController.instance.healthSlider.value = _currentHealth;
        }
    }


    void SwitchGun()
    {


        foreach (Gun gun in allGuns)
        {
            gun.gameObject.SetActive(false);
        }

        allGuns[_selectedGun].gameObject.SetActive(true);
        allGuns[_selectedGun].muzzleFlash.SetActive(false);


    }


    [PunRPC]
    public void SetGun(int gunToSwitchTo)
    {
        if (gunToSwitchTo < allGuns.Count)
        {
            _selectedGun = gunToSwitchTo;
            SwitchGun();
        }
    }

}
