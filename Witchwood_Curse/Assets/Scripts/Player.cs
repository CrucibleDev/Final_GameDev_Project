using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Player : MonoBehaviour
{
    public Camera playerCamera;
    GameManager gm = GameManager.instance;

    int health = 10;
    float speed = 10f;
    float damage = 10f;
    float range = 10f;
    float spread = 1f;
    Vector3 InputVector = Vector3.zero;

    bool isAlive = true;
    bool isDebugSet = true;
    
    void Start()
    {
        playerCamera = Camera.main;
    }
    
    void Update()
    {
        //Update Stats
        isAlive = health > 0;

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

        Vector3 moveDirection = forward * InputVector.z + right * InputVector.x;


        //Debug line
        if(isDebugSet) {
            Debug.DrawRay(transform.position + new Vector3(0,0.5f,0), moveDirection * speed, Color.green); 
        }

        //Set position
        transform.position += moveDirection * speed * Time.deltaTime; 
    }
}
