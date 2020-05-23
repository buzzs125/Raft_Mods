using Harmony;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ModTitle("More Storages")] // The mod name.
[ModDescription("Adds new chests and new storages to the game.")] // Short description for the mod.
[ModAuthor("TeKGameR")] // The author name of the mod.
[ModIconUrl("https://www.raftmodding.com/TeKGameRMods/morestorages_icon.jpg")] // An icon for your mod. Its recommended to be 128x128px and in .jpg format.
[ModWallpaperUrl("https://www.raftmodding.com/TeKGameRMods/morestorages_banner.png")] // A banner for your mod. Its recommended to be 330x100px and in .jpg format.
[ModVersionCheckUrl("https://raftmodding.com/api/v1/mods/morestorages/version.txt")] // This is for update checking. Needs to be a .txt file with the latest mod version.
[ModVersion("1.1")] // This is the mod version.
[RaftVersion("Update 9.05")] // This is the recommended raft version.
[ModIsPermanent(true)] // If your mod add new blocks, new items or just content you should set that to true. It loads the mod on start and prevents unloading.
public class MoreStorages : Mod
{
    public static AssetBundle assetbundle;

    IEnumerator Start()
    {
        RNotification notification = FindObjectOfType<RNotify>().AddNotification(RNotify.NotificationType.spinning, "Loading MoreStorages...");

        var bundleLoadRequest = AssetBundle.LoadFromFileAsync("mods\\ModData\\MoreStorages\\morestorages.assets");
        yield return bundleLoadRequest;

        assetbundle = bundleLoadRequest.assetBundle;
        if (assetbundle == null)
        {
            RConsole.LogError("Failed to load AssetBundle for MoreStorages!");
            notification.Close();
            yield return null;
        }

        List<Item_Base> list = Traverse.Create(typeof(ItemManager)).Field("allAvailableItems").GetValue<List<Item_Base>>();

        Item_Base Placeable_MoreStorages_MinecraftChest = (Item_Base)assetbundle.LoadAsset<ScriptableObject>("Placeable_MoreStorages_MinecraftChest");
        Placeable_MoreStorages_MinecraftChest.Initialize(9845, "Placeable_MoreStorages_MinecraftChest", 1);
        list.Add(Placeable_MoreStorages_MinecraftChest);
        RegisterChest(Placeable_MoreStorages_MinecraftChest);

        Item_Base Placeable_MoreStorages_Barrel = (Item_Base)assetbundle.LoadAsset<ScriptableObject>("Placeable_MoreStorages_Barrel");
        Placeable_MoreStorages_Barrel.Initialize(9846, "Placeable_MoreStorages_Barrel", 1);
        list.Add(Placeable_MoreStorages_Barrel);
        RegisterChest(Placeable_MoreStorages_Barrel);

        Item_Base Placeable_MoreStorages_Crate = (Item_Base)assetbundle.LoadAsset<ScriptableObject>("Placeable_MoreStorages_Crate");
        Placeable_MoreStorages_Crate.Initialize(9847, "Placeable_MoreStorages_Crate", 1);
        list.Add(Placeable_MoreStorages_Crate);
        RegisterChest(Placeable_MoreStorages_Crate);

        Item_Base Placeable_MoreStorages_Luggage = (Item_Base)assetbundle.LoadAsset<ScriptableObject>("Placeable_MoreStorages_Luggage");
        Placeable_MoreStorages_Luggage.Initialize(9848, "Placeable_MoreStorages_Luggage", 1);
        list.Add(Placeable_MoreStorages_Luggage);
        RegisterChest(Placeable_MoreStorages_Luggage);

        Item_Base Placeable_MoreStorages_MedievalChest = (Item_Base)assetbundle.LoadAsset<ScriptableObject>("Placeable_MoreStorages_MedievalChest");
        Placeable_MoreStorages_MedievalChest.Initialize(9849, "Placeable_MoreStorages_MedievalChest", 1);
        list.Add(Placeable_MoreStorages_MedievalChest);
        RegisterChest(Placeable_MoreStorages_MedievalChest);

        Item_Base Placeable_MoreStorages_Package = (Item_Base)assetbundle.LoadAsset<ScriptableObject>("Placeable_MoreStorages_Package");
        Placeable_MoreStorages_Package.Initialize(9850, "Placeable_MoreStorages_Package", 1);
        list.Add(Placeable_MoreStorages_Package);
        RegisterChest(Placeable_MoreStorages_Package);


        Traverse.Create(typeof(ItemManager)).Field("allAvailableItems").SetValue(list);

        notification.Close();
        RConsole.Log("MoreStorages has been successfully loaded!");
    }

    void RegisterChest(Item_Base i)
    {
        RAPI.AddItemToBlockQuadType(i, RBlockQuadType.quad_floor);
        RAPI.AddItemToBlockQuadType(i, RBlockQuadType.quad_foundation);
        RAPI.AddItemToBlockQuadType(i, RBlockQuadType.quad_table);
    }

    public void OnModUnload()
    {

        RConsole.Log("MoreStorages can't be unloaded!");
    }
}
