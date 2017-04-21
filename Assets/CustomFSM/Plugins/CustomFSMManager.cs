using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using System.Reflection;

public class CustomFSMManager : MonoBehaviour
{
    public string fsmName;
    public object comp;
    private Enum _state;
    public int state;
    public float currentStateTime = 0;
    public bool autoUpdate;

    private Dictionary<int, Action<Enum, Dictionary<string, object>>> enterLookup = new Dictionary<int, Action<Enum, Dictionary<string, object>>> ();
    private Dictionary<int, Action<Enum, Dictionary<string, object>>> exitLookup = new Dictionary<int, Action<Enum, Dictionary<string, object>>> ();
    private Dictionary<int, Func<float, bool>> updateLookup = new Dictionary<int, Func<float, bool>> ();

    public Action<float> StateMachineUpdate = NoopUpdate;
    bool stopUpdate = false;
    Type enumType;

    /**
      Initialize this class with the custom enum.
     * The first enum is the default state
     **/
    public void Initialize (Type T, object comp, bool autoUpdate = true)
    {
        this.comp = comp;
        this.enumType = T;
        this.autoUpdate = autoUpdate;

        var values = Enum.GetValues (T);
        this._state = (Enum)values.GetValue (0);
        this.state = (int)((object)this._state);

        CreateAllDelegates ();

        if (!autoUpdate) {
            StateMachineUpdate = _StateMachineUpdate;
        } else {
            StateMachineUpdate = NoopUpdate;
        }
    }

    void CreateAllDelegates ()
    {
        var values = Enum.GetValues (enumType);
        string methodName;
        object f;

        enterLookup.Clear ();
        exitLookup.Clear ();
        updateLookup.Clear ();

        foreach (var value in values) {
            var intValue = (int)((object)value);

            methodName = String.Format ("StateMachineEnter_{0}", value.ToString ());
            f = CreateDelegate (typeof(Action<Enum, Dictionary<string, object>>), comp, methodName) as Action<Enum, Dictionary<string, object>>;

            if (f != null) {
                enterLookup [intValue] = f as Action<Enum, Dictionary<string, object>>;
            } else {
                enterLookup [intValue] = Noop;
            }

            methodName = String.Format ("StateMachineExit_{0}", value.ToString ());
            f = CreateDelegate (typeof(Action<Enum, Dictionary<string, object>>), comp, methodName) as Action<Enum, Dictionary<string, object>>;

            if (f != null) {
                exitLookup [intValue] = f as Action<Enum, Dictionary<string, object>>;
            } else {
                exitLookup [intValue] = Noop;
            }

            methodName = String.Format ("StateMachineUpdate_{0}", value.ToString ());
            f = CreateDelegate (typeof(Func<float, bool>), comp, methodName) as Func<float, bool>;

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

    public static void NoopUpdate (float t)
    {
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
