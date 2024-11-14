using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Game : MonoBehaviour
{
    private static Game _instance;
    public static Game instance {get{return _instance;}}

    //gameState
    Player player; 

    void Awake() {
        if(_instance != null && _instance != this){
            Destroy(gameObject);
        }
        else{
            _instance = this;
        }
    }
    
    void Start()
    {

    }
    
    void Update()
    {

    }
}
