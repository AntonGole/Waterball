﻿using System;
using DefaultNamespace;
using UnityEngine;
using Random = System.Random;


public class WaterballBallParticles : MonoBehaviour {



    public float rippleThreshold = 0.1f;


    public float rippleSpeedThreshold = 0.5f;
    public float splashSpeedThreshold = 4f; 
    
    public ParticleSystem rippleEffect;
    public ParticleSystem trailEffect; 

    public float emissionHeight = -0.4f; 

    private Rigidbody rb;

    private Random rd = new Random();

    private float tStart;

    public float ripplePeriod = 1;

    private ParticleSystem.MainModule mainModule;
    private ParticleSystem.EmissionModule emissionModule; 
    
    // private float previousTime; 

    private void Start() {
        rb = GetComponent<Rigidbody>();
        tStart = Time.time;
        mainModule = trailEffect.main;
        emissionModule = trailEffect.emission; 
    }


    private void Update() {
        PlayRipple();
        PlaySplash();
    }


    private void PlayRipple() {
        var currentTime = Time.time;
        var translationSpeed = rb.velocity.magnitude;
        var position = rb.position;
        var emissionPosition = new Vector3(position.x, emissionHeight, position.z);
        var height = position.y;
        if (translationSpeed < rippleSpeedThreshold) {
            return;
        }

        if (height > 1) {
            return;
        }

        var nextTime = currentTime + Time.deltaTime;
        var period = ripplePeriod / translationSpeed; 
        var d1 = Math.Floor((currentTime - tStart) / period);
        var d2 = Math.Floor((nextTime - tStart) / period);

        if (d2 <= d1) {
            return; 
        }
        
        var subEmitterInstance = Instantiate(rippleEffect, emissionPosition, Quaternion.identity); 
        subEmitterInstance.Play();
        Destroy(subEmitterInstance.gameObject, subEmitterInstance.main.startLifetime.constantMax);
    }


    private void PlaySplash() {
        var rotationVelocity = rb.angularVelocity; 
        var rotationSpeed = rotationVelocity.magnitude;

        if (rotationSpeed < splashSpeedThreshold) {
            return;
        }

        Quaternion xRotation = Quaternion.AngleAxis(90f, Vector3.up);
        Quaternion zRotation = Quaternion.AngleAxis(90f, Vector3.up);

        var xComponent = new Vector3(rotationVelocity.x, 0, 0); 
        var zComponent = new Vector3(0, 0, rotationVelocity.z); 
        var yComponent = new Vector3(0, rotationVelocity.y, 0); 

        var xDirection = xRotation * xComponent;
        var zDirection = zRotation * zComponent; 

        Quaternion yRotation = Quaternion.AngleAxis(zComponent.magnitude, zComponent.normalized);

        var projectedDirection = yRotation * (zDirection + xDirection); 
        var crossProduct = Vector3.Cross(projectedDirection, Vector3.up);
        var targetDirection = Quaternion.AngleAxis(30f, crossProduct) * projectedDirection;
        var tiltedUp = Vector3.RotateTowards(projectedDirection, targetDirection, 30f * Mathf.Deg2Rad, 0f); 
        
        var normalized = tiltedUp.normalized;

        mainModule.startSpeed = projectedDirection.magnitude*2;
        emissionModule.rateOverTime =  projectedDirection.magnitude * 3;
        
        var trailTransform = trailEffect.transform;
        trailTransform.forward = normalized;
        var bottomPosition = rb.position - Vector3.up * 0.5f; 
        trailTransform.position = bottomPosition; 

    }
    
}



// trailEffect.Play();



// var bottomPosition = GetComponent<MeshRenderer>().bounds.min;


// Debug.Log("normalized: " + normalized);


        // emissionModule.rateOverTime = (float)Math.Pow(2f, projectedDirection.magnitude);



// var mainModule = trailEffect.main;
// var emissionModule = trailEffect.emission; 





// Quaternion xRotation = Quaternion.identity; 
// Quaternion yRotation = Quaternion.AngleAxis(90f, Vector3.up);
// Quaternion zRotation = Quaternion.identity; 




        // Quaternion lookUp = Quaternion.Euler(-30f, 0f, 0f); 
        
        // var newDirection =  

        // var projectedDirection = new Vector3(newDirection.x, 0, newDirection.y); 

        // var projectedDirection = xDirection + zDirection;
        
        
// var tiltedUp =  lookUp * projectedDirection;

