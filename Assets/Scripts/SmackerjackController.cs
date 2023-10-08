using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SmackerjackController : MonoBehaviour
{
    float _leftPos = 5f;
    float _rightPos = 175f;
    float _currPos;
    [SerializeField] float hitStrength;
    float _damper = 150f;
    HingeJoint _hinge;

    // Start is called before the first frame update
    void Start()
    {
        _hinge = GetComponent<HingeJoint>();
        _currPos = _leftPos;
    }

    // Update is called once per frame
    void Update()
    {
        var spring = new JointSpring();
        spring.spring = hitStrength;
        spring.damper = _damper;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (_currPos == _leftPos)
                _currPos = _rightPos;
            else
                _currPos = _leftPos;
        }

        spring.targetPosition = _currPos;

        _hinge.spring = spring;
    }
}