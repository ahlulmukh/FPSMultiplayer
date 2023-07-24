using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyOverTime : MonoBehaviour
{
    [SerializeField] private float lifetime = 1.5f;
    
    void Start()
    {
        Destroy(gameObject, lifetime);
    }

}
