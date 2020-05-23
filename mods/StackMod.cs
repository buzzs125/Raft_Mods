using Harmony;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

[ModTitle("Stack Mod")] // The mod name.
[ModDescription("Increase stack sizes to ")] // Short description for the mod.
[ModAuthor("Loikas")] // The author name of the mod.
[ModIconUrl("Icon Url")] // An icon for your mod. Its recommended to be 128x128px and in .jpg format.
[ModWallpaperUrl("Banner Url")] // A banner for your mod. Its recommended to be 330x100px and in .jpg format.
[ModVersionCheckUrl("Version File Url")] // This is for update checking. Needs to be a .txt file with the latest mod version.
[ModVersion("1.2")] // This is the mod version.
[RaftVersion("Update Name")] // This is the recommended raft version.
[ModIsPermanent(false)] // If your mod add new blocks, new items or just content you should set that to true. It loads the mod on start and prevents unloading.
public class StackMod : Mod
{
    int lastSize = 0;
    public void Start()
    {
        RConsole.Log("StackMod has been loaded!");
        RConsole.registerCommand(typeof(StackMod), "", "stacksize", getCommand);

        lastSize = PlayerPrefs.GetInt("lastSize", 0);

        if (lastSize != 0)
        {
            Change(lastSize);
        }
    }

    void getCommand()
    {
        string command = RConsole.lastCommands[RConsole.lastCommands.Count - 1];
        string[] split = command.Split(' ');

        Change(int.Parse(split[1]));
    }

    void Change(int size)
    {
        RConsole.Log("Changing sizes");
        List<Item_Base> items = ItemManager.GetAllItems();

        foreach (Item_Base item in items)
        {
            if (item.settings_Inventory.StackSize != 1)
            {
                Traverse.Create(item.settings_Inventory).Field("stackSize").SetValue(size);
            }
        }

        Traverse.Create(typeof(ItemManager)).Field("allAvailableItems").SetValue(items);
        PlayerPrefs.SetInt("lastSize", size);
    }

    public void Update()
    {
    }

    public void OnModUnload()
    {
        RConsole.Log("StackMod has been unloaded!");
        Destroy(gameObject); 
    }
}
