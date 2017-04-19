using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;

public enum MyState
{
    Start,
    Loop,
    End
}
public class Main : MonoBehaviour
{
    CustomFSMManager customFSMManager;

    void Start ()
    {
        customFSMManager = gameObject.AddComponent<CustomFSMManager>();
        customFSMManager.Initialize(typeof(MyState), this);

        customFSMManager.StateMachineChange(MyState.Loop);
        // Should print
        // Exit Start to Loop
        // Enter Loop from Start
        // Exit Loop to End
        // Enter End from Loop

    }

    void StateMachineExit_Start(Enum state, Dictionary<string, object> options = null)
    {
        Debug.Log("Exit Start to " + state);
    }

    void StateMachineEnter_Loop(Enum state, Dictionary<string, object> options = null)
    {
        Debug.Log("Enter Loop from " + state);
        customFSMManager.StateMachineChange(MyState.End);
    }

    void StateMachineExit_Loop(Enum state, Dictionary<string, object> options = null)
    {
        Debug.Log("Exit Loop to " + state);
    }

    void StateMachineEnter_End(Enum state, Dictionary<string, object> options = null)
    {
        Debug.Log("Enter End from " + state);
    }

    bool StateMachineUpdate_End(float deltaTime)
    {
        Debug.Log("Update end");
        return false;
    }

}
