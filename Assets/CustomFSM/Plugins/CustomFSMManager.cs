using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using System.Reflection;

public class CustomFSMManager : MonoBehaviour
{
    public string fsmName;
    private Enum _state;
    public int state;
    public float currentStateTime = 0;
    public bool autoUpdate;

    private Dictionary<int, Action<Enum, Dictionary<string, object>>> enterLookup = new Dictionary<int, Action<Enum, Dictionary<string, object>>> ();
    private Dictionary<int, Action<Enum, Dictionary<string, object>>> exitLookup = new Dictionary<int, Action<Enum, Dictionary<string, object>>> ();
    private Dictionary<int, Func<float, bool>> updateLookup = new Dictionary<int, Func<float, bool>> ();

    bool stopUpdate = false;
    string enumTypeName;
    string componentName;

    /**
     * Initialize this class with the custom enum.
     * The first enum is the default state
     **/
    public void Initialize (Type stateType, Type comp, bool autoUpdate = true)
    {
        this.componentName = comp.FullName;
        this.enumTypeName = stateType.FullName;
        this.autoUpdate = autoUpdate;

        var values = Enum.GetValues (stateType);
        this._state = (Enum)values.GetValue (0);
        this.state = 0;

        CreateAllDelegates ();
    }

    void CreateAllDelegates ()
    {
        var values = Enum.GetValues (Type.GetType (enumTypeName));
        var comp = this.GetComponent (Type.GetType(componentName));
        string methodName;
        object f;

        enterLookup.Clear ();
        exitLookup.Clear ();
        updateLookup.Clear ();

        foreach (var value in values) {
            var intValue = (int)((object)value);

            methodName = String.Format ("StateMachineEnter_{0}", value.ToString ());
            f = CreateDelegate<Action<Enum, Dictionary<string, object>>> (comp, methodName);

            if (f != null) {
                enterLookup [intValue] = f as Action<Enum, Dictionary<string, object>>;
            } else {
                enterLookup [intValue] = Noop;
            }

            methodName = String.Format ("StateMachineExit_{0}", value.ToString ());
            f = CreateDelegate<Action<Enum, Dictionary<string, object>>> (comp, methodName);

            if (f != null) {
                exitLookup [intValue] = f as Action<Enum, Dictionary<string, object>>;
            } else {
                exitLookup [intValue] = Noop;
            }

            methodName = String.Format ("StateMachineUpdate_{0}", value.ToString ());
            f = CreateDelegate <Func<float, bool>> (comp, methodName);

            if (f != null) {
                updateLookup [intValue] = f as Func<float, bool>;
            } else {
                updateLookup [intValue] = Noop;
            }
        }
    }

    public static object CreateDelegate (Type T, object o, string methodName)
    {
        MethodInfo m = GetMethodRecursive (o, methodName);

        if (m != null) {
            try {
                return Delegate.CreateDelegate (T, o, m);
            } catch (Exception e) {
                Debug.LogError ("CustomFSMManager::CreateDelegate method not compatible " + methodName + " " + e.StackTrace);
            }
        }

        return null;
    }

    public static T CreateDelegate <T> (object o, string methodName) where T : class
    {
        MethodInfo m = GetMethodRecursive (o, methodName);

        if (m != null) {
            try {
                return Delegate.CreateDelegate (typeof(T), o, m) as T;
            } catch (Exception e) {
                Debug.LogError ("CustomFSMManager::CreateDelegate method not compatible " + methodName + " " + e.StackTrace);
            }
        }

        return null;
    }

    public static MethodInfo GetMethodRecursive (object o, string methodName)
    {
        Type type = o.GetType ();
        MethodInfo m = null;

        while (type != null) {
            m = type.GetMethod (methodName, BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic);

            if (m != null) {
                return m;
            } else {
                type = type.BaseType;
            }
        }

        return m;
    }

    public static void Noop (Enum e, Dictionary<string, object> options)
    {
    }

    public static bool Noop (float t)
    {
        return false;
    }

    // Try not to transition to the current state. It's a hack to do that
    public void StateMachineChange (Enum state, Dictionary<string, object> options = null)
    {
        //        Debug.LogFormat ("{0}::StateMachineChange {1}", comp.GetType ().Name, state);
        StateMachineExit (state, options);
        StateMachineEnter (state, options);
    }

    void StateMachineEnter (Enum state, Dictionary<string, object> options = null)
    {
        var oldState = this._state;
        this._state = state;
        this.state = (int)((object)this._state);
        this.currentStateTime = 0;
        enterLookup [this.state] (oldState, options);
    }

    void StateMachineExit (Enum state, Dictionary<string, object> options = null)
    {
        exitLookup [this.state] (state, options);
    }

    public void Update ()
    {
        if (stopUpdate) {
            return;
        }

        if (autoUpdate) {
            _StateMachineUpdate (Time.deltaTime);
        }
    }

    public void SetStopUpdate (bool stopUpdate)
    {
        this.stopUpdate = stopUpdate;
    }

    public void StateMachineUpdate (float deltaTime)
    {
        if (!autoUpdate) {
            _StateMachineUpdate (deltaTime);
        }
    }

    void _StateMachineUpdate (float deltaTime)
    {
        if (!updateLookup.ContainsKey (this.state)) {
            // For some reason, the delegates were destroyed
            CreateAllDelegates ();
        }

        if (updateLookup [this.state] (deltaTime)) {
            return;
        }

        currentStateTime += deltaTime;
    }
}
