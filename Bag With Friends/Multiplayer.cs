using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MelonLoader;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using WebSocketSharp;
using Steamworks;
using Galaxy.Api;
using System.Collections.ObjectModel;
using System.Management;
using System.Threading.Tasks;
using System.Threading;
using System.Text.Json;
using System.Web.Security;
using System.Security.Cryptography;
using System.IO;
using System.Collections;
using System.Net;
using System.Diagnostics;
using HarmonyLib;
using System.Buffers.Text;

namespace Bag_With_Friends
{
    public class Multiplayer : MelonMod
    {
        static MelonLogger.Instance logger;

        bool debugMode = false;
        string server = "bwf.givo.xyz";
        bool moreLogs = false;
        bool compMode = false;
        bool casterMode = false;

        System.Random rand = new System.Random();

        static WebSocket ws;
        static ulong playerId;
        string cacheName;
        string playerName;
        static Player mePlayerPlayer;
        bool amHost = false;
        bool inRoom = false;
        bool connected = false;
        bool wasAlive = false;
        bool wasConnected = false;
        bool makingShadowPrefab = false;
        bool freshBoot = true;
        string myLastScene = "TitleScreen";
        CursorLockMode mouseOnOpen = CursorLockMode.None;

        public static Font arial;
        bool frozen = false;

        GameObject multiplayerMenuObject;
        Canvas multiplayerMenuCanvas;
        static GameObject multiplayerMenu;
        GameObject roomMenu;
        GraphicRaycaster multiplayerRaycaster;
        GameObject roomContainer;
        GameObject playerMenuContainer;
        GameObject playerSettingsMenu;

        static GameObject updateContainer;
        Image roomBack;
        Image playerBack;

        Text roomMenuRoomName;
        Text roomMenuRoomPass;
        InputField roomMenuName;
        InputField roomMenuPass;
        GameObject updateRoomButton;
        UnityEngine.UI.Button roomMenuUpdate;

        Image noCrampons;
        Image crampons6;
        Image crampons10;
        Image noCoffee;
        Image yesCoffe;
        Image noPipe;
        Image yesPipe;
        Image noAxes;
        Image yesAxes;

        AssetBundle UIBundle;
        System.Reflection.Assembly thisAssembly;

        List<InputField> inputFields = new List<InputField>(0);
        Dictionary<InputField, bool> inputFieldChecks = new Dictionary<InputField, bool>(0);

        long reconnectDelay = 0;

        ulong hostId = 0;
        ulong roomId = 0;
        string roomName = "";
        string roomPass = "";
        List<Player> playersInRoom = new List<Player>(0);
        Dictionary<ulong, Player> playerLookup = new Dictionary<ulong, Player>(0);
        public GameObject playerContainer;
        public Dictionary<string, Transform> sceneSplitters = new Dictionary<string, Transform>(0);
        Dictionary<ulong, GameObject> playerListingLookup = new Dictionary<ulong, GameObject>(0);

        Color playerColor = new Color(1, 1, 1, 1);
        Color playerColorLast = new Color(1, 1, 1, 1);
        Image playerColorPreview;

        Shader transparentDiffuse;
        Color myColor = Color.white;
        GameObject mePlayer;
        BodyTurning meBodyTurning;
        Climbing meClimbing;
        Footplacement meFoots;
        PlayerShadow shadow;
        Barometer barometer;
        static GameObject pipeObject;
        public GameObject shadowPrefab;
        //public List<GameObject> shadowPrefabs = new List<GameObject>(0);
        public List<Player> shadowPrefabRequests = new List<Player>(0);
        public List<Player> bannedPlayers = new List<Player>(0);

        bool cramponsB = false;
        bool cramponUpgradeB = false;
        bool coffeeB = false;
        bool pipeB = false;
        bool axesB = false;

        long lastPing = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        long myPing = 0;
        long lastRefresh = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        long recoverTimeout = 0;

        float debugSpinTimer = 0;
        Vector3 debugSpinPos = Vector3.zero;
        bool debugSpin = false;

        long cheatEngineCheck = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        public void Connect()
        {
            //ws = new WebSocket("ws://bwf.givo.xyz:3000");
            ws = new WebSocket($"ws://{server}:3000");
            ws.WaitTime = new TimeSpan(0);

            ws.OnMessage += (sender, e) =>
            {
                lastPing = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                JsonDocument doc = JsonDocument.Parse(e.Data);
                JsonElement res = doc.RootElement;

                if (res.GetProperty("data").GetString() != "updatePlayerPosition" && res.GetProperty("data").GetString() != "pong" && res.GetProperty("data").GetString() != "updatePlayerPing" && (debugMode || moreLogs))
                {
                    //LoggerInstance.Msg("got message " + res.GetProperty("data").GetString());
                    LoggerInstance.Msg(res);
                }

                switch (res.GetProperty("data").GetString())
                {
                    case "identify":
                        giveInfo();

                        if (wasConnected && inRoom)
                        {
                            recoverTimeout = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + 60000;
                            LoggerInstance.Msg("Helping server recover");

                            MakeInfoText("Helping server recover", Color.white);
                            MakeAndSendRecovery();

                            amHost = false;
                            foreach (Player player in playersInRoom)
                            {
                                if (player != mePlayerPlayer)
                                {
                                    player.Yeet(false);
                                }
                            }
                            foreach (GameObject ob in playerListingLookup.Values)
                            {
                                GameObject.Destroy(ob);
                            }
                            playersInRoom.Clear();
                            playerLookup.Clear();
                            shadowPrefabRequests.Clear();
                            playerListingLookup.Clear();

                            MakePlayerInList(mePlayerPlayer);
                        }
                        break;

                    case "yeet":
                        amHost = false;
                        foreach (Player player in playersInRoom)
                        {
                            player.Yeet(false);
                        }
                        foreach (GameObject ob in playerListingLookup.Values)
                        {
                            GameObject.Destroy(ob);
                        }
                        playersInRoom.Clear();
                        playerLookup.Clear();
                        shadowPrefabRequests.Clear();
                        playerListingLookup.Clear();

                        frozen = false;
                        PeakSummited[] summiteds = Resources.FindObjectsOfTypeAll<PeakSummited>();
                        if (summiteds.Length != 0)
                        {
                            summiteds[0].DisablePlayerMovement(frozen);
                        } else
                        {
                            GameObject temp = new GameObject();
                            temp.AddComponent<PeakSummited>().DisablePlayerMovement(frozen);
                            GameObject.Destroy(temp);
                        }
                        break;

                    case "pong":
                        lastPing = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                        ws.Send($"{{\"data\":\"ping\", \"id\":\"{playerId}\", \"ping\":{lastPing}}}");
                        freshBoot = false;
                        break;

                    case "summit":
                        Player playerSummit = playerLookup.ContainsKey(res.GetProperty("id").GetUInt64()) ? playerLookup[res.GetProperty("id").GetUInt64()] : mePlayerPlayer;
                        MakeInfoText(playerSummit.name + " summited " + res.GetProperty("scene").GetString() + "!", new Color(252f / 255f, 230f / 255f, 121f / 255f), 36);
                        break;

                    case "changeColor":
                        Player playerColor = playerLookup[res.GetProperty("id").GetUInt64()];
                        JsonElement.ArrayEnumerator color2 = res.GetProperty("color").EnumerateArray();
                        playerColor.ChangeColor(new Color(float.Parse(color2.ElementAt(0).GetString()), float.Parse(color2.ElementAt(1).GetString()), float.Parse(color2.ElementAt(2).GetString()), float.Parse(color2.ElementAt(3).GetString())));
                        break;

                    case "host":
                        amHost = true;
                        break;

                    case "error":
                    case "info":
                        MakeInfoText(res.GetProperty("info").GetString(), res.GetProperty("data").GetString() == "error" ? new Color(1, 0, 0, 1) : new Color(1, 1, 1, 1));
                        break;

                    case "roomList":
                        LoggerInstance.Msg("making room list");

                        InputField[] fields = GetAllInputFieldsInChildren(roomContainer);

                        foreach (InputField field in fields)
                        {
                            inputFields.Remove(field);
                            inputFieldChecks.Remove(field);
                        }

                        for (int i = 0; i < roomContainer.transform.childCount; i++)
                        {
                            GameObject.Destroy(roomContainer.transform.GetChild(i).gameObject);
                        }

                        JsonElement.ArrayEnumerator rooms = res.GetProperty("rooms").EnumerateArray();
                        LoggerInstance.Msg(rooms.Count());
                        for (int i = 0; i < rooms.Count(); i++)
                        {
                            JsonElement room = rooms.ElementAt(i);
                            MakeRoomListing(room.GetProperty("name").GetString(), room.GetProperty("id").GetUInt64(), room.GetProperty("pass").GetBoolean(), room.GetProperty("players").GetInt16(), room.GetProperty("host").GetString());
                        }
                        LoggerInstance.Msg(roomContainer.transform.childCount);
                        break;

                    case "inRoom":
                        inRoom = res.GetProperty("inRoom").GetBoolean();

                        if (inRoom && multiplayerMenu.activeSelf)
                        {
                            multiplayerMenu.SetActive(false);
                            roomMenu.SetActive(true);
                            MakePlayerInList(mePlayerPlayer);
                        }

                        if (!inRoom && roomMenu.activeSelf)
                        {
                            roomMenu.SetActive(false);
                            multiplayerMenu.SetActive(true);
                        }

                        ws.Send($"{{\"data\":\"switchScene\", \"id\":\"{playerId}\", \"scene\":\"{SceneManager.GetActiveScene().name}\"}}");
                        break;

                    case "roomUpdate":
                        roomName = res.GetProperty("name").GetString();
                        roomPass = res.GetProperty("password").GetString();
                        roomId = res.GetProperty("id").GetUInt64();
                        break;

                    case "hostUpdate":
                        hostId = res.GetProperty("newHost").GetUInt64();
                        amHost = res.GetProperty("newHost").GetUInt64() == playerId;
                        mePlayerPlayer.host = amHost;
                        playerListingLookup[playerId].transform.GetChild(0).GetComponent<Text>().text = (mePlayerPlayer.host ? "[HOST] " : "") + mePlayerPlayer.name;
                        LoggerInstance.Msg(playerId);
                        LoggerInstance.Msg(res.GetProperty("newHost").GetUInt64());
                        roomMenuName.interactable = amHost;
                        //roomMenuPass.interactable = amHost;
                        //roomMenuUpdate.interactable = amHost;
                        roomMenuPass.gameObject.SetActive(amHost);
                        roomMenuUpdate.gameObject.SetActive(amHost);

                        foreach (Player player in playersInRoom)
                        {
                            player.host = res.GetProperty("newHost").GetUInt64() == player.id;
                            playerListingLookup[player.id].transform.GetChild(0).GetComponent<Text>().text = (player.host ? "[HOST] " : "") + player.name;
                        }

                        foreach (GameObject ob in playerListingLookup.Values)
                        {
                            ob.transform.GetChild(3).gameObject.SetActive(amHost);
                            ob.transform.GetChild(4).gameObject.SetActive(amHost);
                        }

                        if (updateRoomButton != null)
                        {
                            updateRoomButton.SetActive(amHost);
                        }
                        break;

                    case "addPlayer":
                        JsonElement recievedPlayer = res.GetProperty("player").EnumerateArray().ElementAt(0);

                        if (recievedPlayer.GetProperty("id").GetUInt64() == playerId) return;
                        if (playerLookup.ContainsKey(recievedPlayer.GetProperty("id").GetUInt64())) return;

                        Player playerToAdd = new Player(recievedPlayer.GetProperty("name").GetString(), recievedPlayer.GetProperty("id").GetUInt64(), recievedPlayer.GetProperty("scene").GetString(), recievedPlayer.GetProperty("host").GetBoolean(), this);

                        JsonElement.ArrayEnumerator color = res.GetProperty("color").EnumerateArray();
                        playerToAdd.bodyColor = new Color(float.Parse(color.ElementAt(0).GetString()), float.Parse(color.ElementAt(1).GetString()), float.Parse(color.ElementAt(2).GetString()), float.Parse(color.ElementAt(3).GetString()));
                        LoggerInstance.Msg(playerToAdd.bodyColor);

                        playersInRoom.Add(playerToAdd);
                        playerLookup.Add(playerToAdd.id, playerToAdd);
                        playerToAdd.UpdateVisual(recievedPlayer.GetProperty("scene").GetString());
                        MakePlayerInList(playerToAdd);

                        /*GameObject gam = new GameObject("TestOB");
                        GameObject.DontDestroyOnLoad(gam);
                        ThingTester tes = gam.AddComponent<ThingTester>();
                        tes.player = playerToAdd;*/
                        break;

                    case "removePlayer":
                        Player playerToRemove = playerLookup[res.GetProperty("id").GetUInt64()];
                        playersInRoom.Remove(playerToRemove);
                        playerLookup.Remove(playerToRemove.id);
                        GameObject.Destroy(playerListingLookup[playerToRemove.id]);
                        playerListingLookup.Remove(playerToRemove.id);
                        playerToRemove.Yeet(false);
                        playerToRemove = null;
                        break;

                    case "updatePlayerScene":
                        Player playerToUpdate = playerLookup[res.GetProperty("id").GetUInt64()];
                        playerToUpdate.ChangeScene(res.GetProperty("scene").GetString());
                        break;

                    case "freeze":
                        frozen = (res.GetProperty("freeze").GetUInt64() == 1);
                        if (compMode)
                        {
                            PeakSummited[] summiteds2 = Resources.FindObjectsOfTypeAll<PeakSummited>();

                            if (summiteds2.Length != 0)
                            {
                                summiteds2[0].DisablePlayerMovement(frozen);
                            } else
                            {
                                GameObject temp2 = new GameObject();
                                temp2.AddComponent<PeakSummited>().DisablePlayerMovement(frozen);
                                GameObject.Destroy(temp2);
                            }
                        }
                        break;

                    case "playerNewName":
                        Player playerToUpdate4 = playerLookup.ContainsKey(res.GetProperty("id").GetUInt64()) ? playerLookup[res.GetProperty("id").GetUInt64()] : mePlayerPlayer;
                        playerToUpdate4.name = res.GetProperty("newName").GetString();
                        break;

                    case "updatePlayerPing":
                        Player playerToUpdate3 = playerLookup.ContainsKey(res.GetProperty("id").GetUInt64()) ? playerLookup[res.GetProperty("id").GetUInt64()] : mePlayerPlayer;
                        playerToUpdate3.ping = res.GetProperty("ping").GetInt64();
                        break;

                    case "updatePlayerName":
                        Player playerNameUpdate = playerLookup.ContainsKey(res.GetProperty("id").GetUInt64()) ? playerLookup[res.GetProperty("id").GetUInt64()] : mePlayerPlayer;
                        playerNameUpdate.name = res.GetProperty("name").GetString();
                        break;

                    case "updatePlayerPosition":
                        Player playerToUpdate2 = playerLookup[res.GetProperty("id").GetUInt64()];

                        JsonElement.ArrayEnumerator position = res.GetProperty("position").EnumerateArray();
                        Vector3 bodyPos = new Vector3(float.Parse(position.ElementAt(0).GetString()), float.Parse(position.ElementAt(1).GetString()), float.Parse(position.ElementAt(2).GetString()));
                        JsonElement.ArrayEnumerator handL = res.GetProperty("handL").EnumerateArray();
                        Vector3 handLPos = new Vector3(float.Parse(handL.ElementAt(0).GetString()), float.Parse(handL.ElementAt(1).GetString()), float.Parse(handL.ElementAt(2).GetString()));
                        JsonElement.ArrayEnumerator handR = res.GetProperty("handR").EnumerateArray();
                        Vector3 handRPos = new Vector3(float.Parse(handR.ElementAt(0).GetString()), float.Parse(handR.ElementAt(1).GetString()), float.Parse(handR.ElementAt(2).GetString()));
                        JsonElement.ArrayEnumerator footL = res.GetProperty("footL").EnumerateArray();
                        Vector3 footLPos = new Vector3(float.Parse(footL.ElementAt(0).GetString()), float.Parse(footL.ElementAt(1).GetString()), float.Parse(footL.ElementAt(2).GetString()));
                        JsonElement.ArrayEnumerator footR = res.GetProperty("footR").EnumerateArray();
                        Vector3 footRPos = new Vector3(float.Parse(footR.ElementAt(0).GetString()), float.Parse(footR.ElementAt(1).GetString()), float.Parse(footR.ElementAt(2).GetString()));
                        JsonElement.ArrayEnumerator footLBend = res.GetProperty("footLBend").EnumerateArray();
                        Vector3 footLBendPos = new Vector3(float.Parse(footLBend.ElementAt(0).GetString()), float.Parse(footLBend.ElementAt(1).GetString()), float.Parse(footLBend.ElementAt(2).GetString()));
                        JsonElement.ArrayEnumerator footRBend = res.GetProperty("footRBend").EnumerateArray();
                        Vector3 footRBendPos = new Vector3(float.Parse(footRBend.ElementAt(0).GetString()), float.Parse(footRBend.ElementAt(1).GetString()), float.Parse(footRBend.ElementAt(2).GetString()));

                        JsonElement.ArrayEnumerator rotation = res.GetProperty("rotation").EnumerateArray();
                        Quaternion bodyRot = new Quaternion(float.Parse(rotation.ElementAt(0).GetString()), float.Parse(rotation.ElementAt(1).GetString()), float.Parse(rotation.ElementAt(2).GetString()), float.Parse(rotation.ElementAt(3).GetString()));
                        JsonElement.ArrayEnumerator handLrot = res.GetProperty("handLRotation").EnumerateArray();
                        Quaternion handLRot = new Quaternion(float.Parse(handLrot.ElementAt(0).GetString()), float.Parse(handLrot.ElementAt(1).GetString()), float.Parse(handLrot.ElementAt(2).GetString()), float.Parse(handLrot.ElementAt(3).GetString()));
                        JsonElement.ArrayEnumerator handRrot = res.GetProperty("handRRotation").EnumerateArray();
                        Quaternion handRRot = new Quaternion(float.Parse(handRrot.ElementAt(0).GetString()), float.Parse(handRrot.ElementAt(1).GetString()), float.Parse(handRrot.ElementAt(2).GetString()), float.Parse(handRrot.ElementAt(3).GetString()));
                        JsonElement.ArrayEnumerator footLrot = res.GetProperty("footLRotation").EnumerateArray();
                        Quaternion footLRot = new Quaternion(float.Parse(footLrot.ElementAt(0).GetString()), float.Parse(footLrot.ElementAt(1).GetString()), float.Parse(footLrot.ElementAt(2).GetString()), float.Parse(footLrot.ElementAt(3).GetString()));
                        JsonElement.ArrayEnumerator footRrot = res.GetProperty("footRRotation").EnumerateArray();
                        Quaternion footRRot = new Quaternion(float.Parse(footRrot.ElementAt(0).GetString()), float.Parse(footRrot.ElementAt(1).GetString()), float.Parse(footRrot.ElementAt(2).GetString()), float.Parse(footRrot.ElementAt(3).GetString()));

                        playerToUpdate2.UpdatePosition(bodyPos, float.Parse(res.GetProperty("height").GetString()), handLPos, handRPos, float.Parse(res.GetProperty("armStrechL").GetString()), float.Parse(res.GetProperty("armStrechR").GetString()), footLPos, footRPos, footLBendPos, footRBendPos, bodyRot, handLRot, handRRot, footLRot, footRRot);
                        break;
                }
            };

            ws.ConnectAsync();
        }

        public override async void OnApplicationStart()
        {
            logger = LoggerInstance;
            thisAssembly = MelonAssembly.Assembly;

            makingShadowPrefab = true;
            MelonCoroutines.Start(MakeShadowPrefab());
            makingShadowPrefab = false;

            string[] args = Environment.GetCommandLineArgs();

            for (int i = 1; i < args.Length; i++)
            {
                if (args[i] == "-bwf-debug")
                {
                    debugMode = true;
                    LoggerInstance.Msg("In debug mode");
                } else if (args[i] == "-server" && i + 1 < args.Length)
                {
                    server = args[i + 1];
                    LoggerInstance.Msg("Custom server");
                } else if (args[i] == "-more-logs")
                {
                    moreLogs = true;
                    LoggerInstance.Msg("More Logs");
                } else if (args[i] == "-comp-mode")
                {
                    compMode = true;
                } else if (args[i] == "-caster-mode")
                {
                    casterMode = true;
                }
            }
        }

        IEnumerator MakeShadowPrefab()
        {
            LoggerInstance.Msg("Making Shadow Prefab");

            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(2, LoadSceneMode.Additive);

            while (!asyncLoad.isDone)
            {
                yield return null;
            }

            PlayerShadow[] shads = Resources.FindObjectsOfTypeAll<PlayerShadow>();
            if (shads.Length != 0)
            {
                LoggerInstance.Msg("Found Shadow I'm Stealing");

                shadowPrefab = GameObject.Instantiate(shads[0].gameObject);
                GameObject.Destroy(shadowPrefab.GetComponent<PlayerShadow>());
                GameObject.DontDestroyOnLoad(shadowPrefab);
                shadowPrefab.transform.position = new Vector3(0, -1000000, 0);

                /*for (int i = 0; i < 10; i++)
                {
                    shadowPrefabs.Add(GameObject.Instantiate(shadowPrefab));
                    GameObject.Destroy(shadowPrefabs[i].GetComponent<PlayerShadow>());
                    GameObject.DontDestroyOnLoad(shadowPrefabs[i]);
                    shadowPrefabs[i].transform.position = new Vector3(0, -1000000, 0);
                }*/
            }

            AsyncOperation asyncUnload = SceneManager.UnloadSceneAsync(2);

            while (!asyncLoad.isDone)
            {
                yield return null;
            }

            SceneManager.LoadScene(SceneManager.GetActiveScene().name, LoadSceneMode.Single);
            LoggerInstance.Msg("Making Shadow Prefab Done");
        }

        public override void OnDeinitializeMelon()
        {
            if (!connected) return;
            ws.Send($"{{\"data\":\"yeet\", \"id\":\"{playerId}\"}}");
            amHost = false;
        }

        public void MakeMenu()
        {
            LoggerInstance.Msg("Making Multiplayer Menu");

            multiplayerMenuObject = new GameObject("Multiplayer Canvas");
            UnityEngine.Object.DontDestroyOnLoad(multiplayerMenuObject);
            multiplayerMenuCanvas = multiplayerMenuObject.AddComponent<Canvas>();
            multiplayerMenuCanvas.sortingOrder = 1;
            multiplayerMenuCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = multiplayerMenuObject.AddComponent<CanvasScaler>();
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

            multiplayerRaycaster = multiplayerMenuObject.AddComponent<GraphicRaycaster>();

            multiplayerMenu = new GameObject("background");
            multiplayerMenu.transform.SetParent(multiplayerMenuObject.transform);
            Image bgIm = multiplayerMenu.AddComponent<Image>();
            bgIm.rectTransform.sizeDelta = new Vector2(1820, 780);
            bgIm.rectTransform.anchoredPosition = new Vector2(0, 100);
            bgIm.color = new Color(0, 0, 0, 0.85f);

            GameObject scroller = new GameObject("Room List");
            scroller.transform.SetParent(multiplayerMenu.transform);

            GameObject viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scroller.transform);

            roomContainer = new GameObject("Content");
            roomContainer.transform.SetParent(viewport.transform);

            ScrollRect roomScroll = scroller.AddComponent<ScrollRect>();
            roomScroll.horizontal = false;
            roomScroll.elasticity = 0.05f;

            RectMask2D mask = viewport.AddComponent<RectMask2D>();
            Image maskImage = viewport.AddComponent<Image>();
            mask.rectTransform.sizeDelta = new Vector2(1320, 730);
            maskImage.color = new Color(0, 0, 0, 0.75f);

            roomBack = roomContainer.AddComponent<Image>();
            roomBack.color = new Color(0, 0, 0, 0.5f);
            roomBack.rectTransform.pivot = new Vector2(0.5f, 1);
            VerticalLayoutGroup verticalLayoutGroup0 = roomContainer.AddComponent<VerticalLayoutGroup>();
            ContentSizeFitter contentSizeFitter0 = roomContainer.AddComponent<ContentSizeFitter>();
            contentSizeFitter0.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            verticalLayoutGroup0.childAlignment = TextAnchor.UpperCenter;
            verticalLayoutGroup0.childControlHeight = false;
            verticalLayoutGroup0.childControlWidth = false;
            verticalLayoutGroup0.childForceExpandHeight = false;
            verticalLayoutGroup0.childForceExpandWidth = false;
            verticalLayoutGroup0.childScaleHeight = false;
            verticalLayoutGroup0.childScaleWidth = false;
            verticalLayoutGroup0.spacing = 5;

            roomScroll.viewport = mask.rectTransform;
            roomScroll.content = roomContainer.GetComponent<Image>().rectTransform;

            scroller.transform.localPosition = new Vector3(225, 0, 0);
            roomBack.rectTransform.sizeDelta = new Vector2(1320, 110 * roomContainer.transform.childCount - 10);

            updateContainer = new GameObject("Updates");
            updateContainer.transform.SetParent(multiplayerMenuObject.transform);
            RectTransform rectTrans = updateContainer.AddComponent<RectTransform>();
            VerticalLayoutGroup verticalLayoutGroup = updateContainer.AddComponent<VerticalLayoutGroup>();
            ContentSizeFitter contentSizeFitter = updateContainer.AddComponent<ContentSizeFitter>();
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            verticalLayoutGroup.childAlignment = TextAnchor.UpperCenter;
            verticalLayoutGroup.childControlHeight = false;
            verticalLayoutGroup.childControlWidth = false;
            verticalLayoutGroup.childForceExpandHeight = false;
            verticalLayoutGroup.childForceExpandWidth = false;
            verticalLayoutGroup.childScaleHeight = false;
            verticalLayoutGroup.childScaleWidth = false;
            verticalLayoutGroup.spacing = 5;
            rectTrans.anchoredPosition = new Vector2(0, 540);
            rectTrans.pivot = new Vector2(0.5f, 1);

            GameObject refreshButtonOb = new GameObject("Refesh Button");
            refreshButtonOb.transform.SetParent(multiplayerMenu.transform);
            refreshButtonOb.transform.localPosition = new Vector3(-550, 300, 0);
            UnityEngine.UI.Button joinButton = refreshButtonOb.AddComponent<UnityEngine.UI.Button>();
            Image joinBG = refreshButtonOb.AddComponent<Image>();
            joinBG.color = new Color(0.9f, 0.9f, 0.9f, 1);
            joinBG.rectTransform.sizeDelta = new Vector2(200, 50);
            joinButton.image = joinBG;
            joinButton.onClick.AddListener(() =>
            {
                if (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() > lastRefresh)
                {
                    lastRefresh = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    ws.Send($"{{\"data\":\"getRooms\"}}");
                }
            });

            GameObject refreshTextOb = new GameObject("Refresh");
            refreshTextOb.transform.SetParent(refreshButtonOb.transform);
            refreshTextOb.transform.localPosition = Vector3.zero;
            Text refreshText = refreshTextOb.AddComponent<Text>();
            refreshText.font = arial;
            refreshText.fontSize = 40;
            refreshText.alignment = TextAnchor.MiddleCenter;
            refreshText.text = "Refresh";
            refreshText.color = Color.black;
            refreshText.rectTransform.sizeDelta = new Vector2(200, 50);

            //refreshButtonOb.SetActive(false);

            GameObject roomOb = new GameObject("Room Name");
            roomOb.transform.SetParent(multiplayerMenu.transform);
            roomOb.transform.localPosition = new Vector3(-650, 150, 0);
            InputField roomName = roomOb.AddComponent<InputField>();
            roomName.caretColor = new Color(0f, 0f, 0f, 1);
            Image roomBG = roomOb.AddComponent<Image>();
            roomBG.color = new Color(0.9f, 0.9f, 0.9f, 1);
            roomBG.rectTransform.sizeDelta = new Vector2(200, 50);

            GameObject placeholder = new GameObject("placeholder");
            placeholder.transform.SetParent(roomOb.transform);
            placeholder.transform.localPosition = Vector3.zero;
            Text placeHolderText = placeholder.AddComponent<Text>();
            placeHolderText.text = "Room name";
            placeHolderText.font = arial;
            placeHolderText.fontSize = 35;
            placeHolderText.alignment = TextAnchor.MiddleLeft;
            placeHolderText.color = Color.black;
            placeHolderText.rectTransform.sizeDelta = new Vector2(200, 50);

            GameObject input = new GameObject("input");
            input.transform.SetParent(roomOb.transform);
            input.transform.localPosition = Vector3.zero;
            Text inputText = input.AddComponent<Text>();
            inputText.font = arial;
            inputText.fontSize = 35;
            inputText.alignment = TextAnchor.MiddleLeft;
            inputText.color = Color.black;
            inputText.rectTransform.sizeDelta = new Vector2(200, 50);

            roomName.placeholder = placeHolderText;
            roomName.textComponent = inputText;
            roomName.transition = Selectable.Transition.None;

            inputFields.Add(roomName);
            inputFieldChecks.Add(roomName, false);
            roomName.gameObject.AddComponent<EventSystem>().enabled = false;

            GameObject roomOb2 = new GameObject("Room Password");
            roomOb2.transform.SetParent(multiplayerMenu.transform);
            roomOb2.transform.localPosition = new Vector3(-650, 75, 0);
            InputField roomName2 = roomOb2.AddComponent<InputField>();
            roomName2.caretColor = new Color(0f, 0f, 0f, 1);
            Image roomBG2 = roomOb2.AddComponent<Image>();
            roomBG2.color = new Color(0.9f, 0.9f, 0.9f, 1);
            roomBG2.rectTransform.sizeDelta = new Vector2(200, 50);
            roomName2.inputType = InputField.InputType.Password;

            GameObject placeholder2 = new GameObject("placeholder");
            placeholder2.transform.SetParent(roomOb2.transform);
            placeholder2.transform.localPosition = Vector3.zero;
            Text placeHolderText2 = placeholder2.AddComponent<Text>();
            placeHolderText2.text = "Room password\nor leave empty";
            placeHolderText2.font = arial;
            placeHolderText2.fontSize = 15;
            placeHolderText2.alignment = TextAnchor.MiddleLeft;
            placeHolderText2.color = Color.black;
            placeHolderText2.rectTransform.sizeDelta = new Vector2(200, 50);

            GameObject input2 = new GameObject("input");
            input2.transform.SetParent(roomOb2.transform);
            input2.transform.localPosition = Vector3.zero;
            Text inputText2 = input2.AddComponent<Text>();
            inputText2.font = arial;
            inputText2.fontSize = 38;
            inputText2.alignment = TextAnchor.MiddleLeft;
            inputText2.color = Color.black;
            inputText2.rectTransform.sizeDelta = new Vector2(200, 50);

            roomName2.placeholder = placeHolderText2;
            roomName2.textComponent = inputText2;
            roomName2.transition = Selectable.Transition.None;

            inputFields.Add(roomName2);
            inputFieldChecks.Add(roomName2, false);
            roomName2.gameObject.AddComponent<EventSystem>().enabled = false;

            GameObject makeRoomOb = new GameObject("Make Room");
            makeRoomOb.transform.SetParent(multiplayerMenu.transform);
            makeRoomOb.transform.localPosition = new Vector3(-650, 0, 0);
            UnityEngine.UI.Button makeRoom = makeRoomOb.AddComponent<UnityEngine.UI.Button>();
            Image makeBG = makeRoomOb.AddComponent<Image>();
            makeBG.color = new Color(0.9f, 0.9f, 0.9f, 1);
            makeBG.rectTransform.sizeDelta = new Vector2(200, 50);
            makeRoom.image = makeBG;
            makeRoom.onClick.AddListener(() =>
            {
                if (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() > lastRefresh)
                {
                    lastRefresh = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                    if (roomName.text == "")
                    {
                        MakeInfoText("The room name can't be blank!", Color.red);
                    } else
                    {
                        string[] assemblyHashes = GetAssemblyHashes();

                        ws.Send($"{{\"data\":\"makeRoom\", \"id\":\"{playerId}\", \"name\":\"{roomName.text}\", \"pass\":\"{roomName2.text}\", \"hash\":\"{assemblyHashes[0]}\", \"hash2\":\"{assemblyHashes[1]}\"}}");
                    }
                }
            });

            GameObject makeRoomTextOb = new GameObject("Make");
            makeRoomTextOb.transform.SetParent(makeRoomOb.transform);
            makeRoomTextOb.transform.localPosition = Vector3.zero;
            Text makeRoomText = makeRoomTextOb.AddComponent<Text>();
            makeRoomText.font = arial;
            makeRoomText.fontSize = 36;
            makeRoomText.alignment = TextAnchor.MiddleCenter;
            makeRoomText.text = "Make Room";
            makeRoomText.color = Color.black;
            makeRoomText.rectTransform.sizeDelta = new Vector2(200, 50);

            GameObject KofiOb = new GameObject("Kofi Button");
            KofiOb.transform.SetParent(multiplayerMenu.transform);
            KofiOb.transform.localPosition = Vector2.zero;
            UnityEngine.UI.Button kofiButton = KofiOb.AddComponent<UnityEngine.UI.Button>();
            Image kofi = KofiOb.AddComponent<Image>();
            byte[] imageBytes = new byte[(int)thisAssembly.GetManifestResourceStream("Bag_With_Friends.Kofi.png").Length];
            thisAssembly.GetManifestResourceStream("Bag_With_Friends.Kofi.png").Read(imageBytes, 0, (int)thisAssembly.GetManifestResourceStream("Bag_With_Friends.Kofi.png").Length);
            Texture2D tex = new Texture2D(2, 2);
            ImageConversion.LoadImage(tex, imageBytes);
            kofi.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            kofi.rectTransform.sizeDelta = new Vector2(tex.width / 3f, tex.height / 3f);
            kofi.rectTransform.pivot = Vector2.zero;
            kofi.rectTransform.anchoredPosition = new Vector2(-910, -390);
            kofiButton.image = kofi;
            kofiButton.onClick.AddListener(() =>
            {
                Application.OpenURL("https://ko-fi.com/givowo");
            });

            GameObject playerSettingsButtonOb = new GameObject("Player Settings Button");
            playerSettingsButtonOb.transform.SetParent(multiplayerMenu.transform);
            UnityEngine.UI.Button playerSettingsButton = playerSettingsButtonOb.AddComponent<UnityEngine.UI.Button>();
            Image playerSettings = playerSettingsButtonOb.AddComponent<Image>();
            playerSettings.color = new Color(0.9f, 0.9f, 0.9f, 1);
            playerSettings.rectTransform.sizeDelta = new Vector2(100, 50);
            playerSettings.rectTransform.pivot = new Vector2(0, 1);
            playerSettings.rectTransform.anchoredPosition = new Vector2(-910, 390);
            playerSettingsButton.image = playerSettings;
            playerSettingsButton.onClick.AddListener(() =>
            {
                multiplayerMenu.SetActive(false);
                playerSettingsMenu.SetActive(true);
            });

            GameObject playerSettingsOb = new GameObject("Player Settings");
            playerSettingsOb.transform.SetParent(playerSettingsButtonOb.transform);
            playerSettingsOb.transform.localPosition = Vector3.zero;
            Text playerSettingsText = playerSettingsOb.AddComponent<Text>();
            playerSettingsText.font = arial;
            playerSettingsText.fontSize = 20;
            playerSettingsText.alignment = TextAnchor.MiddleCenter;
            playerSettingsText.text = "player\nsettings";
            playerSettingsText.color = Color.black;
            playerSettingsText.rectTransform.sizeDelta = new Vector2(100, 50);

            playerContainer = new GameObject("Players");
            GameObject.DontDestroyOnLoad(playerContainer);
        }

        public void MakeRoomListing(string name, ulong roomId, bool hasPass, int playerCount, string hostname)
        {
            GameObject room = new GameObject("Room");
            room.transform.SetParent(roomContainer.transform);
            Image bg = room.AddComponent<Image>();
            bg.color = new Color(1, 1, 1, 0.05f);
            bg.rectTransform.sizeDelta = new Vector2(1320, 100);

            GameObject roomNameOb = new GameObject("Name");
            roomNameOb.transform.SetParent(room.transform);
            roomNameOb.transform.localPosition = new Vector3(-650, 0, 0);
            Text roomName = roomNameOb.AddComponent<Text>();
            roomName.rectTransform.pivot = new Vector2(0, 0.5f);
            roomName.rectTransform.sizeDelta = new Vector2(600, 100);
            roomName.text = name;
            roomName.fontSize = 48;
            roomName.font = arial;
            roomName.lineSpacing = 0.85f;
            roomName.alignment = TextAnchor.MiddleLeft;

            GameObject needPassOb = new GameObject("Pass");
            needPassOb.transform.SetParent(room.transform);
            needPassOb.transform.localPosition = new Vector3(0, 0, 0);
            Text needPass = needPassOb.AddComponent<Text>();
            needPass.text = hasPass ? "Need Pass" : "No Pass";
            needPass.color = hasPass ? new Color(1, 0, 0, 1) : new Color(0, 1, 0, 1);
            needPass.fontSize = 36;
            needPass.font = arial;
            needPass.alignment = TextAnchor.MiddleCenter;

            GameObject playersOb = new GameObject("Player Count");
            playersOb.transform.SetParent(room.transform);
            playersOb.transform.localPosition = new Vector3(125, 0, 0);
            Text players = playersOb.AddComponent<Text>();
            players.rectTransform.sizeDelta = new Vector2(120, 100);
            players.text = playerCount + " Players";
            players.fontSize = 36;
            players.font = arial;
            players.alignment = TextAnchor.MiddleCenter;

            GameObject hostOb = new GameObject("Host");
            hostOb.transform.SetParent(room.transform);
            hostOb.transform.localPosition = new Vector3(300, 0, 0);
            Text host = hostOb.AddComponent<Text>();
            host.rectTransform.sizeDelta = new Vector2(230, 100);
            host.text = "Host: \n" + hostname;
            host.fontSize = 36;
            host.font = arial;
            host.alignment = TextAnchor.MiddleCenter;
            host.color = new Color(0, 0, 1, 1);

            GameObject joinButtonOb = new GameObject("Join Button");
            joinButtonOb.transform.SetParent(room.transform);
            joinButtonOb.transform.localPosition = new Vector3(540, 0, 0);
            UnityEngine.UI.Button joinButton = joinButtonOb.AddComponent<UnityEngine.UI.Button>();
            Image joinBG = joinButtonOb.AddComponent<Image>();
            joinBG.color = new Color(1, 1, 1, 0.3f);
            joinBG.rectTransform.sizeDelta = new Vector2(230, 88);
            joinButton.image = joinBG;

            GameObject joinTextOb = new GameObject("Join");
            joinTextOb.transform.SetParent(joinButtonOb.transform);
            joinTextOb.transform.localPosition = Vector3.zero;
            Text joinText = joinTextOb.AddComponent<Text>();
            joinText.font = arial;
            joinText.fontSize = 40;
            joinText.alignment = TextAnchor.MiddleCenter;
            joinText.text = "Join";
            joinText.rectTransform.sizeDelta = new Vector2(230, 88);

            InputField passInput = null;

            if (hasPass)
            {
                joinButtonOb.transform.localPosition = new Vector3(540, -23, 0);
                joinBG.rectTransform.sizeDelta = new Vector2(230, 44);
                joinText.rectTransform.sizeDelta = new Vector2(230, 44);

                GameObject passOb = new GameObject("Password Input");
                passOb.transform.SetParent(room.transform);
                passOb.transform.localPosition = new Vector3(540, 23, 0);
                passInput = passOb.AddComponent<InputField>();
                passInput.inputType = InputField.InputType.Password;
                passInput.caretColor = new Color(0.5f, 0.5f, 0.5f, 1);
                Image inputBG = passOb.AddComponent<Image>();
                inputBG.color = new Color(1, 1, 1, 0.1f);
                inputBG.rectTransform.sizeDelta = new Vector2(230, 44);

                GameObject placeholder = new GameObject("placeholder");
                placeholder.transform.SetParent(passOb.transform);
                placeholder.transform.localPosition = Vector3.zero;
                Text placeHolderText = placeholder.AddComponent<Text>();
                placeHolderText.text = "Enter password";
                placeHolderText.font = arial;
                placeHolderText.fontSize = 28;
                placeHolderText.alignment = TextAnchor.MiddleLeft;
                placeHolderText.rectTransform.sizeDelta = new Vector2(230, 44);

                GameObject input = new GameObject("input");
                input.transform.SetParent(passOb.transform);
                input.transform.localPosition = Vector3.zero;
                Text inputText = input.AddComponent<Text>();
                inputText.font = arial;
                inputText.fontSize = 40;
                inputText.alignment = TextAnchor.MiddleLeft;
                inputText.rectTransform.sizeDelta = new Vector2(230, 44);

                passInput.placeholder = placeHolderText;
                passInput.textComponent = inputText;
                passInput.transition = Selectable.Transition.None;

                inputFields.Add(passInput);
                inputFieldChecks.Add(passInput, false);
                passInput.gameObject.AddComponent<EventSystem>().enabled = false;
            }

            joinButton.onClick.AddListener(() =>
            {
                string password = "";
                if (passInput != null)
                {
                    password = passInput.text;
                }
                LoggerInstance.Msg("Trying to join room " + name + " with password " + password);

                string[] assemblyHashes = GetAssemblyHashes();

                ws.Send($"{{\"data\":\"joinRoom\", \"id\":\"{playerId}\", \"room\":{roomId}, \"pass\":\"{password}\", \"hash\":\"{assemblyHashes[0]}\", \"hash2\":\"{assemblyHashes[1]}\"}}");
            });

            room.transform.localScale = multiplayerMenu.transform.localScale;
        }

        public void MakeRoomMenu()
        {
            LoggerInstance.Msg("Making Room Menu");

            roomMenu = new GameObject("background");
            roomMenu.transform.SetParent(multiplayerMenuObject.transform);
            Image bgIm = roomMenu.AddComponent<Image>();
            bgIm.rectTransform.sizeDelta = new Vector2(1820, 780);
            bgIm.rectTransform.anchoredPosition = new Vector2(0, 100);
            bgIm.color = new Color(0, 0, 0, 0.85f);

            GameObject scroller = new GameObject("Player List");
            scroller.transform.SetParent(roomMenu.transform);

            GameObject viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scroller.transform);

            playerMenuContainer = new GameObject("Content");
            playerMenuContainer.transform.SetParent(viewport.transform);

            ScrollRect playerScroll = scroller.AddComponent<ScrollRect>();
            playerScroll.horizontal = false;
            playerScroll.elasticity = 0.05f;

            RectMask2D mask = viewport.AddComponent<RectMask2D>();
            Image maskImage = viewport.AddComponent<Image>();
            mask.rectTransform.sizeDelta = new Vector2(1320, 730);
            maskImage.color = new Color(0, 0, 0, 0.75f);

            playerBack = playerMenuContainer.AddComponent<Image>();
            playerBack.color = new Color(0, 0, 0, 0.5f);
            playerBack.rectTransform.pivot = new Vector2(0.5f, 1);
            VerticalLayoutGroup verticalLayoutGroup0 = playerMenuContainer.AddComponent<VerticalLayoutGroup>();
            ContentSizeFitter contentSizeFitter0 = playerMenuContainer.AddComponent<ContentSizeFitter>();
            contentSizeFitter0.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            verticalLayoutGroup0.childAlignment = TextAnchor.UpperCenter;
            verticalLayoutGroup0.childControlHeight = false;
            verticalLayoutGroup0.childControlWidth = false;
            verticalLayoutGroup0.childForceExpandHeight = false;
            verticalLayoutGroup0.childForceExpandWidth = false;
            verticalLayoutGroup0.childScaleHeight = false;
            verticalLayoutGroup0.childScaleWidth = false;
            verticalLayoutGroup0.spacing = 5;

            playerScroll.viewport = mask.rectTransform;
            playerScroll.content = playerMenuContainer.GetComponent<Image>().rectTransform;

            scroller.transform.localPosition = new Vector3(225, 0, 0);
            playerBack.rectTransform.sizeDelta = new Vector2(1320, 110 * playerMenuContainer.transform.childCount - 10);

            GameObject roomOb = new GameObject("Room Name");
            roomOb.transform.SetParent(roomMenu.transform);
            roomOb.transform.localPosition = new Vector3(-650, 150, 0);
            roomMenuName = roomOb.AddComponent<InputField>();
            roomMenuName.caretColor = new Color(0f, 0f, 0f, 1);
            Image roomBG = roomOb.AddComponent<Image>();
            roomBG.color = new Color(0.9f, 0.9f, 0.9f, 1);
            roomBG.rectTransform.sizeDelta = new Vector2(200, 50);

            GameObject placeholder = new GameObject("placeholder");
            placeholder.transform.SetParent(roomOb.transform);
            placeholder.transform.localPosition = Vector3.zero;
            roomMenuRoomName = placeholder.AddComponent<Text>();
            roomMenuRoomName.text = "New name";
            roomMenuRoomName.font = arial;
            roomMenuRoomName.fontSize = 35;
            roomMenuRoomName.alignment = TextAnchor.MiddleLeft;
            roomMenuRoomName.color = Color.black;
            roomMenuRoomName.rectTransform.sizeDelta = new Vector2(200, 50);

            GameObject input = new GameObject("input");
            input.transform.SetParent(roomOb.transform);
            input.transform.localPosition = Vector3.zero;
            Text inputText = input.AddComponent<Text>();
            inputText.font = arial;
            inputText.fontSize = 35;
            inputText.alignment = TextAnchor.MiddleLeft;
            inputText.color = Color.black;
            inputText.rectTransform.sizeDelta = new Vector2(200, 50);

            roomMenuName.placeholder = roomMenuRoomName;
            roomMenuName.textComponent = inputText;
            roomMenuName.transition = Selectable.Transition.None;

            inputFields.Add(roomMenuName);
            inputFieldChecks.Add(roomMenuName, false);
            roomMenuName.gameObject.AddComponent<EventSystem>().enabled = false;

            GameObject roomOb2 = new GameObject("Room Password");
            roomOb2.transform.SetParent(roomMenu.transform);
            roomOb2.transform.localPosition = new Vector3(-650, 75, 0);
            roomMenuPass = roomOb2.AddComponent<InputField>();
            roomMenuPass.caretColor = new Color(0f, 0f, 0f, 1);
            Image roomBG2 = roomOb2.AddComponent<Image>();
            roomBG2.color = new Color(0.9f, 0.9f, 0.9f, 1);
            roomBG2.rectTransform.sizeDelta = new Vector2(200, 50);
            roomMenuPass.inputType = InputField.InputType.Password;

            GameObject placeholder2 = new GameObject("placeholder");
            placeholder2.transform.SetParent(roomOb2.transform);
            placeholder2.transform.localPosition = Vector3.zero;
            roomMenuRoomPass = placeholder2.AddComponent<Text>();
            roomMenuRoomPass.text = "New pass\nor empty";
            roomMenuRoomPass.font = arial;
            roomMenuRoomPass.fontSize = 15;
            roomMenuRoomPass.alignment = TextAnchor.MiddleLeft;
            roomMenuRoomPass.color = Color.black;
            roomMenuRoomPass.rectTransform.sizeDelta = new Vector2(200, 50);

            GameObject input2 = new GameObject("input");
            input2.transform.SetParent(roomOb2.transform);
            input2.transform.localPosition = Vector3.zero;
            Text inputText2 = input2.AddComponent<Text>();
            inputText2.font = arial;
            inputText2.fontSize = 38;
            inputText2.alignment = TextAnchor.MiddleLeft;
            inputText2.color = Color.black;
            inputText2.rectTransform.sizeDelta = new Vector2(200, 50);

            roomMenuPass.placeholder = roomMenuRoomPass;
            roomMenuPass.textComponent = inputText2;
            roomMenuPass.transition = Selectable.Transition.None;

            inputFields.Add(roomMenuPass);
            inputFieldChecks.Add(roomMenuPass, false);
            roomMenuPass.gameObject.AddComponent<EventSystem>().enabled = false;

            GameObject makeRoomOb = new GameObject("Update Room");
            makeRoomOb.transform.SetParent(roomMenu.transform);
            makeRoomOb.transform.localPosition = new Vector3(-650, 0, 0);
            roomMenuUpdate = makeRoomOb.AddComponent<UnityEngine.UI.Button>();
            Image makeBG = makeRoomOb.AddComponent<Image>();
            makeBG.color = new Color(0.9f, 0.9f, 0.9f, 1);
            makeBG.rectTransform.sizeDelta = new Vector2(200, 50);
            roomMenuUpdate.image = makeBG;
            roomMenuUpdate.onClick.AddListener(() =>
            {
                if (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() > lastRefresh)
                {
                    lastRefresh = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    ws.Send($"{{\"data\":\"updateRoom\", \"id\":\"{playerId}\", \"name\":\"{roomMenuName.text}\", \"pass\":\"{roomMenuPass.text}\"}}");
                }
            });

            GameObject makeRoomTextOb = new GameObject("Update");
            makeRoomTextOb.transform.SetParent(makeRoomOb.transform);
            makeRoomTextOb.transform.localPosition = Vector3.zero;
            Text makeRoomText = makeRoomTextOb.AddComponent<Text>();
            makeRoomText.font = arial;
            makeRoomText.fontSize = 36;
            makeRoomText.alignment = TextAnchor.MiddleCenter;
            makeRoomText.text = "Update Room";
            makeRoomText.color = Color.black;
            makeRoomText.rectTransform.sizeDelta = new Vector2(200, 50);

            updateRoomButton = makeRoomOb;
            updateRoomButton.SetActive(amHost);

            GameObject KofiOb = new GameObject("Kofi Button");
            KofiOb.transform.SetParent(roomMenu.transform);
            KofiOb.transform.localPosition = Vector2.zero;
            UnityEngine.UI.Button kofiButton = KofiOb.AddComponent<UnityEngine.UI.Button>();
            Image kofi = KofiOb.AddComponent<Image>();
            byte[] imageBytes = new byte[(int)thisAssembly.GetManifestResourceStream("Bag_With_Friends.Kofi.png").Length];
            thisAssembly.GetManifestResourceStream("Bag_With_Friends.Kofi.png").Read(imageBytes, 0, (int)thisAssembly.GetManifestResourceStream("Bag_With_Friends.Kofi.png").Length);
            Texture2D tex = new Texture2D(2, 2);
            ImageConversion.LoadImage(tex, imageBytes);
            kofi.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            kofi.rectTransform.sizeDelta = new Vector2(tex.width / 3f, tex.height / 3f);
            kofi.rectTransform.pivot = Vector2.zero;
            kofi.rectTransform.anchoredPosition = new Vector2(-910, -390);
            kofiButton.image = kofi;
            kofiButton.onClick.AddListener(() =>
            {
                Application.OpenURL("https://ko-fi.com/givowo");
            });

            GameObject leaveRoomOb = new GameObject("Leave Room");
            leaveRoomOb.transform.SetParent(roomMenu.transform);
            leaveRoomOb.transform.localPosition = new Vector3(-650, -225, 0);
            UnityEngine.UI.Button leaveRoom = leaveRoomOb.AddComponent<UnityEngine.UI.Button>();
            Image leaveBG = leaveRoomOb.AddComponent<Image>();
            leaveBG.color = new Color(0.9f, 0.9f, 0.9f, 1);
            leaveBG.rectTransform.sizeDelta = new Vector2(200, 50);
            leaveRoom.image = leaveBG;
            leaveRoom.onClick.AddListener(() =>
            {
                if (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() > lastRefresh)
                {
                    lastRefresh = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    ws.Send($"{{\"data\":\"leaveRoom\", \"id\":\"{playerId}\"}}");

                    for (int i = 0; i < playerMenuContainer.transform.childCount; i++)
                    {
                        GameObject.Destroy(playerMenuContainer.transform.GetChild(i));
                    }

                    frozen = false;
                    PeakSummited[] summiteds = Resources.FindObjectsOfTypeAll<PeakSummited>();
                    if (summiteds.Length != 0)
                    {
                        summiteds[0].DisablePlayerMovement(frozen);
                    }
                }
            });

            GameObject leaveRoomTextOb = new GameObject("Update");
            leaveRoomTextOb.transform.SetParent(leaveRoomOb.transform);
            leaveRoomTextOb.transform.localPosition = Vector3.zero;
            Text leaveRoomText = leaveRoomTextOb.AddComponent<Text>();
            leaveRoomText.font = arial;
            leaveRoomText.fontSize = 36;
            leaveRoomText.alignment = TextAnchor.MiddleCenter;
            leaveRoomText.text = "Leave Room";
            leaveRoomText.color = Color.black;
            leaveRoomText.rectTransform.sizeDelta = new Vector2(200, 50);

            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                string[] splitNam = SceneUtility.GetScenePathByBuildIndex(i).Split('/');
                string scene = splitNam[splitNam.Length - 1].Split('.')[0];

                GameObject sceneOb = new GameObject("Scene " + scene);
                sceneOb.transform.SetParent(playerMenuContainer.transform);
                Image bg = sceneOb.AddComponent<Image>();
                bg.color = new Color(1, 1, 1, 0.05f);
                bg.rectTransform.sizeDelta = new Vector2(1320, 50);

                GameObject sceneNameOb = new GameObject("Name");
                sceneNameOb.transform.SetParent(sceneOb.transform);
                sceneNameOb.transform.localPosition = new Vector3(-650, 0, 0);
                Text sceneName = sceneNameOb.AddComponent<Text>();
                sceneName.rectTransform.pivot = new Vector2(0, 0.5f);
                sceneName.rectTransform.sizeDelta = new Vector2(650, 50);
                sceneName.text = scene;
                sceneName.fontSize = 36;
                sceneName.font = arial;
                sceneName.lineSpacing = 0.85f;
                sceneName.color = new Color(0, 1, 0, 1);
                sceneName.alignment = TextAnchor.MiddleLeft;

                sceneOb.transform.localScale = roomMenu.transform.localScale;
                sceneSplitters.Add(scene, sceneOb.transform);
            }

            GameObject playerSettingsButtonOb = new GameObject("Player Settings Button");
            playerSettingsButtonOb.transform.SetParent(roomMenu.transform);
            UnityEngine.UI.Button playerSettingsButton = playerSettingsButtonOb.AddComponent<UnityEngine.UI.Button>();
            Image playerSettings = playerSettingsButtonOb.AddComponent<Image>();
            playerSettings.color = new Color(0.9f, 0.9f, 0.9f, 1);
            playerSettings.rectTransform.sizeDelta = new Vector2(100, 50);
            playerSettings.rectTransform.pivot = new Vector2(0, 1);
            playerSettings.rectTransform.anchoredPosition = new Vector2(-910, 390);
            playerSettingsButton.image = playerSettings;
            playerSettingsButton.onClick.AddListener(() =>
            {
                roomMenu.SetActive(false);
                playerSettingsMenu.SetActive(true);
            });

            GameObject playerSettingsOb = new GameObject("Player Settings");
            playerSettingsOb.transform.SetParent(playerSettingsButtonOb.transform);
            playerSettingsOb.transform.localPosition = Vector3.zero;
            Text playerSettingsText = playerSettingsOb.AddComponent<Text>();
            playerSettingsText.font = arial;
            playerSettingsText.fontSize = 20;
            playerSettingsText.alignment = TextAnchor.MiddleCenter;
            playerSettingsText.text = "player\nsettings";
            playerSettingsText.color = Color.black;
            playerSettingsText.rectTransform.sizeDelta = new Vector2(100, 50);

            GameObject freezePlayersOb = new GameObject("Freeze Players Button");
            freezePlayersOb.transform.SetParent(roomMenu.transform);
            freezePlayersOb.transform.localPosition = new Vector3(-650, 350, 0);
            UnityEngine.UI.Button freezePlayersButton = freezePlayersOb.AddComponent<UnityEngine.UI.Button>();
            Image freezePlayersBG = freezePlayersOb.AddComponent<Image>();
            freezePlayersBG.color = new Color(0.9f, 0.9f, 0.9f, 1);
            freezePlayersBG.rectTransform.sizeDelta = new Vector2(200, 50);
            freezePlayersBG.rectTransform.pivot = new Vector2(0, 1);
            freezePlayersButton.image = freezePlayersBG;
            freezePlayersButton.onClick.AddListener(() =>
            {
                ws.Send($"{{\"data\":\"freeze\", \"id\":\"{playerId}\", \"freeze\":{(!frozen).ToString().ToLower()}}}");
            });

            GameObject freezePlayersTextOb = new GameObject("Freeze Players");
            freezePlayersTextOb.transform.SetParent(freezePlayersOb.transform);
            freezePlayersTextOb.transform.localPosition = Vector3.zero;
            Text freezePlayersText = freezePlayersTextOb.AddComponent<Text>();
            freezePlayersText.font = arial;
            freezePlayersText.fontSize = 20;
            freezePlayersText.alignment = TextAnchor.MiddleCenter;
            freezePlayersText.text = "freeze\nplayers";
            freezePlayersText.color = Color.black;
            freezePlayersText.rectTransform.sizeDelta = new Vector2(200, 50);

            if (compMode || !casterMode)
            {
                freezePlayersOb.SetActive(false);
            }
        }

        public void MakePlayerInList(Player player)
        {
            GameObject playerOB = new GameObject("Player " + player.name);
            playerOB.transform.SetParent(playerMenuContainer.transform);
            Image bg = playerOB.AddComponent<Image>();
            bg.color = new Color(1, 1, 1, 0.05f);
            bg.rectTransform.sizeDelta = new Vector2(1320, 50);

            GameObject playerNameOb = new GameObject("Name");
            playerNameOb.transform.SetParent(playerOB.transform);
            playerNameOb.transform.localPosition = new Vector3(-650, 0, 0);
            player.nameText = playerNameOb.AddComponent<Text>();
            player.nameText.rectTransform.pivot = new Vector2(0, 0.5f);
            player.nameText.rectTransform.sizeDelta = new Vector2(650, 50);
            player.nameText.text = (player.host ? "[HOST] " : "") + player.name;
            player.nameText.fontSize = 36;
            player.nameText.font = arial;
            player.nameText.lineSpacing = 0.85f;
            player.nameText.alignment = TextAnchor.MiddleLeft;
            player.nameText.horizontalOverflow = HorizontalWrapMode.Overflow;

            GameObject heightOb = new GameObject("Height");
            heightOb.transform.SetParent(playerOB.transform);
            heightOb.transform.localPosition = new Vector3(100, 0, 0);
            player.heightText = heightOb.AddComponent<Text>();
            player.heightText.rectTransform.pivot = new Vector2(0, 0.5f);
            player.heightText.rectTransform.sizeDelta = new Vector2(300, 50);
            player.heightText.text = 0 + "m";
            player.heightText.fontSize = 36;
            player.heightText.font = arial;
            player.heightText.lineSpacing = 0.85f;
            player.heightText.alignment = TextAnchor.MiddleLeft;

            GameObject pingOb = new GameObject("Ping");
            pingOb.transform.SetParent(playerOB.transform);
            pingOb.transform.localPosition = new Vector3(300, 0, 0);
            player.pingText = pingOb.AddComponent<Text>();
            player.pingText.rectTransform.sizeDelta = new Vector2(120, 50);
            player.pingText.text = player.ping + "ms";
            player.pingText.fontSize = 36;
            player.pingText.font = arial;
            player.pingText.alignment = TextAnchor.MiddleCenter;

            GameObject banButtonOb = new GameObject("Ban Button");
            banButtonOb.transform.SetParent(playerOB.transform);
            banButtonOb.transform.localPosition = new Vector3(605, 0, 0);
            UnityEngine.UI.Button banButton = banButtonOb.AddComponent<UnityEngine.UI.Button>();
            Image banBG = banButtonOb.AddComponent<Image>();
            banBG.color = new Color(1, 1, 1, 0.3f);
            banBG.rectTransform.sizeDelta = new Vector2(100, 44);
            banButton.image = banBG;

            GameObject banTextOb = new GameObject("banText");
            banTextOb.transform.SetParent(banButtonOb.transform);
            banTextOb.transform.localPosition = Vector3.zero;
            Text banText = banTextOb.AddComponent<Text>();
            banText.font = arial;
            banText.fontSize = 40;
            banText.alignment = TextAnchor.MiddleCenter;
            banText.text = "Ban";
            banText.rectTransform.sizeDelta = new Vector2(100, 44);

            banButton.onClick.AddListener(() =>
            {
                ws.Send($"{{\"data\":\"banPlayer\", \"id\":\"{playerId}\", \"ban\":\"{player.id}\"}}");
            });
            banButtonOb.SetActive(amHost);

            GameObject switchButtonOb = new GameObject("Switch Button");
            switchButtonOb.transform.SetParent(playerOB.transform);
            switchButtonOb.transform.localPosition = new Vector3(505, 0, 0);
            UnityEngine.UI.Button switchButton = switchButtonOb.AddComponent<UnityEngine.UI.Button>();
            Image switchBG = switchButtonOb.AddComponent<Image>();
            switchBG.color = new Color(1, 1, 1, 0.3f);
            switchBG.rectTransform.sizeDelta = new Vector2(100, 44);
            switchButton.image = switchBG;

            GameObject switchTextOb = new GameObject("banText");
            switchTextOb.transform.SetParent(switchButtonOb.transform);
            switchTextOb.transform.localPosition = Vector3.zero;
            Text switchText = switchTextOb.AddComponent<Text>();
            switchText.font = arial;
            switchText.fontSize = 20;
            switchText.alignment = TextAnchor.MiddleCenter;
            switchText.text = "make\nhost";
            switchText.rectTransform.sizeDelta = new Vector2(100, 44);

            switchButton.onClick.AddListener(() =>
            {
                ws.Send($"{{\"data\":\"switchHost\", \"id\":\"{playerId}\", \"newHost\":\"{player.id}\"}}");
            });
            switchButtonOb.SetActive(amHost);

            playerOB.transform.localScale = roomMenu.transform.localScale;
            playerListingLookup.Add(player.id, playerOB);
        }

        public void MakePlayerSettingsMenu()
        {
            LoggerInstance.Msg("Making Player Settings Menu");

            playerSettingsMenu = new GameObject("background");
            playerSettingsMenu.transform.SetParent(multiplayerMenuObject.transform);
            Image bgIm = playerSettingsMenu.AddComponent<Image>();
            bgIm.rectTransform.sizeDelta = new Vector2(1820, 780);
            bgIm.rectTransform.anchoredPosition = new Vector2(0, 100);
            bgIm.color = new Color(0, 0, 0, 0.85f);

            GameObject playerColorOb = new GameObject("Player Color");
            playerColorOb.transform.SetParent(playerSettingsMenu.transform);
            playerColorOb.transform.localPosition = new Vector3(-650, 0, 0);
            playerColorPreview = playerColorOb.AddComponent<Image>();
            playerColorPreview.color = playerColor;
            playerColorPreview.rectTransform.sizeDelta = new Vector2(400, 300);

            GameObject sliderSteal = GameObject.Find("Canvas").transform.Find("InGameMenu/InGameMenuObj_DisableMe/Options_pg_DisableMe/holder/mainanchor/main_options/MasterLevelsSlider").gameObject;
            LoggerInstance.Msg(sliderSteal);

            GameObject colorRObject = GameObject.Instantiate(sliderSteal);
            colorRObject.transform.SetParent(playerColorOb.transform);
            colorRObject.transform.localPosition = new Vector3(0, 112.5f, 0);
            colorRObject.transform.GetChild(colorRObject.transform.childCount - 1).GetComponent<Text>().text = "R";
            Slider colorRSlider = colorRObject.GetComponent<Slider>();
            colorRSlider.minValue = 0f;
            colorRSlider.maxValue = 1f;
            colorRSlider.value = 1f;
            colorRSlider.onValueChanged = new Slider.SliderEvent();
            colorRSlider.onValueChanged.AddListener((float value) =>
            {
                playerColor.r = value;
                playerColorPreview.color = playerColor;
            });

            GameObject colorGObject = GameObject.Instantiate(sliderSteal);
            colorGObject.transform.SetParent(playerColorOb.transform);
            colorGObject.transform.localPosition = new Vector3(0, 37.5f, 0);
            colorGObject.transform.GetChild(colorRObject.transform.childCount - 1).GetComponent<Text>().text = "G";
            Slider colorGSlider = colorGObject.GetComponent<Slider>();
            colorGSlider.minValue = 0f;
            colorGSlider.maxValue = 1f;
            colorGSlider.value = 1f;
            colorGSlider.onValueChanged = new Slider.SliderEvent();
            colorGSlider.onValueChanged.AddListener((float value) =>
            {
                playerColor.g = value;
                playerColorPreview.color = playerColor;
            });

            GameObject colorBObject = GameObject.Instantiate(sliderSteal);
            colorBObject.transform.SetParent(playerColorOb.transform);
            colorBObject.transform.localPosition = new Vector3(0, -37.5f, 0);
            colorBObject.transform.GetChild(colorRObject.transform.childCount - 1).GetComponent<Text>().text = "B";
            Slider colorBSlider = colorBObject.GetComponent<Slider>();
            colorBSlider.minValue = 0f;
            colorBSlider.maxValue = 1f;
            colorBSlider.value = 1f;
            colorBSlider.onValueChanged = new Slider.SliderEvent();
            colorBSlider.onValueChanged.AddListener((float value) =>
            {
                playerColor.b = value;
                playerColorPreview.color = playerColor;
            });

            GameObject colorAObject = GameObject.Instantiate(sliderSteal);
            colorAObject.transform.SetParent(playerColorOb.transform);
            colorAObject.transform.localPosition = new Vector3(0, -112.5f, 0);
            colorAObject.transform.GetChild(colorRObject.transform.childCount - 1).GetComponent<Text>().text = "A";
            Slider colorASlider = colorAObject.GetComponent<Slider>();
            colorASlider.minValue = 0f;
            colorASlider.maxValue = 1f;
            colorASlider.value = 1f;
            colorASlider.onValueChanged = new Slider.SliderEvent();
            colorASlider.onValueChanged.AddListener((float value) =>
            {
                playerColor.a = value;
                playerColorPreview.color = playerColor;
            });

            // CRAMPONS
            GameObject CramponsHeaderOb = new GameObject("Crampons Header");
            CramponsHeaderOb.transform.SetParent(playerSettingsMenu.transform);
            CramponsHeaderOb.transform.localPosition = new Vector3(100, 225, 0);
            Text Crampons = CramponsHeaderOb.AddComponent<Text>();
            Crampons.font = arial;
            Crampons.fontSize = 40;
            Crampons.alignment = TextAnchor.MiddleCenter;
            Crampons.text = "Crampons";
            Crampons.color = Color.white;
            Crampons.rectTransform.sizeDelta = new Vector2(200, 50);

            GameObject noCramponsButtonOb = new GameObject("No Crampons Button");
            noCramponsButtonOb.transform.SetParent(playerSettingsMenu.transform);
            noCramponsButtonOb.transform.localPosition = new Vector3(100, 150, 0);
            UnityEngine.UI.Button noCramponsButton = noCramponsButtonOb.AddComponent<UnityEngine.UI.Button>();
            Image noCramponsImage = noCramponsButtonOb.AddComponent<Image>();
            noCramponsImage.color = new Color(0.9f, 0.9f, 0.9f, 1);
            noCramponsImage.rectTransform.sizeDelta = new Vector2(200, 50);
            noCramponsButton.image = noCramponsImage;

            GameObject noCramponsOb = new GameObject("No Crampons");
            noCramponsOb.transform.SetParent(noCramponsButtonOb.transform);
            Text noCrampons = noCramponsOb.AddComponent<Text>();
            noCrampons.font = arial;
            noCrampons.fontSize = 40;
            noCrampons.alignment = TextAnchor.MiddleCenter;
            noCrampons.text = "No";
            noCrampons.color = Color.black;
            noCrampons.rectTransform.sizeDelta = new Vector2(200, 50);
            noCramponsOb.transform.localPosition = new Vector3(0, 0, 0);

            GameObject CramponsButton6Ob = new GameObject("6pt crampons Button");
            CramponsButton6Ob.transform.SetParent(playerSettingsMenu.transform);
            CramponsButton6Ob.transform.localPosition = new Vector3(100, 75, 0);
            UnityEngine.UI.Button CramponsButton6 = CramponsButton6Ob.AddComponent<UnityEngine.UI.Button>();
            Image CramponsImage6 = CramponsButton6Ob.AddComponent<Image>();
            CramponsImage6.color = new Color(0.9f, 0.9f, 0.9f, 1);
            CramponsImage6.rectTransform.sizeDelta = new Vector2(200, 50);
            CramponsButton6.image = CramponsImage6;

            GameObject Crampons6Ob = new GameObject("6pt crampons");
            Crampons6Ob.transform.SetParent(CramponsButton6Ob.transform);
            Text Crampons6 = Crampons6Ob.AddComponent<Text>();
            Crampons6.font = arial;
            Crampons6.fontSize = 40;
            Crampons6.alignment = TextAnchor.MiddleCenter;
            Crampons6.text = "6pt";
            Crampons6.color = Color.black;
            Crampons6.rectTransform.sizeDelta = new Vector2(200, 50);
            Crampons6Ob.transform.localPosition = new Vector3(0, 0, 0);

            GameObject CramponsButton10Ob = new GameObject("10pt crampons Button");
            CramponsButton10Ob.transform.SetParent(playerSettingsMenu.transform);
            CramponsButton10Ob.transform.localPosition = new Vector3(100, 0, 0);
            UnityEngine.UI.Button CramponsButton10 = CramponsButton10Ob.AddComponent<UnityEngine.UI.Button>();
            Image CramponsImage10 = CramponsButton10Ob.AddComponent<Image>();
            CramponsImage10.color = new Color(0.9f, 0.9f, 0.9f, 1);
            CramponsImage10.rectTransform.sizeDelta = new Vector2(200, 50);
            CramponsButton10.image = CramponsImage10;

            GameObject Crampons10Ob = new GameObject("10pt crampons");
            Crampons10Ob.transform.SetParent(CramponsButton10Ob.transform);
            Text Crampons10 = Crampons10Ob.AddComponent<Text>();
            Crampons10.font = arial;
            Crampons10.fontSize = 40;
            Crampons10.alignment = TextAnchor.MiddleCenter;
            Crampons10.text = "10pt";
            Crampons10.color = Color.black;
            Crampons10.rectTransform.sizeDelta = new Vector2(200, 50);
            Crampons10Ob.transform.localPosition = new Vector3(0, 0, 0);


            noCramponsButton.onClick.AddListener(() =>
            {
                GameManager.control.crampons = false;
                cramponsB = false;

                noCramponsImage.color = new Color(0.9f, 0.9f, 0.9f, 1);
                CramponsImage6.color = new Color(0.5f, 0.5f, 0.5f, 1);
                CramponsImage10.color = new Color(0.5f, 0.5f, 0.5f, 1);

                UpdateName(cacheName);
            });

            CramponsButton6.onClick.AddListener(() =>
            {
                GameManager.control.crampons = true;
                GameManager.control.cramponsUpgrade = false;

                cramponsB = true;
                cramponUpgradeB = false;

                noCramponsImage.color = new Color(0.5f, 0.5f, 0.5f, 1);
                CramponsImage6.color = new Color(0.9f, 0.9f, 0.9f, 1);
                CramponsImage10.color = new Color(0.5f, 0.5f, 0.5f, 1);

                UpdateName(cacheName);
            });

            CramponsButton10.onClick.AddListener(() =>
            {
                GameManager.control.crampons = true;
                GameManager.control.cramponsUpgrade = true;

                cramponsB = true;
                cramponUpgradeB = true;

                noCramponsImage.color = new Color(0.5f, 0.5f, 0.5f, 1);
                CramponsImage6.color = new Color(0.5f, 0.5f, 0.5f, 1);
                CramponsImage10.color = new Color(0.9f, 0.9f, 0.9f, 1);

                UpdateName(cacheName);
            });

            if (!GameManager.control.crampons)
            {
                noCramponsImage.color = new Color(0.9f, 0.9f, 0.9f, 1);
                CramponsImage6.color = new Color(0.5f, 0.5f, 0.5f, 1);
                CramponsImage10.color = new Color(0.5f, 0.5f, 0.5f, 1);
            } else if (GameManager.control.cramponsUpgrade)
            {
                noCramponsImage.color = new Color(0.5f, 0.5f, 0.5f, 1);
                CramponsImage6.color = new Color(0.5f, 0.5f, 0.5f, 1);
                CramponsImage10.color = new Color(0.9f, 0.9f, 0.9f, 1);
            } else
            {
                noCramponsImage.color = new Color(0.5f, 0.5f, 0.5f, 1);
                CramponsImage6.color = new Color(0.9f, 0.9f, 0.9f, 1);
                CramponsImage10.color = new Color(0.5f, 0.5f, 0.5f, 1);
            }

            // COFFEE
            GameObject CoffeeHeaderOb = new GameObject("Coffee Header");
            CoffeeHeaderOb.transform.SetParent(playerSettingsMenu.transform);
            CoffeeHeaderOb.transform.localPosition = new Vector3(325, 225, 0);
            Text Coffee = CoffeeHeaderOb.AddComponent<Text>();
            Coffee.font = arial;
            Coffee.fontSize = 40;
            Coffee.alignment = TextAnchor.MiddleCenter;
            Coffee.text = "Coffee";
            Coffee.color = Color.white;
            Coffee.rectTransform.sizeDelta = new Vector2(200, 50);

            GameObject noCoffeeButtonOb = new GameObject("No Coffee Button");
            noCoffeeButtonOb.transform.SetParent(playerSettingsMenu.transform);
            noCoffeeButtonOb.transform.localPosition = new Vector3(325, 150, 0);
            UnityEngine.UI.Button noCoffeeButton = noCoffeeButtonOb.AddComponent<UnityEngine.UI.Button>();
            Image noCoffeeImage = noCoffeeButtonOb.AddComponent<Image>();
            noCoffeeImage.color = new Color(0.9f, 0.9f, 0.9f, 1);
            noCoffeeImage.rectTransform.sizeDelta = new Vector2(200, 50);
            noCoffeeButton.image = noCoffeeImage;

            GameObject noCoffeeOb = new GameObject("No coffee");
            noCoffeeOb.transform.SetParent(noCoffeeButtonOb.transform);
            Text noCoffee = noCoffeeOb.AddComponent<Text>();
            noCoffee.font = arial;
            noCoffee.fontSize = 40;
            noCoffee.alignment = TextAnchor.MiddleCenter;
            noCoffee.text = "No";
            noCoffee.color = Color.black;
            noCoffee.rectTransform.sizeDelta = new Vector2(200, 50);
            noCoffeeOb.transform.localPosition = new Vector3(0, 0, 0);

            GameObject CoffeeOb = new GameObject("Coffee Button");
            CoffeeOb.transform.SetParent(playerSettingsMenu.transform);
            CoffeeOb.transform.localPosition = new Vector3(325, 75, 0);
            UnityEngine.UI.Button CoffeeButton = CoffeeOb.AddComponent<UnityEngine.UI.Button>();
            Image CoffeeImage = CoffeeOb.AddComponent<Image>();
            CoffeeImage.color = new Color(0.9f, 0.9f, 0.9f, 1);
            CoffeeImage.rectTransform.sizeDelta = new Vector2(200, 50);
            CoffeeButton.image = CoffeeImage;

            GameObject CoffeeTextOb = new GameObject("Coffee");
            CoffeeTextOb.transform.SetParent(CoffeeOb.transform);
            Text CoffeeText = CoffeeTextOb.AddComponent<Text>();
            CoffeeText.font = arial;
            CoffeeText.fontSize = 40;
            CoffeeText.alignment = TextAnchor.MiddleCenter;
            CoffeeText.text = "Yes";
            CoffeeText.color = Color.black;
            CoffeeText.rectTransform.sizeDelta = new Vector2(200, 50);
            CoffeeTextOb.transform.localPosition = new Vector3(0, 0, 0);


            noCoffeeButton.onClick.AddListener(() =>
            {
                GameManager.control.coffee = false;

                coffeeB = false;

                noCoffeeImage.color = new Color(0.9f, 0.9f, 0.9f, 1);
                CoffeeImage.color = new Color(0.5f, 0.5f, 0.5f, 1);

                UpdateName(cacheName);
            });

            CoffeeButton.onClick.AddListener(() =>
            {
                GameManager.control.coffee = true;

                coffeeB = true;

                noCoffeeImage.color = new Color(0.5f, 0.5f, 0.5f, 1);
                CoffeeImage.color = new Color(0.9f, 0.9f, 0.9f, 1);

                UpdateName(cacheName);
            });

            if (GameManager.control.coffee)
            {
                noCoffeeImage.color = new Color(0.5f, 0.5f, 0.5f, 1);
                CoffeeImage.color = new Color(0.9f, 0.9f, 0.9f, 1);
            } else
            {
                noCoffeeImage.color = new Color(0.9f, 0.9f, 0.9f, 1);
                CoffeeImage.color = new Color(0.5f, 0.5f, 0.5f, 1);
            }

            // PIPE
            GameObject PipeHeaderOb = new GameObject("Pipe Header");
            PipeHeaderOb.transform.SetParent(playerSettingsMenu.transform);
            PipeHeaderOb.transform.localPosition = new Vector3(550, 225, 0);
            Text Pipe = PipeHeaderOb.AddComponent<Text>();
            Pipe.font = arial;
            Pipe.fontSize = 40;
            Pipe.alignment = TextAnchor.MiddleCenter;
            Pipe.text = "Pipe";
            Pipe.color = Color.white;
            Pipe.rectTransform.sizeDelta = new Vector2(200, 50);

            GameObject noPipeButtonOb = new GameObject("No Pipe Button");
            noPipeButtonOb.transform.SetParent(playerSettingsMenu.transform);
            noPipeButtonOb.transform.localPosition = new Vector3(550, 150, 0);
            UnityEngine.UI.Button noPipeButton = noPipeButtonOb.AddComponent<UnityEngine.UI.Button>();
            Image noPipeImage = noPipeButtonOb.AddComponent<Image>();
            noPipeImage.color = new Color(0.9f, 0.9f, 0.9f, 1);
            noPipeImage.rectTransform.sizeDelta = new Vector2(200, 50);
            noPipeButton.image = noPipeImage;

            GameObject noPipeOb = new GameObject("No Pipe");
            noPipeOb.transform.SetParent(noPipeButtonOb.transform);
            Text noPipe = noPipeOb.AddComponent<Text>();
            noPipe.font = arial;
            noPipe.fontSize = 40;
            noPipe.alignment = TextAnchor.MiddleCenter;
            noPipe.text = "No";
            noPipe.color = Color.black;
            noPipe.rectTransform.sizeDelta = new Vector2(200, 50);
            noPipeOb.transform.localPosition = new Vector3(0, 0, 0);

            GameObject PipeOb = new GameObject("Pipe Button");
            PipeOb.transform.SetParent(playerSettingsMenu.transform);
            PipeOb.transform.localPosition = new Vector3(550, 75, 0);
            UnityEngine.UI.Button PipeButton = PipeOb.AddComponent<UnityEngine.UI.Button>();
            Image PipeImage = PipeOb.AddComponent<Image>();
            PipeImage.color = new Color(0.9f, 0.9f, 0.9f, 1);
            PipeImage.rectTransform.sizeDelta = new Vector2(200, 50);
            PipeButton.image = PipeImage;

            GameObject PipeTextOb = new GameObject("Pipe");
            PipeTextOb.transform.SetParent(PipeOb.transform);
            Text PipeText = PipeTextOb.AddComponent<Text>();
            PipeText.font = arial;
            PipeText.fontSize = 40;
            PipeText.alignment = TextAnchor.MiddleCenter;
            PipeText.text = "Yes";
            PipeText.color = Color.black;
            PipeText.rectTransform.sizeDelta = new Vector2(200, 50);
            PipeTextOb.transform.localPosition = new Vector3(0, 0, 0);


            noPipeButton.onClick.AddListener(() =>
            {
                GameManager.control.isUsingPipe = false;

                pipeB = false;

                noPipeImage.color = new Color(0.9f, 0.9f, 0.9f, 1);
                PipeImage.color = new Color(0.5f, 0.5f, 0.5f, 1);

                UpdateName(cacheName);
            });

            PipeButton.onClick.AddListener(() =>
            {
                GameManager.control.isUsingPipe = true;

                pipeB = true;

                noPipeImage.color = new Color(0.5f, 0.5f, 0.5f, 1);
                PipeImage.color = new Color(0.9f, 0.9f, 0.9f, 1);

                UpdateName(cacheName);
            });

            if (GameManager.control.isUsingPipe)
            {
                noPipeImage.color = new Color(0.5f, 0.5f, 0.5f, 1);
                PipeImage.color = new Color(0.9f, 0.9f, 0.9f, 1);
            }
            else
            {
                noPipeImage.color = new Color(0.9f, 0.9f, 0.9f, 1);
                PipeImage.color = new Color(0.5f, 0.5f, 0.5f, 1);
            }


            // ICE AXES
            GameObject IceAxesHeaderOb = new GameObject("IceAxes Header");
            IceAxesHeaderOb.transform.SetParent(playerSettingsMenu.transform);
            IceAxesHeaderOb.transform.localPosition = new Vector3(775, 225, 0);
            Text IceAxes = IceAxesHeaderOb.AddComponent<Text>();
            IceAxes.font = arial;
            IceAxes.fontSize = 40;
            IceAxes.alignment = TextAnchor.MiddleCenter;
            IceAxes.text = "Ice Axes";
            IceAxes.color = Color.white;
            IceAxes.rectTransform.sizeDelta = new Vector2(200, 50);

            GameObject noIceAxesButtonOb = new GameObject("No IceAxes Button");
            noIceAxesButtonOb.transform.SetParent(playerSettingsMenu.transform);
            noIceAxesButtonOb.transform.localPosition = new Vector3(775, 150, 0);
            UnityEngine.UI.Button noIceAxesButton = noIceAxesButtonOb.AddComponent<UnityEngine.UI.Button>();
            Image noIceAxesImage = noIceAxesButtonOb.AddComponent<Image>();
            noIceAxesImage.color = new Color(0.9f, 0.9f, 0.9f, 1);
            noIceAxesImage.rectTransform.sizeDelta = new Vector2(200, 50);
            noIceAxesButton.image = noIceAxesImage;

            GameObject noIceAxesOb = new GameObject("No IceAxes");
            noIceAxesOb.transform.SetParent(noIceAxesButtonOb.transform);
            Text noIceAxes = noIceAxesOb.AddComponent<Text>();
            noIceAxes.font = arial;
            noIceAxes.fontSize = 40;
            noIceAxes.alignment = TextAnchor.MiddleCenter;
            noIceAxes.text = "No";
            noIceAxes.color = Color.black;
            noIceAxes.rectTransform.sizeDelta = new Vector2(200, 50);
            noIceAxesOb.transform.localPosition = new Vector3(0, 0, 0);

            GameObject IceAxesOb = new GameObject("IceAxes Button");
            IceAxesOb.transform.SetParent(playerSettingsMenu.transform);
            IceAxesOb.transform.localPosition = new Vector3(775, 75, 0);
            UnityEngine.UI.Button IceAxesButton = IceAxesOb.AddComponent<UnityEngine.UI.Button>();
            Image IceAxesImage = IceAxesOb.AddComponent<Image>();
            IceAxesImage.color = new Color(0.9f, 0.9f, 0.9f, 1);
            IceAxesImage.rectTransform.sizeDelta = new Vector2(200, 50);
            IceAxesButton.image = IceAxesImage;

            GameObject IceAxesTextOb = new GameObject("IceAxes");
            IceAxesTextOb.transform.SetParent(IceAxesOb.transform);
            Text IceAxesText = IceAxesTextOb.AddComponent<Text>();
            IceAxesText.font = arial;
            IceAxesText.fontSize = 40;
            IceAxesText.alignment = TextAnchor.MiddleCenter;
            IceAxesText.text = "Yes";
            IceAxesText.color = Color.black;
            IceAxesText.rectTransform.sizeDelta = new Vector2(200, 50);
            IceAxesTextOb.transform.localPosition = new Vector3(0, 0, 0);


            noIceAxesButton.onClick.AddListener(() =>
            {
                GameManager.control.iceAxes = false;

                axesB = false;

                noIceAxesImage.color = new Color(0.9f, 0.9f, 0.9f, 1);
                IceAxesImage.color = new Color(0.5f, 0.5f, 0.5f, 1);

                UpdateName(cacheName);
            });

            IceAxesButton.onClick.AddListener(() =>
            {
                GameManager.control.iceAxes = true;

                axesB = true;

                noIceAxesImage.color = new Color(0.5f, 0.5f, 0.5f, 1);
                IceAxesImage.color = new Color(0.9f, 0.9f, 0.9f, 1);

                UpdateName(cacheName);
            });

            if (GameManager.control.iceAxes)
            {
                noIceAxesImage.color = new Color(0.5f, 0.5f, 0.5f, 1);
                IceAxesImage.color = new Color(0.9f, 0.9f, 0.9f, 1);
            }
            else
            {
                noIceAxesImage.color = new Color(0.9f, 0.9f, 0.9f, 1);
                IceAxesImage.color = new Color(0.5f, 0.5f, 0.5f, 1);
            }

            if (!compMode)
            {
                noCramponsButton.interactable = false;
                CramponsButton6.interactable = false;
                CramponsButton10.interactable = false;

                CoffeeButton.interactable = false;
                noCramponsButton.interactable = false;

                PipeButton.interactable = false;
                noPipeButton.interactable = false;

                IceAxesButton.interactable = false;
                noIceAxesButton.interactable = false;

                GameObject CoverOb = new GameObject("Cover");
                CoverOb.transform.SetParent(playerSettingsMenu.transform);
                CoverOb.transform.localPosition = new Vector3(437.5f, 75, 0);
                Image CoverImage = CoverOb.AddComponent<Image>();
                CoverImage.color = new Color(0, 0, 0, 0.85f);
                CoverImage.rectTransform.sizeDelta = new Vector2(900, 500);

                GameObject CoverTextOb = new GameObject("Cover Text");
                CoverTextOb.transform.SetParent(CoverOb.transform);
                Text CoverText = CoverTextOb.AddComponent<Text>();
                CoverText.font = arial;
                CoverText.fontSize = 64;
                CoverText.alignment = TextAnchor.MiddleCenter;
                CoverText.text = "Only available in Comp mode!";
                CoverText.color = Color.red;
                CoverText.rectTransform.sizeDelta = new Vector2(1000, 600);
                CoverTextOb.transform.localPosition = new Vector3(0, 0, 0);
            }
        }


        public override async void OnFixedUpdate()
        {
            //LoggerInstance.Msg(amHost);

            if (makingShadowPrefab) return;

            if (SteamManager.Initialized && ws == null)
            {
                Connect();
            }

            if (wasAlive && lastPing + 15000 < DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() && !freshBoot)
            {
                MakeInfoText("The server is not responding! Give it a sec to restart or something", Color.red);
                wasAlive = false;
                connected = false;
            }

            if (!wasAlive && lastPing + (inRoom ? 5000 : 15000) < DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() && !freshBoot)
            {
                if (reconnectDelay + 1000 < DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
                {
                    reconnectDelay = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    ws.ConnectAsync();
                    LoggerInstance.Msg("reconnecting");
                }
            }

            if (!wasAlive && lastPing + 5000 < DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() && freshBoot)
            {
                if (reconnectDelay + 1000 < DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
                {
                    reconnectDelay = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    ws.ConnectAsync();
                }
            }

            if (SceneManager.GetActiveScene().name != mePlayerPlayer.scene && inRoom)
            {
                LoggerInstance.Msg("Fixing my scene state");
                OnSceneWasLoaded(SceneManager.GetActiveScene().buildIndex, SceneManager.GetActiveScene().name);
            }

            if (debugSpin && mePlayer != null)
            {
                debugSpinTimer += Time.deltaTime * 2f;
                debugSpinTimer = debugSpinTimer % (2 * Mathf.PI);

                Vector3 spinPos = debugSpinPos;
                spinPos.x += Mathf.Sin(debugSpinTimer);
                spinPos.z += Mathf.Cos(debugSpinTimer);

                mePlayer.transform.position = spinPos;
            }

            if (!connected || lastPing + (inRoom ? 5000 : 15000) < DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()) return;

            if (playerColorLast != playerColor)
            {
                playerColorLast = playerColor;
                ws.Send($"{{\"data\":\"changeColor\", \"id\":\"{playerId}\", \"color\":[\"{playerColor.r}\", \"{playerColor.g}\", \"{playerColor.b}\", \"{playerColor.a}\"]}}");
            }

            /*if (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() > lastPing + 1000)
            {
                lastPing = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                ws.Send($"{{\"data\":\"ping\", \"id\":\"{playerId}\", \"ping\":{lastPing}}}");
            }*/

            /*for (int i = shadowPrefabs.Count; i < 10; i++)
            {
                shadowPrefabs.Add(GameObject.Instantiate(shadowPrefab));
                GameObject.Destroy(shadowPrefabs[i].GetComponent<PlayerShadow>());
                GameObject.DontDestroyOnLoad(shadowPrefabs[i]);
                shadowPrefabs[i].transform.position = new Vector3(0, -1000000, 0);
            }*/

            for (int i = 0; i < shadowPrefabRequests.Count; i++)
            {
                shadowPrefabRequests[i].player = GameObject.Instantiate(shadowPrefab);
                GameObject.Destroy(shadowPrefabRequests[i].player.GetComponent<PlayerShadow>());
                GameObject.DontDestroyOnLoad(shadowPrefabRequests[i].player);
                shadowPrefabRequests[i].player.transform.position = new Vector3(0, -1000000, 0);
                shadowPrefabRequests[i].MakeBody();
            }
            shadowPrefabRequests.Clear();

            GetDependencies();

            if (pipeObject != null)
            {
                pipeObject.SetActive(GameManager.control.isUsingPipe && GameManager.control.smokingpipe);
            }

            if (inRoom && mePlayer != null && shadow != null && meClimbing != null && barometer != null)
            {
                //meFoots.legHolder.localPosition = new Vector3(0, meFoots.legHolderDefaultPos.localPosition.y, 0);


                float height = barometer.currentMetresUp;
                mePlayerPlayer.heightText.text = height.ToString("0.0#") + "m";

                string updateString = $"{{\"data\":\"updatePosition\", \"id\":\"{playerId}\", \"update\":\"" +
                    $"{{\\\"data\\\": \\\"updatePlayerPosition\\\", \\\"id\\\":{playerId}, " +
                    $"\\\"height\\\":\\\"{height.ToString("0.0#")}\\\", " +
                    $"\\\"position\\\":[\\\"{shadow.transform.position.x.ToString("0.0#")}\\\",\\\"{shadow.transform.position.y.ToString("0.0#")}\\\",\\\"{shadow.transform.position.z.ToString("0.0#")}\\\"], " +
                    $"\\\"handL\\\":[\\\"{shadow.handIK_L.solver.arm.target.position.x.ToString("0.0#")}\\\",\\\"{shadow.handIK_L.solver.arm.target.position.y.ToString("0.0#")}\\\",\\\"{shadow.handIK_L.solver.arm.target.position.z.ToString("0.0#")}\\\"], " +
                    $"\\\"handR\\\":[\\\"{shadow.handIK_R.solver.arm.target.position.x.ToString("0.0#")}\\\",\\\"{shadow.handIK_R.solver.arm.target.position.y.ToString("0.0#")}\\\",\\\"{shadow.handIK_R.solver.arm.target.position.z.ToString("0.0#")}\\\"], " +
                    $"\\\"armStrechL\\\":\\\"{shadow.handIK_L.solver.arm.armLengthMlp}\\\", " +
                    $"\\\"armStrechR\\\":\\\"{shadow.handIK_R.solver.arm.armLengthMlp}\\\", " +
                    $"\\\"footL\\\":[\\\"{shadow.footIK_L.solver.target.transform.position.x.ToString("0.0#")}\\\",\\\"{shadow.footIK_L.solver.target.position.y.ToString("0.0#")}\\\",\\\"{shadow.footIK_L.solver.target.position.z.ToString("0.0#")}\\\"], " +
                    $"\\\"footR\\\":[\\\"{shadow.footIK_R.solver.target.position.x.ToString("0.0#")}\\\",\\\"{shadow.footIK_R.solver.target.position.y.ToString("0.0#")}\\\",\\\"{shadow.footIK_R.solver.target.position.z.ToString("0.0#")}\\\"], " +
                    $"\\\"footLBend\\\":[\\\"{shadow.realleftKnee.transform.position.x.ToString("0.0#")}\\\",\\\"{shadow.realleftKnee.transform.position.y.ToString("0.0#")}\\\",\\\"{shadow.realleftKnee.transform.position.z.ToString("0.0#")}\\\"], " +
                    $"\\\"footRBend\\\":[\\\"{shadow.realrightKnee.transform.position.x.ToString("0.0#")}\\\",\\\"{shadow.realrightKnee.transform.position.y.ToString("0.0#")}\\\",\\\"{shadow.realrightKnee.transform.position.z.ToString("0.0#")}\\\"], " +
                    $"\\\"rotation\\\":[\\\"{shadow.transform.rotation.x.ToString("0.0#")}\\\",\\\"{shadow.transform.rotation.y.ToString("0.0#")}\\\",\\\"{shadow.transform.rotation.z.ToString("0.0#")}\\\",\\\"{shadow.transform.rotation.w.ToString("0.0#")}\\\"], " +
                    $"\\\"handLRotation\\\":[\\\"{shadow.handIK_L.solver.arm.target.rotation.x.ToString("0.0#")}\\\",\\\"{shadow.handIK_L.solver.arm.target.rotation.y.ToString("0.0#")}\\\",\\\"{shadow.handIK_L.solver.arm.target.rotation.z.ToString("0.0#")}\\\",\\\"{shadow.handIK_L.solver.arm.target.rotation.w.ToString("0.0#")}\\\"], " +
                    $"\\\"handRRotation\\\":[\\\"{shadow.handIK_R.solver.arm.target.rotation.x.ToString("0.0#")}\\\",\\\"{shadow.handIK_R.solver.arm.target.rotation.y.ToString("0.0#")}\\\",\\\"{shadow.handIK_R.solver.arm.target.rotation.z.ToString("0.0#")}\\\",\\\"{shadow.handIK_R.solver.arm.target.rotation.w.ToString("0.0#")}\\\"], " +
                    $"\\\"footLRotation\\\":[\\\"{shadow.footIK_L.solver.target.rotation.x.ToString("0.0#")}\\\",\\\"{shadow.footIK_L.solver.target.rotation.y.ToString("0.0#")}\\\",\\\"{shadow.footIK_L.solver.target.rotation.z.ToString("0.0#")}\\\",\\\"{shadow.footIK_L.solver.target.rotation.w.ToString("0.0#")}\\\"], " +
                    $"\\\"footRRotation\\\":[\\\"{shadow.footIK_R.solver.target.rotation.x.ToString("0.0#")}\\\",\\\"{shadow.footIK_R.solver.target.rotation.y.ToString("0.0#")}\\\",\\\"{shadow.footIK_R.solver.target.rotation.z.ToString("0.0#")}\\\",\\\"{shadow.footIK_R.solver.target.rotation.w.ToString("0.0#")}\\\"]" +
                    $"}}\"}}";
                ws.SendAsync(updateString, null);
            }

            foreach (Transform tran in sceneSplitters.Values)
            {
                tran.gameObject.SetActive(false);
            }

            Transform meSplitter = sceneSplitters[mePlayerPlayer.scene];
            meSplitter.gameObject.SetActive(true);
            playerListingLookup[mePlayerPlayer.id].transform.SetSiblingIndex(meSplitter.GetSiblingIndex() + 1);
            mePlayerPlayer.nameText.text = mePlayerPlayer.name;

            foreach (Player player in playersInRoom)
            {
                Transform playerSplitter = sceneSplitters[player.scene];
                playerSplitter.gameObject.SetActive(true);
                playerListingLookup[player.id].transform.SetSiblingIndex(playerSplitter.GetSiblingIndex() + 1);

                //LoggerInstance.Msg(player.name + "'s ping: " + player.ping + "ms");
                player.pingText.text = player.ping + "ms";
                player.heightText.text = player.height.ToString("#.##") + "m";
                player.nameText.text = player.name;
                
                if (player.height == 0)
                {
                    player.heightText.text = "";
                }

                if (player.player == null) continue;

                player.handLIK.solver.arm.armLengthMlp = player.armStretchL;
                player.handRIK.solver.arm.armLengthMlp = player.armStretchR;

                if (player.armStretchL > 1)
                {
                    player.handLIK.solver.arm.armLengthMlp = player.armStretchL * 1.3f;
                }

                if (player.armStretchR > 1)
                {
                    player.handRIK.solver.arm.armLengthMlp = player.armStretchR * 1.3f;
                }

                player.player.transform.position = player.bodyPosition;
                player.handL.transform.position = player.handLPosition;
                player.handR.transform.position = player.handRPosition;
                player.footL.transform.position = player.footLPosition;
                player.footR.transform.position = player.footRPosition;
                player.footLBend.transform.position = player.footLBendPosition;
                player.footRBend.transform.position = player.footRBendPosition;

                player.player.transform.rotation = player.bodyRotation;
                player.handL.transform.rotation = player.handLRotation;
                player.handR.transform.rotation = player.handRRotation;
                player.footL.transform.rotation = player.footLRotation;
                player.footR.transform.rotation = player.footRRotation;

                player.nameBillboard.rotation = Camera.main.transform.rotation;

                foreach (TextMesh mesh in GetAllTextMeshesInChildren(player.nameBillboard.gameObject))
                {
                    mesh.text = player.name;
                }
            }

            if (SceneManager.GetActiveScene().name == "TitleScreen")
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        public override void OnUpdate()
        {
            if (Input.GetKeyDown(KeyCode.BackQuote))
            {
                Cursor.lockState = Cursor.lockState == CursorLockMode.Locked ? CursorLockMode.None : CursorLockMode.Locked;
                Cursor.visible = Cursor.lockState == CursorLockMode.None;
            }

            if (!connected) return;

            if (Input.GetKeyDown(KeyCode.Tab))
            {
                if (compMode)
                {
                    GameManager.control.crampons = cramponsB;
                    GameManager.control.cramponsUpgrade = cramponUpgradeB;
                    GameManager.control.coffee = coffeeB;
                    GameManager.control.isUsingPipe = pipeB;
                    GameManager.control.iceAxes = axesB;
                }

                multiplayerMenuObject.SetActive(true);
                if (inRoom)
                {
                    roomMenu.SetActive(!roomMenu.activeSelf);
                } else
                {
                    multiplayerMenu.SetActive(!multiplayerMenu.activeSelf);
                }

                if ((multiplayerMenu.activeSelf || roomMenu.activeSelf) && !playerSettingsMenu.activeSelf)
                {
                    mouseOnOpen = Cursor.lockState;
                } else if (InGameMenu.isCurrentlyNavigationMenu || SceneManager.GetActiveScene().name == "TitleScreen")
                {
                    mouseOnOpen = CursorLockMode.None;
                }

                playerSettingsMenu.SetActive(false);

                Cursor.lockState = (multiplayerMenu.activeSelf || roomMenu.activeSelf) ? CursorLockMode.None : mouseOnOpen;
                Cursor.visible = (multiplayerMenu.activeSelf || roomMenu.activeSelf) || mouseOnOpen == CursorLockMode.None ? true : false;
            }

            if (debugMode && Input.GetKeyDown(KeyCode.KeypadPlus))
            {
                debugSpin = !debugSpin;
                debugSpinPos = mePlayer.transform.position;
            }

            if (debugMode && Input.GetKeyDown(KeyCode.KeypadMinus))
            {
                myColor = new Color(UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f));
                ws.Send($"{{\"data\":\"changeColor\", \"id\":\"{playerId}\", \"color\":[\"{myColor.r}\", \"{myColor.g}\", \"{myColor.b}\", \"{myColor.a}\"]}}");
            }

            if (multiplayerMenu.activeSelf || roomMenu.activeSelf)
            {
                InputField newClicked = null;
                InputField atLeastOne = null;
                foreach (InputField field in inputFields)
                {
                    if (field.isFocused && !inputFieldChecks[field])
                    {
                        newClicked = field;
                        atLeastOne = field;
                    }

                    inputFieldChecks[field] = field.isFocused;
                }

                if (newClicked != null)
                {
                    foreach (InputField field in inputFields)
                    {
                        if (field != newClicked)
                        {
                            field.OnDeselect(new BaseEventData(field.GetComponent<EventSystem>()));
                        }
                    }
                }

                if (atLeastOne != null && Event.current.isKey && Event.current.type == EventType.KeyDown)
                {
                    atLeastOne.ProcessEvent(Event.current);
                    atLeastOne.ForceLabelUpdate();
                }

            }

            if (compMode && DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() > cheatEngineCheck)
            {
                cheatEngineCheck = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + 1000;

                IDictionary<IntPtr, string> windows = OpenWindowGetter.GetOpenWindows();
                for (int i = 0; i < windows.Count; i++)
                {
                    if (windows.Values.ElementAt(i).Contains("Cheat Engine"))
                    {
                        i = windows.Count + 1;
                        ws.Send($"{{\"data\":\"sendToEveryone\", \"id\":\"{playerId}\", \"type\":false, \"message\":\"HAS CHEAT ENGINE OPEN!\"}}");
                    }
                }
            }
        }

        public override async void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            if (multiplayerMenuObject == null) {
                arial = new GameObject().AddComponent<TextMesh>().font;
                MakeMenu();
                MakeRoomMenu();
                MakePlayerSettingsMenu();
                multiplayerMenu.SetActive(false);
                roomMenu.SetActive(false);
                playerSettingsMenu.SetActive(false);

                MakeInfoText("Current BWF Version: " + Info.SemanticVersion, Color.white);
                MakeInfoText("Press ` to lock/unlock Mouse", Color.white, 48);
                MakeInfoText("Press Tab to open/close menu", Color.white, 48);
            }

            if (sceneName == "TitleScreen")
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = Cursor.lockState == CursorLockMode.None;
            }

            if (sceneName == "TitleScreen" || sceneName == "Cabin")
            {
                if (mePlayerPlayer.heightText.text != null)
                {
                    mePlayerPlayer.heightText.text = "";
                }
            }

            myLastScene = sceneName;

            if (compMode)
            {
                GameManager.control.crampons = cramponsB;
                GameManager.control.cramponsUpgrade = cramponUpgradeB;
                GameManager.control.coffee = coffeeB;
                GameManager.control.isUsingPipe = pipeB;
                GameManager.control.iceAxes = axesB;
            }

            if (makingShadowPrefab) return;
            if (!connected) return;

            LoggerInstance.Msg("In scene " + sceneName);
            LoggerInstance.Msg("Shadow Prefab " + shadowPrefab);
            LoggerInstance.Msg("Players in room: " + playersInRoom.Count);

            ws.Send($"{{\"data\":\"switchScene\", \"id\":\"{playerId}\", \"scene\":\"{sceneName}\"}}");
            multiplayerMenu.SetActive(false);
            roomMenu.SetActive(false);

            GetDependencies();

            foreach (Player player in playersInRoom)
            {
                LoggerInstance.Msg("fixing " + player.name + "'s scene");
                if (mePlayerPlayer.scene == sceneName)
                {
                    player.Yeet(true);
                }
                player.UpdateVisual(sceneName);
            }

            if (casterMode)
            {
                StamperPeakSummit[] stampers = Resources.FindObjectsOfTypeAll<StamperPeakSummit>();
                
                foreach (StamperPeakSummit stamp in stampers)
                {
                    //GameObject.Destroy(stamp.gameObject);
                }
            }

            mePlayerPlayer.scene = sceneName;
        }

        [HarmonyPatch(typeof(StamperPeakSummit), "StampJournal", new Type[] {})]
        private static class StampPatch
        {
            private static void Prefix()
            {
                ws.Send($"{{\"data\":\"summit\", \"id\":\"{playerId}\", \"scene\":\"{mePlayerPlayer.scene}\"}}");
            }
        }

        [HarmonyPatch(typeof(DiscordActivityUpdater), "UpdateStatus", new Type[] { })]
        private static class DiscordPatch
        {
            private static void Postfix(DiscordActivityUpdater __instance)
            {
                Reflec reflec = new Reflec(typeof(DiscordActivityUpdater), __instance);
                __instance.discord.details = "BWF on " + ((Text)reflec.GetField("peakName")).text;
            }
        }

        static bool tempPipe = false;
        [HarmonyPatch(typeof(Pipe), "CheckLoad", new Type[] { })]
        private static class PipePatch
        {
            private static void Prefix(Pipe __instance)
            {
                tempPipe = GameManager.control.isUsingPipe;
                GameManager.control.isUsingPipe = true;
            }

            private static void Postfix(Pipe __instance)
            {
                GameManager.control.isUsingPipe = tempPipe;
                pipeObject = __instance.gameObject;
            }
        }

        public void giveInfo()
        {
            wasAlive = true;
            connected = true;
            LoggerInstance.Msg("giving identity");

            if (!debugMode)
            {
                if (SteamManager.Initialized)
                {
                    playerId = SteamUser.GetSteamID().m_SteamID;
                    playerName = SteamFriends.GetPersonaName();
                }
            } else if (!wasConnected)
            {
                playerId = (ulong)LongRandom(0, 100000000000000);
                playerName = RandomString(10) + "_DEBUG";
            }

            if (mePlayerPlayer == null)
            {
                mePlayerPlayer = new Player(playerName, playerId, SceneManager.GetActiveScene().name, amHost, this);
            }

            cacheName = playerName;

            lastPing = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            ws.Send($"{{\"data\":\"identify\", \"id\":\"{playerId}\", \"name\":\"{playerName}\", \"CoC\":{(compMode ? 1 : (casterMode ? 2 : 0))}, \"scene\":\"{SceneManager.GetActiveScene().name}\", \"ping\":{lastPing}, \"major\":{Info.SemanticVersion.Major}, \"minor\":{Info.SemanticVersion.Minor}, \"patch\":{Info.SemanticVersion.Patch}, \"wasConnected\":{wasConnected.ToString().ToLower()}}}");
            ws.Send($"{{\"data\":\"getRooms\"}}");
            ws.Send($"{{\"data\":\"changeColor\", \"id\":\"{playerId}\", \"color\":[\"{myColor.r}\", \"{myColor.g}\", \"{myColor.b}\", \"{myColor.a}\"]}}");
            UpdateName(playerName);
            wasConnected = true;
        }

        void GetDependencies()
        {
            if (mePlayer == null)
            {
                mePlayer = GameObject.Find("Player");
            }

            if (shadow == null)
            {
                PlayerShadow[] shads = Resources.FindObjectsOfTypeAll<PlayerShadow>();
                if (shads.Length != 0)
                {
                    shadow = shads[0];
                }
            }

            if (meBodyTurning == null)
            {
                BodyTurning[] bodyTurns = Resources.FindObjectsOfTypeAll<BodyTurning>();
                if (bodyTurns.Length != 0)
                {
                    meBodyTurning = bodyTurns[0];
                }
            }

            if (meClimbing == null)
            {
                Climbing[] climbings = Resources.FindObjectsOfTypeAll<Climbing>();
                if (climbings.Length != 0)
                {
                    meClimbing = climbings[0];
                }
            }

            if (meFoots == null)
            {
                Footplacement[] placements = Resources.FindObjectsOfTypeAll<Footplacement>();
                if (placements.Length != 0)
                {
                    meFoots = placements[0];
                }
            }

            if (barometer == null)
            {
                Barometer[] barometers = Resources.FindObjectsOfTypeAll<Barometer>();
                if (barometers.Length != 0)
                {
                    barometer = barometers[0];
                }
            }

            if (pipeObject == null)
            {
                Pipe[] pipes = Resources.FindObjectsOfTypeAll<Pipe>();
                if (pipes.Length != 0)
                {
                    pipeObject = pipes[0].gameObject;
                }
            }
        }

        void UpdateName(string name)
        {
            string newName = "";

            if (compMode)
            {
                newName += "[";
                newName += !GameManager.control.crampons ? 0 : (GameManager.control.cramponsUpgrade ? 10 : 6);
                newName += "|";
                newName += GameManager.control.coffee ? "Y" : "N";
                newName += "|";
                newName += GameManager.control.isUsingPipe ? "Y" : "N";
                newName += "|";
                newName += GameManager.control.iceAxes ? "Y" : "N";
                newName += "] ";
            }

            if (casterMode)
            {
                newName += "[CASTER] ";
            }

            newName += name;
            playerName = newName;
            mePlayerPlayer.name = playerName;

            ws.Send($"{{\"data\":\"updateName\", \"id\":\"{playerId}\", \"newName\":\"{playerName}\"}}");
        }

        public static void MakeInfoText(string text, Color color, int fontSize = 18)
        {
            List<string> textLines = new List<string>(0);
            string remaining = text;

            while (remaining.Length > 50)
            {
                string split = "";
                for (int i = 49; i >= 0; i--)
                {
                    if (remaining[i] == ' ')
                    {
                        split = remaining.Substring(0, i);
                        i = -1;
                    }
                }

                if (split == "")
                {
                    split = remaining.Substring(0, 50);
                    remaining = remaining.Substring(50, remaining.Length - 50);
                } else
                {
                    remaining = remaining.Substring(split.Length, remaining.Length - split.Length);
                }

                textLines.Add(split);
            }

            textLines.Add(remaining);

            foreach (string line in textLines)
            {
                GameObject infoTextOb = new GameObject("update");
                infoTextOb.transform.SetParent(updateContainer.transform);
                Text infoText = infoTextOb.AddComponent<Text>();
                infoText.raycastTarget = false;
                infoText.text = line;
                infoText.color = color;
                infoText.fontSize = fontSize;
                infoText.font = arial;
                infoText.fontStyle = FontStyle.Bold;
                infoText.alignment = TextAnchor.MiddleCenter;
                infoText.horizontalOverflow = HorizontalWrapMode.Wrap;
                infoText.verticalOverflow = VerticalWrapMode.Overflow;
                infoText.rectTransform.sizeDelta = new Vector2(1920, fontSize * 1.1f);
                infoTextOb.AddComponent<InfoCloser>();
                infoTextOb.transform.localScale = multiplayerMenu.transform.localScale;
            }
        }

        public void MakeAndSendRecovery()
        {
            string recoverString = $"{{\"data\":\"recovery\", ";
            recoverString += $"\"roomName\":\"{roomName}\", ";
            recoverString += $"\"roomPass\":\"{roomPass}\", ";
            recoverString += $"\"roomID\":{roomId}, ";
            recoverString += $"\"host\":\"{hostId}\", ";
            recoverString += $"\"id\":\"{playerId}\"}}";

            ws.SendAsync(recoverString, null);
        }

        public string[] GetAssemblyHashes()
        {
            FileStream assemblySteam = new FileStream("./Peaks of Yore_Data/Managed/Assembly-CSharp.dll", FileMode.Open);
            FileStream assembly2Steam = new FileStream("./Peaks of Yore_Data/Managed/Assembly-CSharp-firstpass.dll", FileMode.Open);

            SHA256 assemblyHash = SHA256.Create();
            byte[] assemblyHashBytes = assemblyHash.ComputeHash(assemblySteam);
            string assemblyHashString = Convert.ToBase64String(assemblyHashBytes);

            SHA256 assembly2Hash = SHA256.Create();
            byte[] assembly2HashBytes = assemblyHash.ComputeHash(assembly2Steam);
            string assembly2HashString = Convert.ToBase64String(assembly2HashBytes);

            return new string[] { assemblyHashString, assembly2HashString };
        }



        long LongRandom(long min, long max)
        {
            byte[] buf = new byte[8];
            rand.NextBytes(buf);
            long longRand = BitConverter.ToInt64(buf, 0);

            return (Math.Abs(longRand % (max - min)) + min);
        }

        public string RandomString(int size, bool lowerCase = false)
        {
            var builder = new StringBuilder(size);

            char offset = lowerCase ? 'a' : 'A';
            const int lettersOffset = 26;

            for (var i = 0; i < size; i++)
            {
                var @char = (char)rand.Next(offset, offset + lettersOffset);
                builder.Append(@char);
            }

            return lowerCase ? builder.ToString().ToLower() : builder.ToString();
        }

        InputField[] GetAllInputFieldsInChildren(GameObject gameOb)
        {
            List<InputField> components = new List<InputField>(0);

            if (gameOb.GetComponent<InputField>() != null)
            {
                components.Add(gameOb.GetComponent<InputField>());
            }

            for (int i = 0; i < gameOb.transform.childCount; i++)
            {
                components.AddRange(GetAllInputFieldsInChildren(gameOb.transform.GetChild(i).gameObject));
            }

            return components.ToArray();
        }

        TextMesh[] GetAllTextMeshesInChildren(GameObject gameOb)
        {
            List<TextMesh> components = new List<TextMesh>(0);

            if (gameOb.GetComponent<TextMesh>() != null)
            {
                components.Add(gameOb.GetComponent<TextMesh>());
            }

            for (int i = 0; i < gameOb.transform.childCount; i++)
            {
                components.AddRange(GetAllTextMeshesInChildren(gameOb.transform.GetChild(i).gameObject));
            }

            return components.ToArray();
        }

        public static SkinnedMeshRenderer[] GetAllSkinnedMeshRenderersInChildren(GameObject gameOb)
        {
            List<SkinnedMeshRenderer> components = new List<SkinnedMeshRenderer>(0);

            if (gameOb.GetComponent<SkinnedMeshRenderer>() != null)
            {
                components.Add(gameOb.GetComponent<SkinnedMeshRenderer>());
            }

            for (int i = 0; i < gameOb.transform.childCount; i++)
            {
                components.AddRange(GetAllSkinnedMeshRenderersInChildren(gameOb.transform.GetChild(i).gameObject));
            }

            return components.ToArray();
        }

        public static string Base64Decode(string base64EncodedData)
        {
            var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
            return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
        }
        public static string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }
    }
}
