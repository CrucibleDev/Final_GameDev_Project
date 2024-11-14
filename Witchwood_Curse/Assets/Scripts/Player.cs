using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Player : MonoBehaviour
{
    public Camera playerCamera;
    Game gm = Game.instance;
    Rigidbody rb;

    int health = 10;
    float maxVelocity = 2f;
    float damage = 10f;
    float range = 10f;
    float spread = 1f;
    Vector3 InputVector = Vector3.zero;

    bool isAlive = true;
    bool isDebugSet = true;

    Vector3 moveDirection = Vector3.zero;
    bool dashToggle = false;
    int dashFrameCount = 0;
    
    void Start()
    {
        playerCamera = Camera.main;
        rb = gameObject.GetComponent<Rigidbody>();
    }
    
    void Update(){
        Inputs();
    }

    void FixedUpdate()
    {
        //Update Stats
        isAlive = health > 0;

        //Debug line
        if(isDebugSet) {
            Debug.DrawRay(transform.position + new Vector3(0,0.5f,0), moveDirection * 5, Color.green); 
        }

        //Set position
        if(dashToggle){
            //Iframes too
            rb.AddForce(moveDirection * dashFrameCount * 2,ForceMode.Impulse);
            dashFrameCount--;
            if(dashFrameCount <= 0) {
                dashToggle = false;
            }
        }
        else{
            rb.AddForce(moveDirection * maxVelocity * 10, ForceMode.VelocityChange);
            //rb.velocity = moveDirection * maxVelocity;

            if(rb.velocity.magnitude > maxVelocity){
                rb.velocity = moveDirection * maxVelocity;
            }
            //transform.position += moveDirection * speed * Time.deltaTime; 
        }

    }

    void Inputs(){
        Vector3 forward = playerCamera.transform.forward;
        Vector3 right = playerCamera.transform.right;

        forward.y = 0;
        forward.Normalize();

        right.y = 0;
        right.Normalize();

        InputVector = Vector3.zero;

        //Input
        if(Input.GetKey(KeyCode.W)){
            InputVector.z = 1f; 
        }
        if(Input.GetKey(KeyCode.S)){
            InputVector.z = -1f; 
        }
        if(Input.GetKey(KeyCode.D)){
            InputVector.x = 1f; 
        }
        if(Input.GetKey(KeyCode.A)){
            InputVector.x = -1f; 
        }
        InputVector.Normalize();

        moveDirection = forward * InputVector.z + right * InputVector.x;

        if(Input.GetKeyDown(KeyCode.LeftShift) && moveDirection != Vector3.zero){
            dashToggle = true;
            dashFrameCount = 8;
            //rb.AddForce(moveDirection,ForceMode.Impulse);
        }
    }
}
