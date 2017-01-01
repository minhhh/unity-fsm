# Unity Finite State Machine

State machine is a very effective way to manage game entities with complex behaviours based on time and/or various states. This is a simple, performant implementation of FSM.

See [http://minhhh.github.io/posts/finite-state-machine-for-unity](Finite state machine for Unity) for a more detailed discussion of FSM in Unity.

## How to use

`CustomFSMManager` is a `Component` that facilitates your custom FSM. It does not represent states itself, instead, it uses reflection to look at functions of the main Component and determines which function should be called when switching states.

First, you create an `Enum` which represents the states like so:

```
public enum MyState
{
    Start,
    Loop,
    End
}
```

Next, you define a `MonoBehaviour` that will represents the Entity with those states. This `MonoBehaviour` will contain a reference to a `CustomFSMManager`, which in turn will manage the `MonoBehaviour`'s states. 'In the `Start` or `Awake` function, you add the following code to initialize the `CustomFSMManager` according to the states defined in `MyState`:

```
CustomFSMManager customFSMManager;

 void Start ()
{
    customFSMManager = gameObject.AddComponent<CustomFSMManager>();
    customFSMManager.Initialize(typeof(MyState), this);

    customFSMManager.StateMachineChange(MyState.Loop); // this is optional
}
```

Initially, the `CustomFSMManager` will start with state corresponding to the integer `0`, so optionally you can call `customFSMManager.StateMachineChange` right away.

The `CustomFSMManager` will update itself by default, so if you want more control, you can force it to not update itself, and call `CustomFSMManager.StateMachineUpdate` manually. To do this, you can call `Initialize (T, comp, false)`, passing `false` to the third parameter.

Next, you define `Enter`, `Exit`, `Update` functions for each necessary states. These functions will be found using reflection. For example, the state `Start` will correspond to the following functions: `StateMachineExit_Start`, `StateMachineEnter_Loop`, `StateMachineEnter_Update`.

The signature of `Start` and `Exit` functions is: `void StateMachineEnter_XXX (Enum state, Dictionary<string, object> options = null)`. The signature of the `Update` function is: `void StateMachineUpdate_XXX (float deltaTime)`

You can also call `SetStopUpdate` to temporarily stop the update of the state machine.

An sample scene is included in the `Assets` folder.
