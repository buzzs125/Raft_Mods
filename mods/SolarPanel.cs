using Harmony;
using I2.Loc;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AzureSky;

[ModTitle("SolarPanel")] // The mod name.
[ModDescription("Adds solar panels raft.")] // Short description for the mod.
[ModAuthor("TeKGameR")] // The author name of the mod.
[ModIconUrl("https://www.raftmodding.com/TeKGameRMods/solarpanelicon.png")] // An icon for your mod. Its recommended to be 128x128px and in .jpg format.
[ModWallpaperUrl("https://www.raftmodding.com/TeKGameRMods/solarpanelmod.jpg")] // A banner for your mod. Its recommended to be 330x100px and in .jpg format.
[ModVersionCheckUrl("https://raftmodding.com/api/v1/mods/solarpanel/version.txt")] // This is for update checking. Needs to be a .txt file with the latest mod version.
[ModVersion("1.2")] // This is the mod version.
[RaftVersion("Update 9.05")] // This is the recommended raft version.
[ModIsPermanent(true)] // If your mod add new blocks, new items or just content you should set that to true. It loads the mod on start and prevents unloading.
public class SolarPanel : Mod
{

    public static AssetBundle assetbundle;

    IEnumerator Start()
    {
        RNotification notification = FindObjectOfType<RNotify>().AddNotification(RNotify.NotificationType.spinning, "Loading SolarPanelMod...");

        var bundleLoadRequest = AssetBundle.LoadFromFileAsync("mods\\ModData\\SolarPanel\\solarpanel.assets");
        yield return bundleLoadRequest;

        assetbundle = bundleLoadRequest.assetBundle;
        if (assetbundle == null)
        {
            RConsole.LogError("Failed to load AssetBundle for solarpanel!");
            notification.Close();
            yield return null;
        }

        List<Item_Base> list = Traverse.Create(typeof(ItemManager)).Field("allAvailableItems").GetValue<List<Item_Base>>();

        Item_Base placeable_solarpanel = (Item_Base)assetbundle.LoadAsset<ScriptableObject>("placeable_solarpanel");
        placeable_solarpanel.settings_buildable.GetBlockPrefab(0).gameObject.AddComponent<SolarPanelObject>();
        SolarPanelObject obj = placeable_solarpanel.settings_buildable.GetBlockPrefab(0).gameObject.AddComponent<SolarPanelObject>();
        Traverse.Create(placeable_solarpanel.settings_buildable.GetBlockPrefab(0).GetComponentInChildren<Battery>()).Field("networkBehaviourID").SetValue(obj);
        placeable_solarpanel.Initialize(999, "placeable_solarpanel", 1);
        
        list.Add(placeable_solarpanel);

        Traverse.Create(typeof(ItemManager)).Field("allAvailableItems").SetValue(list);

        RAPI.AddItemToBlockQuadType(placeable_solarpanel, RBlockQuadType.quad_floor);
        RAPI.AddItemToBlockQuadType(placeable_solarpanel, RBlockQuadType.quad_foundation);
        RAPI.AddItemToBlockQuadType(placeable_solarpanel, RBlockQuadType.quad_table);

        notification.Close();
    }

    public void OnModUnload()
    {

        RConsole.Log("SolarPanelMod can't be unloaded!");
    }
}

public class SolarPanelObject : MonoBehaviour_ID_Network, IRaycastable
{
    private CanvasHelper canvas;
    private Network_Player localPlayer;
    private bool showingText;
    public Battery battery;

    public DateTime lastRecharge = DateTime.Now;
    public int secsBetweenRecharges = 5;
    public AzureSkyController skyController;

    public int batteryMaxUses;

    void Start()
    {
        lastRecharge = DateTime.Now;
        battery = GetComponentInChildren<Battery>();
        batteryMaxUses = ItemManager.GetItemByName("Battery").MaxUses;
    }

    void Update()
    {
        if (skyController == null)
        {
            skyController = FindObjectOfType<AzureSkyController>();
        }

        if (!battery.BatterySlotIsEmpty && Semih_Network.IsHost)
        {
            if (battery.GetBatteryInstance().Uses < ItemManager.GetItemByName("Battery").MaxUses && lastRecharge.AddSeconds(secsBetweenRecharges) <= DateTime.Now)
            {
                lastRecharge = DateTime.Now;
                AddBatteryUsesNetworked(1);
                return;
            }
        }
    }

    public bool AddBatteryUsesNetworked(int amount)
    {
        if (Semih_Network.IsHost)
        {
            if (skyController.timeOfDay.hour >= 5 && skyController.timeOfDay.hour <= 19)
            {
                battery.GetBatteryInstance().Uses++;
                RAPI.GetLocalPlayer().Network.RPC(new Message_Battery(Messages.Battery_Update, RAPI.GetLocalPlayer().Network.NetworkIDManager, RAPI.GetLocalPlayer().steamID, this.ObjectIndex, battery.GetBatteryInstance().Uses), Target.Other, EP2PSend.k_EP2PSendReliable, NetworkChannel.Channel_Game);
                return true;
            }
        }
        return false;

    }

    public void OnIsRayed()
    {
        if (this.canvas == null)
        {
            this.canvas = ComponentManager<CanvasHelper>.Value;
        }
        if (this.localPlayer == null)
        {
            this.localPlayer = ComponentManager<Network_Player>.Value;
        }
        if (CanvasHelper.ActiveMenu != MenuType.None || !Helper.LocalPlayerIsWithinDistance(base.transform.position, Player.UseDistance))
        {
            if (this.showingText)
            {
                this.canvas.displayTextManager.HideDisplayTexts();
                this.showingText = false;
            }
            return;
        }
        if (skyController.timeOfDay.hour < 5 || skyController.timeOfDay.hour > 19)
        {
            this.canvas.displayTextManager.ShowText("Its the spooky night! The solar panel won't work!", 0, true, 0);
            this.showingText = true;
            return;
        }
        if (!battery.BatterySlotIsEmpty)
        {
            if (battery.GetBatteryInstance().Uses < batteryMaxUses)
            {
                float progress = (float)battery.GetBatteryInstance().Uses / (float)batteryMaxUses;
                this.canvas.displayTextManager.ShowText("The battery is charging... (" + Mathf.Round(progress * 100) + "%)", 0, true, 0);
                this.showingText = true;
            }
            else
            {
                this.canvas.displayTextManager.ShowText("The battery is fully charged!", 0, true, 0);
                this.showingText = true;
            }
        }
        else
        {
            this.canvas.displayTextManager.ShowText("No battery, place one to charge it!", 0, true, 0);
            this.showingText = true;
            return;
        }
    }

    public void OnRayEnter()
    {
    }

    void IRaycastable.OnRayExit()
    {
        if (this.showingText)
        {
            this.canvas.displayTextManager.HideDisplayTexts();
            this.showingText = false;
        }
    }



    protected override void OnDestroy()
    {
        base.OnDestroy();
        NetworkIDManager.RemoveNetworkID(this);
    }

    public void OnBlockPlaced()
    {
        NetworkIDManager.AddNetworkID(this);
    }


}