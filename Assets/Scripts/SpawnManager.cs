using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    public static SpawnManager instance;

    [SerializeField] private List<Transform> spawnPoins;

    void Awake()
    {
        instance = this;
    }


    // Start is called before the first frame update
    void Start()
    {
        foreach(Transform spawn in spawnPoins)
        {
            spawn.gameObject.SetActive(false);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    public Transform GetSpawnPoint()
    {
        return spawnPoins[Random.Range(0, spawnPoins.Count)];
    }
}
