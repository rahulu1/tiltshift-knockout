using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WheelHandler : MonoBehaviour
{
    [SerializeField] GameObject wheelBody;
    
    [SerializeField] AnimationCurve gripCurve; // Used to determine traction based on current velocity

    [SerializeField] AnimationCurve torqueCurve; // Used to determine engine power based on current velocity

    [SerializeField] AnimationCurve brakeCurve;
    
    Rigidbody _carRigid;
    
    Transform _tireTransform;

    Transform _carTransform;
    
    [SerializeField] float maxRaycastDistance;
    
    [SerializeField] float tireMass; // "Virtual" mass to calculate force on car

    [SerializeField] float tireRestPos; // Where tire wants to be aka ideal height off ground
    
    float _tireRadius; // Used to determine where to place tire mesh (visual only)

    float _tireCircumference;

    float _tireRenderPos;

    float _tireRenderRotation;

    [SerializeField] float minSteeringAngle;

    [SerializeField] float maxSteeringAngle;

    [SerializeField] float neutralSteeringAngle;

    [SerializeField] float steeringSpeed;

    float _currSteeringAngle;

    [SerializeField] float carTopSpeed;

    [SerializeField] float accelerationFactor;

    [SerializeField] float coilStrength;

    [SerializeField] float coilDamper;

    float _coilForce;

    [SerializeField] float rollingFriction;

    [SerializeField] bool handbrakeable;

    [SerializeField] int _pID;

    string _horAxis, _vertAxis, _brakeButton, _handbrakeButton;
    
    // Start is called before the first frame update
    void Start()
    {
        _tireTransform = this.transform;
        _carTransform = GetComponentInParent<Transform>();
        _carRigid = GetComponentInParent<Rigidbody>();
        _tireRadius = wheelBody.GetComponent<Renderer>().bounds.extents.z;
        _tireCircumference = 2f * Mathf.PI * _tireRadius;

        _horAxis = "Horizontal";
        _vertAxis = "Vertical";
        _brakeButton = "Brake";
        _handbrakeButton = "Handbrake";
        
        if (_pID == 2)
        {
            _horAxis = "HorizontalP2";
            _vertAxis = "VerticalP2";
            _brakeButton = "BrakeP2";
            _handbrakeButton = "HandbrakeP2";
        }
    }

    void Update()
    {
        SetWheelAngle();
        wheelBody.transform.position = _tireTransform.position - (_tireTransform.up * _tireRenderPos);
        HandleRenderRotation();
    }

    void FixedUpdate()
    {
        _tireRenderPos = maxRaycastDistance - _tireRadius;
        
        // Cast ray downwards to see where tire touches ground, if at all
        if (Physics.Raycast(transform.position, _tireTransform.up * -1f, out RaycastHit tireRay, maxRaycastDistance))
        {
            HandleSuspension(tireRay);
            HandleSteering();
            HandleAcceleration();
            HandleFriction();
            HandleBraking();
        }
    }

    void HandleSuspension(RaycastHit tireRay)
    {
        _tireRenderPos = tireRay.distance - _tireRadius;
        
        var coilDirection = _tireTransform.up;

        var tireWorldVel = _carRigid.GetPointVelocity(_tireTransform.position);

        var offset = tireRestPos - tireRay.distance;

        var vel = Vector3.Dot(coilDirection, tireWorldVel);

        _coilForce = (offset * coilStrength) - (vel * coilDamper);

        _carRigid.AddForceAtPosition(coilDirection * _coilForce, _tireTransform.position);
    }
    
    void HandleSteering()
    {
        var steeringDirection = _tireTransform.right;

        var tireWorldVel = _carRigid.GetPointVelocity(_tireTransform.position);

        var steeringVel = Vector3.Dot(steeringDirection, tireWorldVel);

        var desiredVelChange = -steeringVel * gripCurve.Evaluate(steeringVel / tireWorldVel.magnitude);

        var desiredAccel = desiredVelChange / Time.fixedDeltaTime;
        
        _carRigid.AddForceAtPosition(tireMass * desiredAccel * steeringDirection, _tireTransform.position);
    }
    
    void HandleAcceleration()
    {
        if (handbrakeable && Input.GetKey(KeyCode.LeftShift))
            return;
        
        var accelDirection = _tireTransform.forward;
        var accelInput = Input.GetAxis(_vertAxis);
        
        // Accelerating
        if(!Mathf.Approximately(0f, accelInput))
        {
            var carSpeed = Vector3.Dot(_carTransform.forward, _carRigid.velocity);

            var normalizeSpeed = Mathf.Clamp01(Mathf.Abs(carSpeed) / carTopSpeed);

            var availableTorque = torqueCurve.Evaluate(normalizeSpeed) * accelInput * accelerationFactor;
            
            _carRigid.AddForceAtPosition(availableTorque * tireMass * accelDirection, _tireTransform.position);
        }
    }
    
    // Called by handle acceleration if no input
    void HandleFriction()
    {
        var frictionForce = (rollingFriction * _coilForce) * -1f * _carRigid.GetPointVelocity(_tireTransform.position);
        
        _carRigid.AddForceAtPosition(frictionForce, _tireTransform.position);
    }
    
    // Called by handle acceleration if braking
    void HandleBraking()
    {
        if (Input.GetButton(_brakeButton))
        {
            var carSpeed = Vector3.Dot(_carTransform.forward, _carRigid.velocity);

            var normalizeSpeed = Mathf.Clamp01(Mathf.Abs(carSpeed) / carTopSpeed);

            var brakeFriction = brakeCurve.Evaluate(normalizeSpeed);
            
            var brakeForce = (brakeFriction * _coilForce) * -1f * _carRigid.GetPointVelocity(_tireTransform.position);
        
            _carRigid.AddForceAtPosition(brakeForce, _tireTransform.position);
        }
    }
    
    // Called every update to set wheel rotation (clamp max steering per wheel, 0 means no steering)
    void SetWheelAngle()
    {
        var steerInput = Input.GetAxis(_horAxis);
        if (Mathf.Approximately(0f, steerInput))
            _currSteeringAngle = Mathf.MoveTowardsAngle(_currSteeringAngle, neutralSteeringAngle,
                steeringSpeed * Time.deltaTime);
        else
            _currSteeringAngle = Mathf.Clamp(_currSteeringAngle + steerInput * steeringSpeed
                                                                         * Time.deltaTime, minSteeringAngle, maxSteeringAngle);

        transform.localRotation = Quaternion.Euler(_currSteeringAngle * Vector3.up);
    }
    
    void HandleRenderRotation()
    {
        var tireWorldVel = _carRigid.GetPointVelocity(_tireTransform.position);

        var tireForwardVel = Vector3.Dot(_tireTransform.forward, tireWorldVel);

        var tireRotationPerSec = tireForwardVel / _tireCircumference;

        var tireRotationAmount = Vector3.right * (tireRotationPerSec * Time.deltaTime * 360f);

        wheelBody.transform.Rotate(tireRotationAmount, Space.Self);
    }

    public void setPID(int playerID) { _pID = playerID; }
}
