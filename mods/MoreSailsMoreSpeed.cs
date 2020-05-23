using FMODUnity;
using Harmony;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

[ModTitle("MoreSailsMoreSpeed")]
[ModDescription("More sails/engines will make you go faster. Also bring Quality of Life improvements to control all your sails/engines at once.")]
[ModAuthor("Akitake")]
[ModIconUrl("https://gitlab.com/Akitake/RaftMods/raw/master/MoreSailsMoreSpeed/MoreSailsMoreSpeed_Icon.jpg")]
[ModWallpaperUrl("https://gitlab.com/Akitake/RaftMods/raw/master/MoreSailsMoreSpeed/MoreSailsMoreSpeed_Banner.jpg")]
[ModVersionCheckUrl("https://raftmodding.com/api/v1/mods/moresailsmorespeed/version.txt")]
[ModVersion("3.1.5")]
[RaftVersion("Update 10.07 (4497220)")]
public class MoreSailsMoreSpeed : Mod
{
    #region Variables
    public static MoreSailsMoreSpeed instance;

    // Harmony
    public HarmonyInstance harmony;
    public readonly string harmonyID = "com.gitlab.akitake.raftmods.moresailsmorespeed";

    // Misc
    private static Semih_Network network = ComponentManager<Semih_Network>.Value;
    public static float RaftFixedUpdatePatchRate = 1.9f;

    // Console stuff
    public static string modColor = "#4DB2FF";
    public static string modPrefix = "[" + Utils.Colorize("MoreSailsMoreSpeed", modColor) + "] ";
    #endregion

    #region Start/Unload
    public void Start()
    {
        if (instance != null) { throw new Exception("MoreSailsMoreSpeed singleton was already set"); }
        instance = this;

        harmony = HarmonyInstance.Create(harmonyID);
        harmony.PatchAll(Assembly.GetExecutingAssembly());

        RConsole.registerCommand(typeof(MoreSailsMoreSpeed), "Lower all sails", "sailsOpen", SailsOpen);
        RConsole.registerCommand(typeof(MoreSailsMoreSpeed), "Raise all sails", "sailsClose", SailsClose);
        RConsole.registerCommand(typeof(MoreSailsMoreSpeed), "Rotate sails by a certain number", "sailsRotate", SailsRotate);
        RConsole.registerCommand(typeof(MoreSailsMoreSpeed), "Toggle all engines at once", "toggleEngines", ToggleAllEngines);
        RConsole.registerCommand(typeof(MoreSailsMoreSpeed), "Toggle all engines's direction at once", "toggleEnginesDir", ToggleAllEnginesDir);
        RConsole.registerCommand(typeof(MoreSailsMoreSpeed), "Tells you your current speed, 1.5 = default drift speed", "raftSpeed", PrintSpeed);

        if (SceneManager.GetActiveScene().isLoaded && SceneManager.GetActiveScene().name == network.gameSceneName)
            WorldEvent_WorldLoaded();

        RConsole.Log(modPrefix + "loaded!");
    }

    public override void WorldEvent_WorldLoaded()
    {
        Raft raft = ComponentManager<Raft>.Value;
        if (raft != null)
        {
            raft.maxSpeed = 20f;
            raft.maxVelocity = 20f;
        }
    }

    public void OnModUnload()
    {
        RConsole.Log(modPrefix + "unloaded!");
        harmony.UnpatchAll(harmonyID);
        Destroy(gameObject);
    }
    #endregion

    public void PrintSpeed()
    {
        Raft raft = FindObjectOfType<Raft>();
        object value = Traverse.Create(raft).Field("currentMovementSpeed").GetValue() as object;
        RConsole.Log(value.ToString());
    }

    #region Sails Functions
    public void SailsRotate()
    {
        string[] args = RConsole.lcargs;
        if (args.Length > 1)
        {
            string x = args[1];
            int value;
            if (int.TryParse(x, out value))
                SailsRotate(value);
        }
    }
    public static void SailsRotate(float axis)
    {
        List<Sail> allSails = Sail.AllSails;
        foreach (Sail current in allSails)
        {
            if (Semih_Network.IsHost)
            {
                AccessTools.Method("Sail:Rotate").Invoke(current, new object[] { axis });
            }
            else
            {
                Message message = new Message_Sail_Rotate(Messages.Sail_Rotate, current, axis);
                network.SendP2P(network.HostID, message, EP2PSend.k_EP2PSendReliable, NetworkChannel.Channel_Game);
            }
        }
    }
    public static void RotateSailsTo(Sail sail)
    {
        if (network == null)
            network = ComponentManager<Semih_Network>.Value;
        List<Sail> allSails = Sail.AllSails;
        foreach (Sail current in allSails)
        {
            if (sail != current)
            {
                float axis = (sail.LocalRotation - current.LocalRotation);
                if (Semih_Network.IsHost)
                {
                    AccessTools.Method("Sail:Rotate").Invoke(current, new object[] { axis });
                }
                else
                {
                    Message message = new Message_Sail_Rotate(Messages.Sail_Rotate, current, axis);
                    network.SendP2P(network.HostID, message, EP2PSend.k_EP2PSendReliable, NetworkChannel.Channel_Game);
                }
            }
        }
    }

    public static void SailsOpen()
    {
        if (network == null)
            network = ComponentManager<Semih_Network>.Value;
        List<Sail> allSails = Sail.AllSails;
        for (int i = 0; i < allSails.Count; i++)
        {
            Sail sail = allSails[i];
            Message_NetworkBehaviour message = new Message_NetworkBehaviour(Messages.Sail_Open, sail);
            if (Semih_Network.IsHost)
            {
                sail.Open();
                network.RPC(message, Target.Other, EP2PSend.k_EP2PSendReliable, NetworkChannel.Channel_Game);
                continue;
            }
            network.SendP2P(network.HostID, message, EP2PSend.k_EP2PSendReliable, NetworkChannel.Channel_Game);
        }
    }

    public static void SailsClose()
    {
        if (network == null)
            network = ComponentManager<Semih_Network>.Value;
        List<Sail> allSails = Sail.AllSails;
        for (int i = 0; i < allSails.Count; i++)
        {
            Sail sail = allSails[i];
            Message_NetworkBehaviour message = new Message_NetworkBehaviour(Messages.Sail_Close, sail);
            if (Semih_Network.IsHost)
            {
                AccessTools.Method("Sail:Close", null, null).Invoke(sail, null);
                network.RPC(message, Target.Other, EP2PSend.k_EP2PSendReliable, NetworkChannel.Channel_Game);
                continue;
            }
            network.SendP2P(network.HostID, message, EP2PSend.k_EP2PSendReliable, NetworkChannel.Channel_Game);
        }
    }
    #endregion

    #region Engines Functions
    public static void ToggleAllEngines()
    {
        if (network == null)
            network = ComponentManager<Semih_Network>.Value;

        foreach (MotorWheel motor in FindObjectsOfType<MotorWheel>())
        {
            Message_NetworkBehaviour_ID message = new Message_NetworkBehaviour_ID(Messages.MotorWheel_PowerButton, network.NetworkIDManager, motor.ObjectIndex);
            if (Semih_Network.IsHost)
            {
                motor.ToggleEngine();
                network.RPC(message, Target.Other, EP2PSend.k_EP2PSendReliable, NetworkChannel.Channel_Game);
                continue;
            }
            network.SendP2P(network.HostID, message, EP2PSend.k_EP2PSendReliable, NetworkChannel.Channel_Game);
        }
    }
    public static void ToggleAllEnginesDir()
    {
        if (network == null)
            network = ComponentManager<Semih_Network>.Value;
        foreach (MotorWheel motor in FindObjectsOfType<MotorWheel>())
        {
            Message_NetworkBehaviour_ID message = new Message_NetworkBehaviour_ID(Messages.MotorWheel_Inverse, network.NetworkIDManager, motor.ObjectIndex);
            if (Semih_Network.IsHost)
            {
                motor.InversePushDirection();
                network.RPC(message, Target.Other, EP2PSend.k_EP2PSendReliable, NetworkChannel.Channel_Game);
                continue;
            }
            network.SendP2P(network.HostID, message, EP2PSend.k_EP2PSendReliable, NetworkChannel.Channel_Game);
        }
    }
    #endregion
}

#region Utils
public class Utils
{
    #region Colorize
    public static string Colorize(string text, string col)
    {
        string s = string.Concat(new string[]
        {
            "<color=",
            col,
            ">",
            text,
            "</color>"
        });
        return s;
    }
    #endregion
}
#endregion

#region Patches

[HarmonyPatch(typeof(MotorWheel)), HarmonyPatch("HandleSounds")]
public class EngineSoundsPatch
{
    private static bool Prefix
        (
            ref StudioEventEmitterSustain ___eventEmitter_engine,
            ref StudioEventEmitterSustain ___eventEmitter_waterWheel,
            ref StudioEventEmitterSustain ___eventEmitter_boiler
        )
    {
        ___eventEmitter_engine.eventEmitter.instance.setVolume(0.1f);
        ___eventEmitter_waterWheel.eventEmitter.instance.setVolume(0.1f);
        ___eventEmitter_boiler.eventEmitter.instance.setVolume(0.1f);
        return true;
    }
}

#region Sail Patches
[HarmonyPatch(typeof(Raft)), HarmonyPatch("FixedUpdate")]
public class RaftFixedUpdatePatch
{
    private static bool Prefix
        (
            Raft __instance,
            ref Rigidbody ___body,
            ref float ___currentMovementSpeed,
            ref float ___speed,
            ref float ___maxDistanceFromAnchorPoint,
            ref StudioEventEmitter ___eventEmitter_idle,
            ref Vector3 ___previousPosition,
            ref Vector3 ___moveDirection,
            ref Vector3 ___anchorPosition
        )
    {
        if (!Semih_Network.IsHost) return false;
        if (GameModeValueManager.GetCurrentGameModeValue().raftSpecificVariables.isRaftAlwaysAnchored) return false;
        if (!__instance.IsAnchored)
        {
            if (___speed != 0f)
            {
                float openedSails = 0f;
                ___moveDirection = Vector3.forward;
                if (RaftVelocityManager.MotorDirection == Vector3.zero)
                {
                    List<Sail> allSails = Sail.AllSails;

                    Vector3 vector = Vector3.zero;

                    for (int i = 0; i < allSails.Count; i++)
                    {
                        Sail sail = allSails[i];
                        if (sail.open)
                        {
                            openedSails++;
                            vector += sail.GetNormalizedDirection();
                        }
                    }
                    if (vector.z < 0f)
                    {
                        if ((double)Mathf.Abs(vector.x) > 0.7)
                        {
                            vector.z = (___moveDirection.z = 0f);
                        }
                        else
                        {
                            vector.z = -0.8f;
                        }
                    }
                    ___moveDirection += vector;
                }
                else
                {
                    ___moveDirection = RaftVelocityManager.MotorDirection;
                }
                ___currentMovementSpeed = ___speed;
                if (RaftVelocityManager.MotorDirection != Vector3.zero)
                {
                    ___currentMovementSpeed = RaftVelocityManager.motorSpeed;
                    if (RaftVelocityManager.MotorWheelWeightStrength == MotorWheel.WeightStrength.Weak)
                    {
                        ___currentMovementSpeed *= 0.5f;
                    }
                    if (___currentMovementSpeed < ___speed)
                    {
                        ___currentMovementSpeed = ___speed;
                    }
                }
                else
                {
                    for (float i = 1; i < (openedSails + 1); i++)
                    {
                        ___currentMovementSpeed += (1.5f * (float)(i / Math.Pow(i, MoreSailsMoreSpeed.RaftFixedUpdatePatchRate)));
                    }
                }
                if (___speed != 0f)
                {
                    if (___currentMovementSpeed > __instance.maxSpeed)
                    {
                        ___currentMovementSpeed = __instance.maxSpeed;
                    }
                    ___moveDirection = Vector3.ClampMagnitude(___moveDirection, 1f);
                    ___body.AddForce(___moveDirection * ___currentMovementSpeed);
                }
            }
            List<SteeringWheel> steeringWheels = RaftVelocityManager.steeringWheels;
            float num = 0f;
            foreach (SteeringWheel steeringWheel in steeringWheels)
            {
                num += steeringWheel.SteeringRotation;
            }
            num = Mathf.Clamp(num, -1f, 1f);
            if (num != 0f)
            {
                Vector3 torque = new Vector3(0f, Mathf.Tan(0.0174532924f * num), 0f) * __instance.maxSteeringTorque;
                ___body.AddTorque(torque, ForceMode.Acceleration);
            }
        }
        else if (__instance.transform.position.DistanceXZ(___anchorPosition) > ___maxDistanceFromAnchorPoint)
        {
            Vector3 vector2 = ___anchorPosition - __instance.transform.position;
            vector2.y = 0f;
            ___body.AddForce(vector2.normalized * 2f);
        }
        if (___body.velocity.sqrMagnitude > __instance.maxVelocity)
        {
            ___body.velocity = Vector3.ClampMagnitude(___body.velocity, __instance.maxVelocity);
        }
        ___eventEmitter_idle.SetParameter("velocity", ___body.velocity.sqrMagnitude / __instance.maxVelocity);
        ___previousPosition = ___body.transform.position;
        return false;
    }
}

[HarmonyPatch(typeof(Sail)), HarmonyPatch("OnIsRayed")]
public class SailMultiEditPatch
{
    private static bool Prefix
        (
            Sail __instance,
            ref bool ___blockPlaced,
            ref Network_Player ___localPlayer,
            ref CanvasHelper ___canvas,
            ref bool ___isRotating,
            ref GameObject ___paintCollider,
            ref Semih_Network ___network
        )
    {
        if (!___blockPlaced) return false;
        if (___canvas == null || ___canvas.displayTextManager == null || ___localPlayer == null) return false;
        if (Helper.LocalPlayerIsWithinDistance(__instance.transform.position, Player.UseDistance) && CanvasHelper.ActiveMenu == MenuType.None)
        {
            bool mod = MyInput.GetButton("Sprint");
            if (!__instance.open)
            {
                if (mod)
                    ___canvas.displayTextManager.ShowText("Open Sails", MyInput.Keybinds["Interact"].MainKey, 1, "Rotate Sails", MyInput.Keybinds["Rotate"].MainKey, 2);
                else
                {
                    ___canvas.displayTextManager.ShowText("Hold for more options", MyInput.Keybinds["Sprint"].MainKey, 1, "Open", MyInput.Keybinds["Interact"].MainKey, 2, 0, true);
                    ___canvas.displayTextManager.ShowText("Rotate", MyInput.Keybinds["Rotate"].MainKey, 3, 0, false);
                }
            }
            else
            {
                if (mod)
                    ___canvas.displayTextManager.ShowText("Close Sails", MyInput.Keybinds["Interact"].MainKey, 1, "Rotate Sails", MyInput.Keybinds["Rotate"].MainKey, 2);
                else
                {
                    ___canvas.displayTextManager.ShowText("Hold for more options", MyInput.Keybinds["Sprint"].MainKey, 1, "Close", MyInput.Keybinds["Interact"].MainKey, 2, 0, true);
                    ___canvas.displayTextManager.ShowText("Rotate", MyInput.Keybinds["Rotate"].MainKey, 3, 0, false);
                }
            }

            if (MyInput.GetButtonDown("Interact"))
            {
                Message_NetworkBehaviour message = new Message_NetworkBehaviour(__instance.open ? Messages.Sail_Close : Messages.Sail_Open, __instance);
                if (Semih_Network.IsHost)
                {
                    if (__instance.open)
                    {
                        if (mod)
                            MoreSailsMoreSpeed.SailsClose();
                        else
                            AccessTools.Method("Sail:Close").Invoke(__instance, null);
                    }
                    else
                    {
                        if (mod)
                            MoreSailsMoreSpeed.SailsOpen();
                        else
                            AccessTools.Method("Sail:Open").Invoke(__instance, null);
                    }
                    ___network.RPC(message, Target.Other, EP2PSend.k_EP2PSendReliable, NetworkChannel.Channel_Game);
                }
                else
                {
                    ___network.SendP2P(___network.HostID, message, EP2PSend.k_EP2PSendReliable, NetworkChannel.Channel_Game);
                }
            }

            if (MyInput.GetButton("Rotate"))
            {
                ___localPlayer.PlayerScript.SetMouseLookScripts(false);
                ___isRotating = true;
            }
            else if (MyInput.GetButtonUp("Rotate"))
            {
                if (mod)
                    MoreSailsMoreSpeed.RotateSailsTo(__instance);
                ___localPlayer.PlayerScript.SetMouseLookScripts(true);
                ___isRotating = false;
            }
            if (MyInput.GetButtonUp("Sprint") && ___isRotating)
            {
                MoreSailsMoreSpeed.RotateSailsTo(__instance);
                ___localPlayer.PlayerScript.SetMouseLookScripts(true);
                ___isRotating = false;
                return false;
            }

            bool button = MyInput.GetButton("Rotate");
            if (button)
            {
                float axis = Input.GetAxis("Mouse X");
                if (Semih_Network.IsHost)
                {
                    AccessTools.Method("Sail:Rotate").Invoke(__instance, new object[] { axis });
                }
                else
                {
                    Message_Sail_Rotate message2 = new Message_Sail_Rotate(Messages.Sail_Rotate, __instance, axis);
                    ___network.SendP2P(___network.HostID, message2, EP2PSend.k_EP2PSendReliable, NetworkChannel.Channel_Game);
                }
            }
            ___paintCollider.SetActiveSafe(!button);
            return false;
        }
        if (___isRotating)
        {
            ___isRotating = false;
            ___localPlayer.PlayerScript.SetMouseLookScripts(true);
        }
        ___canvas.displayTextManager.HideDisplayTexts();
        return false;
    }
}
#endregion

#region Engine Patches

[HarmonyPatch(typeof(MotorWheel)), HarmonyPatch("OnButtonPressed_ToggleEngine")]
public class MotorWheelToggleEnginePatch
{
    private static bool Prefix(ref InteractionMotorWheelPowerButton ___button_EngineOnOff)
    {
        if (MyInput.GetButton("Sprint"))
        {
            MoreSailsMoreSpeed.ToggleAllEngines();
            return false;
        }
        return true;
    }
}

[HarmonyPatch(typeof(MotorWheel)), HarmonyPatch("OnButtonPressed_ChangeDirection")]
public class MotorWheelChangeDirectionPatch
{
    private static bool Prefix(ref BlockButton ___button_ChangeDirection)
    {
        if (MyInput.GetButton("Sprint"))
        {
            MoreSailsMoreSpeed.ToggleAllEnginesDir();
            return false;
        }
        return true;
    }
}
#endregion

#region Steering Wheel Patches
[HarmonyPatch(typeof(SteeringWheel)), HarmonyPatch("OnIsRayed")]
public class SteeringWheelEditPatch
{
    private static bool Prefix
        (
            SteeringWheel __instance,
            ref bool ___hasBeenPlaced,
            ref bool ___isDisplayingText,
            DisplayTextManager ___displayText,
            ref Network_Player ___localPlayer,
            ref Semih_Network ___network
        )
    {
        if (!___hasBeenPlaced)
            return true;
        if (MyInput.GetButton("Sprint"))
        {
            ___isDisplayingText = true;
            ___displayText.ShowText("Toggle Engines", MyInput.Keybinds["Interact"].MainKey, 1, "Toggle Engine Direction", MyInput.Keybinds["Rotate"].MainKey, 2);

            if (MyInput.GetButtonDown("Interact"))
            {
                MoreSailsMoreSpeed.ToggleAllEngines();
            }
            if (MyInput.GetButtonDown("Rotate"))
            {
                MoreSailsMoreSpeed.ToggleAllEnginesDir();
            }
        }
        else
        {
            ___isDisplayingText = true;
            ___displayText.ShowText("Hold for more options", MyInput.Keybinds["Sprint"].MainKey, 1, Helper.GetTerm("Game/RotateSmooth2", false), MyInput.Keybinds["Rotate"].MainKey, 2);
            if (MyInput.GetButtonDown("Rotate"))
            {
                ___localPlayer.PlayerScript.SetMouseLookScripts(false);
            }
            if (MyInput.GetButtonUp("Rotate"))
            {
                ___localPlayer.PlayerScript.SetMouseLookScripts(true);
            }
            if (MyInput.GetButton("Rotate"))
            {
                float axis = Input.GetAxis("Mouse X");
                Message_SteeringWheel_Rotate message = new Message_SteeringWheel_Rotate(Messages.SteeringWheelRotate, ___network.NetworkIDManager, __instance.ObjectIndex, axis);
                if (Semih_Network.IsHost)
                {
                    AccessTools.Method("SteeringWheel:Rotate").Invoke(__instance, new object[] { axis });
                    return false;
                }
                ___network.SendP2P(___network.HostID, message, EP2PSend.k_EP2PSendReliable, NetworkChannel.Channel_Game);
            }
        }
        return false;
    }
}
#endregion

#endregion