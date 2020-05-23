using UnityEngine;
using Harmony;
using System.Collections;
using System.Collections.Generic;
using Steamworks;
using System.Reflection;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System;
using System.Runtime.Serialization;
using System.Linq;

[ModTitle("FertilizerMod")] // The mod name.
[ModDescription("Adds fertlizer to your multiplayer world.")] // Short description for the mod.
[ModAuthor("TechDivers")] // The author name of the mod.
[ModIconUrl("download1512.mediafire.com/vuodyt3t0esg/zf8btgpl7je3ccr/icon.png")] // An icon for your mod. Its recommended to be 128x128px and in .jpg format.
[ModWallpaperUrl("download855.mediafire.com/2vu7qddap3sg/6jnp0untoo2u4z4/banner2.png")] // A banner for your mod. Its recommended to be 330x100px and in .jpg format.
[ModVersionCheckUrl("Version File Url")] // This is for update checking. Needs to be a .txt file with the latest mod version.
[ModVersion("1.1")] // This is the mod version.
[RaftVersion("Update 11")] // This is the recommended raft version.
[ModIsPermanent(true)] // If your mod add new blocks, new items or just content you should set that to true. It loads the mod on start and prevents unloading.
public class Fertilizer : Mod
{
    AssetBundle assets;
    public static List<Plant> plantList = new List<Plant>();
    Network_Player player;
    private CanvasHelper canvas;
    private string currentItem;

    int layerMask = (1 << 20);

    private bool loaded = false;

    public IEnumerator Start()
    {
        layerMask = ~layerMask;

        RConsole.Log("Fertilizer has been loaded!");

        AssetBundleCreateRequest request = AssetBundle.LoadFromFileAsync("mods/ModData/Fertilizer/fertilizer.assets");
        yield return request;
        assets = request.assetBundle;

        Item_Base fertilizeItem = assets.LoadAsset<Item_Base>("fertilizer");
        RAPI.RegisterItem(fertilizeItem);
        RAPI.SetItemObject(fertilizeItem, assets.LoadAsset<GameObject>("fertilizerGameObject"));
    }
    public void Update()
    {
        if (!(Semih_Network.InLobbyScene)) {
            if (loaded)
            {
                currentItem = " ";
                if (!player.Inventory.GetSelectedHotbarSlot().IsEmpty)
                {
                    currentItem = player.Inventory.GetSelectedHotbarItem().settings_Inventory.DisplayName;
                }
                if (currentItem == "Fertilizer")
                {
                    RaycastHit hit;
                    if (Physics.Raycast(player.CameraTransform.position, player.CameraTransform.forward, out hit, Player.UseDistance, layerMask))
                    {
                        if (hit.transform.gameObject.name == "Placeable_Cropplot_Small_Floor(Clone)" || hit.transform.gameObject.name == "Placeable_Cropplot_Medium_Floor(Clone)" || hit.transform.gameObject.name == "Placeable_Cropplot_Large(Clone)" || hit.transform.gameObject.name == "Placeable_Cropplot_Small_Wall(Clone)")
                        {
                            Plant clsPlant = null;
                            float minDist = Mathf.Infinity;
                            foreach (var plants in hit.transform.gameObject.GetComponent<Cropplot>().plantationSlots)
                            {
                                if (hit.transform.gameObject.GetComponent<fertilzing>() != null)
                                {
                                    if (plants.hasWater && plants.busy && !(hit.transform.gameObject.GetComponent<fertilzing>().toFertilize.Contains(plants.plant)))
                                    {
                                        float dist = Vector3.Distance(plants.transform.position, hit.point);
                                        if (dist < minDist)
                                        {
                                            if (plants.plant.FullyGrown())
                                            {
                                                clsPlant = plants.plant;
                                            }
                                            minDist = dist;
                                        }
                                    }
                                }
                                if (plants.hasWater && plants.busy)
                                {
                                    float dist = Vector3.Distance(plants.transform.position, hit.point);
                                    if (dist < minDist)
                                    {
                                        if (!plants.plant.FullyGrown())
                                        {
                                            clsPlant = plants.plant;
                                        }
                                        minDist = dist;
                                    }
                                }
                            }
                            if (clsPlant != null)
                            {
                                if (player.Inventory.GetSelectedHotbarItem().settings_Inventory.DisplayName == "Fertilizer")
                                {
                                    canvas.displayTextManager.ShowText("Use the fertlizer", MyInput.Keybinds["Interact"].MainKey, 0, 0, true);
                                }
                                else
                                {
                                    canvas.displayTextManager.HideDisplayTexts();
                                }
                                if (MyInput.GetButtonDown("Interact"))
                                {
                                    RAPI.GetLocalPlayer().Inventory.GetSelectedHotbarSlot().RemoveItem(1);
                                    if (hit.transform.gameObject.GetComponent<fertilzing>() != null)
                                    {
                                        addPlant(hit);
                                    }
                                    else
                                    {
                                        addFertilizeFirstTime(hit);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            MessageHandler.ReadP2P_Channel_Fertilizer();
        }        
    }
    public override void WorldEvent_WorldLoaded()
    {
        loaded = true;
        player = RAPI.GetLocalPlayer();
        canvas = ComponentManager<CanvasHelper>.Value;
    }

    public void addPlant(RaycastHit hit)
    {
        Plant clsPlant = null;
        float minDist = Mathf.Infinity;
        foreach (var plants in hit.transform.gameObject.GetComponent<Cropplot>().plantationSlots)
        {
            if (plants.hasWater && plants.busy && hit.transform.gameObject.GetComponent<fertilzing>().toFertilize.Contains(plants.plant))
            {
                float dist = Vector3.Distance(plants.transform.position, hit.point);
                if (dist < minDist)
                {
                    clsPlant = plants.plant;
                    minDist = dist;
                }
            }
        }
        if (clsPlant != null)
        {
            hit.transform.gameObject.GetComponent<fertilzing>().toFertilize.Add(clsPlant);
            MessageHandler.SendMessage(new FMessage_CreateFertilizer((Messages)MessageHandler.NetworkMessages.createSync, hit.transform.gameObject.GetComponent<Cropplot>().transform.position, clsPlant.transform.position, 0));
        }
    }
    public void addFertilizeFirstTime(RaycastHit hit)
    {
        hit.transform.gameObject.AddComponent<fertilzing>();
        Plant clsPlant = null;
        float minDist = Mathf.Infinity;
        foreach (var plants in hit.transform.gameObject.GetComponent<Cropplot>().plantationSlots)
        {
            if (plants.hasWater && plants.busy)
            {
                float dist = Vector3.Distance(plants.transform.position, hit.point);
                if (dist < minDist)
                {
                    clsPlant = plants.plant;
                    minDist = dist;
                }
            }
        }
        if (clsPlant != null)
        {
            hit.transform.gameObject.GetComponent<fertilzing>().toFertilize.Add(clsPlant);
            var rc = UnityEngine.Random.Range(120, 240);
            hit.transform.gameObject.GetComponent<fertilzing>().c = rc;
            MessageHandler.SendMessage(new FMessage_CreateFertilizer((Messages)MessageHandler.NetworkMessages.createSync, hit.transform.gameObject.GetComponent<Cropplot>().transform.position, clsPlant.transform.position, rc));
        }
    }

    public void OnModUnload()
    {
        RConsole.Log("Fertilizer has been unloaded!");
        Destroy(gameObject);
    }
}

[Serializable]
public class fertilzing : MonoBehaviour
{
    public List<Plant> toFertilize = new List<Plant>();
    public int c = 0;
    public List<Plant> toRemove = new List<Plant>();
    public void Start()
    {
        StartCoroutine(last(c));
    }
    public void LateUpdate()
    {
        if (toFertilize.Count == 0)
        {
            Destroy(this);
        }
        else
        {
            foreach (var plantToFertilize in toFertilize)
            {
                if (plantToFertilize.FullyGrown())
                {
                    toRemove.Add(plantToFertilize);
                }
                else
                {
                    plantToFertilize.IncrementGrowTimer(Time.deltaTime * 4);
                }
            }
        }
    }
    public void Update()
    {
        if (toRemove.Count != 0)
        {
            foreach (var plantToRemove in toRemove)
            {
                toFertilize.Remove(plantToRemove);
            }
            toRemove.Clear();
        }
    }
    public IEnumerator last(int secs)
    {
        yield return new WaitForSeconds(secs);
        Destroy(this);
    }
}

sealed class PreMergeToMergedDeserializationBinder : SerializationBinder
{
    public override Type BindToType(string assemblyName, string typeName)
    {
        Type typeToDeserialize = null;
        String exeAssembly = Assembly.GetExecutingAssembly().FullName;
        typeToDeserialize = Type.GetType(String.Format("{0}, {1}", typeName, exeAssembly));
        return typeToDeserialize;
    }
}

public class MessageHandler : MonoBehaviour
{
    public enum NetworkMessages
    {
        createSync = 16000,
        addToSync = 16001,
    }

    public static void SendMessage(Message message)
    {
        if (Semih_Network.IsHost)
        {
            RAPI.GetLocalPlayer().Network.RPC(message, Target.Other, EP2PSend.k_EP2PSendReliable, (NetworkChannel)71);
        }
        else
        {
            RAPI.GetLocalPlayer().SendP2P(message, EP2PSend.k_EP2PSendReliable, (NetworkChannel)71);
        }
    }
    public static void ReadP2P_Channel_Fertilizer()
    {
        if (Semih_Network.InLobbyScene) { return; }
        uint num;
        while (SteamNetworking.IsP2PPacketAvailable(out num, 71))
        {
            byte[] array = new byte[num];
            uint num2;
            CSteamID csteamID;
            if (SteamNetworking.ReadP2PPacket(array, num, out num2, out csteamID, 71))
            {
                MemoryStream serializationStream = new MemoryStream(array);
                BinaryFormatter bf = new BinaryFormatter();
                bf.Binder = new PreMergeToMergedDeserializationBinder();
                Packet packet = bf.Deserialize(serializationStream) as Packet;
                Packet_Multiple packet_Multiple;
                if (packet.PacketType == PacketType.Single)
                {
                    Packet_Single packet_Single = packet as Packet_Single;
                    packet_Multiple = new Packet_Multiple(packet_Single.SendType);
                    packet_Multiple.messages = new Message[]
                    {
                        packet_Single.message
                    };
                }
                else
                {
                    packet_Multiple = (packet as Packet_Multiple);
                }
                List<Message> list = packet_Multiple.messages.ToList();
                for (int i = 0; i < list.Count; i++)
                {
                    Message message = list[i];
                    if (message != null)
                    {
                        Messages type = message.Type;
                        switch (type)
                        {
                            case (Messages)NetworkMessages.createSync:
                                {
                                    FMessage_CreateFertilizer msg = message as FMessage_CreateFertilizer;
                                    foreach (Cropplot cropp in FindObjectsOfType<Cropplot>())
                                    {
                                        if (new Vector3((float)(Math.Truncate(1000 * cropp.transform.position.x) / 1000), (float)(Math.Truncate(1000 * cropp.transform.position.y) / 1000), (float)(Math.Truncate(1000 * cropp.transform.position.z) / 1000)) == new Vector3((float)(Math.Truncate(1000 * msg.position.x) / 1000), (float)(Math.Truncate(1000 * msg.position.y) / 1000), (float)(Math.Truncate(1000 * msg.position.z) / 1000)))
                                        {
                                            if (cropp.gameObject.GetComponent<fertilzing>() == null)
                                            {
                                                cropp.gameObject.AddComponent<fertilzing>();
                                                cropp.gameObject.GetComponent<fertilzing>().c = msg.c;
                                            }
                                            foreach (Plant plant in FindObjectsOfType<Plant>())
                                            {
                                                if (new Vector3((float)(Math.Truncate(1000 * plant.transform.position.x) / 1000), (float)(Math.Truncate(1000 * plant.transform.position.y) / 1000), (float)(Math.Truncate(1000 * plant.transform.position.z) / 1000)) == new Vector3((float)(Math.Truncate(1000 * msg.hitPoint.x) / 1000), (float)(Math.Truncate(1000 * msg.hitPoint.y) / 1000), (float)(Math.Truncate(1000 * msg.hitPoint.z) / 1000)))
                                                {
                                                    cropp.gameObject.GetComponent<fertilzing>().toFertilize.Add(plant);
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                    break;
                                }
                        }
                    }

                }
            }
        }
    }
}


[Serializable]
public class FMessage_CreateFertilizer : Message
{
    public Vector3 position { get { return new Vector3(posX, posY, posZ); } private set { posX = value.x; posY = value.y; posZ = value.z; } }
    private float posX, posY, posZ;

    public Vector3 hitPoint { get { return new Vector3(pos2X, pos2Y, pos2Z); } private set { pos2X = value.x; pos2Y = value.y; pos2Z = value.z; } }
    private float pos2X, pos2Y, pos2Z;

    public int c;

    public FMessage_CreateFertilizer(Messages type, Vector3 _position, Vector3 _hitPoint, int _c) : base(type)
    {
        position = _position;
        hitPoint = _hitPoint;
        c = _c;
    }
}