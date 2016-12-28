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

    private Dictionary<Enum, Action<Enum, Dictionary<string, object>>> enterLookup = new Dictionary<Enum, Action<Enum, Dictionary<string, object>>> ();
    private Dictionary<Enum, Action<Enum, Dictionary<string, object>>> exitLookup = new Dictionary<Enum, Action<Enum, Dictionary<string, object>>> ();
    private Dictionary<Enum, Func<float, bool>> updateLookup = new Dictionary<Enum, Func<float, bool>> ();

    public Action<float> StateMachineUpdate = NoopUpdate;
    bool stopUpdate = false;

    /**
      Initialize this class with the custom enum.
     * The first enum is the default state
     **/
    public void Initialize (Type T, object comp, bool autoUpdate = true)
    {
        this.comp = comp;
        this.autoUpdate = autoUpdate;

        var values = Enum.GetValues (T);
        this._state = (Enum)values.GetValue (0);
        this.state = (int)((object)this._state);

        string methodName;
        object f;

        foreach (var value in values) {
            methodName = String.Format ("StateMachineEnter_{0}", value.ToString ());
            f = CreateDelegate (typeof(Action<Enum, Dictionary<string, object>>), comp, methodName) as Action<Enum, Dictionary<string, object>>;

            if (f != null) {
                enterLookup [(Enum)value] = f as Action<Enum, Dictionary<string, object>>;
            } else {
                enterLookup [(Enum)value] = Noop;
            }

            methodName = String.Format ("StateMachineExit_{0}", value.ToString ());
            f = CreateDelegate (typeof(Action<Enum, Dictionary<string, object>>), comp, methodName) as Action<Enum, Dictionary<string, object>>;

            if (f != null) {
                exitLookup [(Enum)value] = f as Action<Enum, Dictionary<string, object>>;
            } else {
                exitLookup [(Enum)value] = Noop;
            }

            methodName = String.Format ("StateMachineUpdate_{0}", value.ToString ());
            f = CreateDelegate (typeof(Func<float, bool>), comp, methodName) as Func<float, bool>;

            if (f != null) {
                updateLookup [(Enum)value] = f as Func<float, bool>;
            } else {
                updateLookup [(Enum)value] = Noop;
            }
        }

        if (!autoUpdate) {
            StateMachineUpdate = _StateMachineUpdate;
        } else {
            StateMachineUpdate = NoopUpdate;
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
        enterLookup [state] (oldState, options);
    }

    void StateMachineExit (Enum state, Dictionary<string, object> options = null)
    {
        exitLookup [_state] (state, options);
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
        if (updateLookup [_state] (deltaTime)) {
            return;
        }

        currentStateTime += deltaTime;
    }
}
