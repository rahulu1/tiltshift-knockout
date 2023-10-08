using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HammerheadController : MonoBehaviour
{
    [SerializeField] float powFactor;
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnCollisionEnter(Collision other)
    {
        Debug.Log(other.gameObject.name);
        other.rigidbody.velocity += Vector3.up * powFactor;
    }
}
