using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

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
    [SerializeField] private float maxHeat = 10f;
//    [SerializeField] private float heatPerShot = 1f;
    [SerializeField] private float coolRate = 4f;
    [SerializeField] private float overheatCoolRate = 5f;
    [SerializeField] private bool invertLook;

    [SerializeField] private float knifeAttackRange = 2f; // Jarak serangan pisau
    [SerializeField] private int knifeAttackDamage = 25;

    [SerializeField] private GameObject playerHitImpact;

    [SerializeField] private float muzzleDisplayTime;
    [SerializeField] private GameObject knifeObject;
    private Animator knifeAnimator;
    private float _muzzleCounter;

    private int _selectedGun;
    private float _shotCounter;
    private float _verticalRotStore;
    private float _activeMoveSpeed;
    private float _heatCouner;
    private bool _isGrounded;
    private bool _overHeated;

    
    private Vector2 _mouseInput;
    private Vector3 _moveDirection;
    private Vector3 _movement;
    private Camera _cam;

    [SerializeField] private int maxHealth = 100;

    [SerializeField] private Animator anim;
    [SerializeField] private GameObject playerModel;
    [SerializeField] private Transform modelGunPoint;
    [SerializeField] private Transform gunHolder;


    public Material[] allSkins;

    public float adsSpeed = 5f;

    private int _currentHealth;

    public Transform adsOutPoint, adsInPoint;

    public AudioSource slow, fast;
    void Start()
    {

        if (knifeObject != null)
        {
            knifeAnimator = knifeObject.GetComponent<Animator>();
        }
        Cursor.lockState = CursorLockMode.Locked;
        UIController.instance.weaponTempSlider.maxValue = maxHeat;
        _cam = Camera.main;

        //SwitchGun();

        photonView.RPC("SetGun", RpcTarget.All, _selectedGun);

        _currentHealth = maxHealth;


        if(photonView.IsMine)
        {
            playerModel.SetActive(false);

            UIController.instance.healthSlider.maxValue = maxHealth;
            UIController.instance.healthSlider.value = _currentHealth;
        }
        else{
            gunHolder.parent = modelGunPoint;
            gunHolder.localPosition = Vector3.zero;
            gunHolder.localRotation = Quaternion.identity;
        }

        int skinIndex = (photonView.Owner.ActorNumber - 2 + allSkins.Length) % allSkins.Length;
        playerModel.GetComponent<Renderer>().material = allSkins[skinIndex];



        // Transform newTrans = SpawnManager.instance.GetSpawnPoint();
        // transform.position = newTrans.position;
        // transform.rotation = newTrans.rotation;
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

            if (Input.GetMouseButtonDown(0))
            {
                if (_selectedGun == 2) // Pemeriksaan jika senjata yang dipilih adalah pisau
                {
                    KnifeAttack();
                }
            }

            _mouseInput = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * mouseSensitivity;

            transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.x, 
                transform.rotation.eulerAngles.y + _mouseInput.x ,transform.rotation.eulerAngles.z);

            _verticalRotStore += _mouseInput.y;
            _verticalRotStore = Mathf.Clamp(_verticalRotStore, -60f, 60f);

            //верх вниз
            if(invertLook){
                viewPoint.rotation = Quaternion.Euler(_verticalRotStore, 
                    viewPoint.rotation.eulerAngles.y, viewPoint.rotation.eulerAngles.z);
            }
            else
            {
                viewPoint.rotation = Quaternion.Euler(-_verticalRotStore, 
                    viewPoint.rotation.eulerAngles.y, viewPoint.rotation.eulerAngles.z);
            }

            _moveDirection = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));



            if(Input.GetKey(KeyCode.LeftShift))
            {
                _activeMoveSpeed = runSpeed;

                if(!fast.isPlaying && _moveDirection != Vector3.zero)
                {
                    fast.Play();
                    slow.Stop();
                }
            }
            else
            {
                _activeMoveSpeed = moveSpeed;

                if(!slow.isPlaying && _moveDirection != Vector3.zero)
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

            if(Input.GetButtonDown("Jump") && _isGrounded)
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

            if(!_overHeated)
            {
                if(Input.GetMouseButtonDown(0))
                {
                    Shoot();

                }

                if(Input.GetMouseButton(0) && allGuns[_selectedGun].isAutomatic)
                {
                    _shotCounter -= Time.deltaTime;

                    if(_shotCounter <= 0)
                    {
                        Shoot();
                    }
                }

                _heatCouner -= coolRate * Time.deltaTime;
            }
            else
            {
                _heatCouner -= overheatCoolRate * Time.deltaTime;

                if(_heatCouner <= 0)
                {
                    _overHeated = false;

                    UIController.instance.overheatedMessage.gameObject.SetActive(false);
                }
            }

            if(_heatCouner < 0)
            {
                _heatCouner = 0f;
            }


            UIController.instance.weaponTempSlider.value = _heatCouner;


            if(Input.GetAxisRaw("Mouse ScrollWheel") > 0)
            {
                _selectedGun++;
                if(_selectedGun >= allGuns.Count)
                {
                    _selectedGun = 0;
                }
                //SwitchGun();
                photonView.RPC("SetGun", RpcTarget.All, _selectedGun);

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
            }

            for(var i = 0; i < allGuns.Count; i++)
            {
                if(Input.GetKeyDown((i + 1).ToString()))
                {
                    _selectedGun = i;
                    //SwitchGun();
                    photonView.RPC("SetGun", RpcTarget.All, _selectedGun);
                }
            }

            
            anim.SetBool("grounded", _isGrounded);
            anim.SetFloat("speed", _moveDirection.magnitude);


            if(Input.GetMouseButton(1))
            {
                _cam.fieldOfView =  Mathf.Lerp(_cam.fieldOfView, allGuns[_selectedGun].adsZoom, adsSpeed * Time.deltaTime);
                gunHolder.position = Vector3.Lerp(gunHolder.position, adsInPoint.position, adsSpeed * Time.deltaTime);
            }
            else
            {
                _cam.fieldOfView =  Mathf.Lerp(_cam.fieldOfView, 60f, adsSpeed * Time.deltaTime);
                gunHolder.position = Vector3.Lerp(gunHolder.position, adsOutPoint.position, adsSpeed * Time.deltaTime);
            }



            if(Input.GetKeyDown(KeyCode.Escape))
            {
                Cursor.lockState = CursorLockMode.None;
                
            }
            else if(Cursor.lockState == CursorLockMode.None)
            {
                if(Input.GetMouseButtonDown(0) && !UIController.instance.optionsScreen.activeInHierarchy)
                {
                    Cursor.lockState = CursorLockMode.Locked;
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
                knifeAnimator.SetTrigger("attacking");
                StartCoroutine(ResetAttackingTrigger());
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


    private IEnumerator ResetAttackingTrigger()
    {
        // Tunggu sampai animasi serangan pisau selesai
        yield return new WaitForSeconds(knifeAnimator.GetCurrentAnimatorStateInfo(0).length);

        // Atur kembali trigger "attacking" ke nilai default
        knifeAnimator.ResetTrigger("attacking");
    }
    private void Shoot()
    {
        if (_selectedGun != 2) // Pemeriksaan jika senjata yang dipilih bukan pisau
        {
            allGuns[_selectedGun].muzzleFlash.SetActive(true);
            _muzzleCounter = muzzleDisplayTime;
        }

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



        if(_selectedGun != 2)
        {
            _shotCounter = allGuns[_selectedGun].timeBetweenShots;
            _heatCouner += allGuns[_selectedGun].heatPerShot;
        }

        if (_heatCouner >= maxHeat)
        {
            _heatCouner = maxHeat;
            _overHeated = true;
            UIController.instance.overheatedMessage.gameObject.SetActive(true);
        }

        if (_selectedGun != 2) // Pemeriksaan jika senjata yang dipilih bukan pisau
        {
            allGuns[_selectedGun].muzzleFlash.SetActive(true);
        }

        _muzzleCounter = muzzleDisplayTime;

        allGuns[_selectedGun].shotSound.Stop();
        allGuns[_selectedGun].shotSound.Play();
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
        if(gunToSwitchTo < allGuns.Count)
        {
            _selectedGun = gunToSwitchTo;
            SwitchGun();
        }
    }

}
