using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MelonLoader;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using WebSocketSharp;
using Steamworks;
using System.Threading.Tasks;
using System.Threading;
using System.Text.Json;
using System.Web.Security;
using System.IO;
using System.Collections;

namespace Bag_With_Friends
{
    public class Multiplayer : MelonMod
    {
        bool debugMode = false;
        string server = "bwf.givo.xyz";

        System.Random rand = new System.Random();

        WebSocket ws;
        ulong playerId;
        string playerName;
        bool amHost = false;
        bool inRoom = false;
        bool connected = false;
        bool makingShadowPrefab = false;
        CursorLockMode mouseOnOpen = CursorLockMode.None;

        Font arial;

        GameObject multiplayerMenuObject;
        Canvas multiplayerMenuCanvas;
        GameObject multiplayerMenu;
        GraphicRaycaster multiplayerRaycaster;
        GameObject roomContainer;

        GameObject updateContainer;
        Image roomBack;

        AssetBundle UIBundle;
        System.Reflection.Assembly thisAssembly;

        List<InputField> inputFields = new List<InputField>(0);
        Dictionary<InputField, bool> inputFieldChecks = new Dictionary<InputField, bool>(0);

        List<Player> playersInRoom = new List<Player>(0);
        Dictionary<long, Player> playerLookup = new Dictionary<long, Player>(0);
        public GameObject playerContainer;

        GameObject mePlayer;
        BodyTurning meBodyTurning;
        Climbing meClimbing;
        Footplacement meFoots;
        PlayerShadow shadow;
        public GameObject shadowPrefab;
        //public List<GameObject> shadowPrefabs = new List<GameObject>(0);
        public List<Player> shadowPrefabRequests = new List<Player>(0);

        public void Connect()
        {
            //ws = new WebSocket("ws://172.234.207.93:3000");
            ws = new WebSocket($"ws://{server}:3000");

            ws.OnMessage += (sender, e) =>
            {
                JsonDocument doc = JsonDocument.Parse(e.Data);
                JsonElement res = doc.RootElement;

                if (res.GetProperty("data").GetString() != "updatePlayerPosition" && debugMode)
                {
                    LoggerInstance.Msg("got message " + res.GetProperty("data").GetString());
                }

                switch (res.GetProperty("data").GetString())
                {
                    case "identify":
                        giveInfo();
                        break;

                    case "error":
                    case "info":
                        GameObject infoTextOb = new GameObject(res.GetProperty("data").GetString());
                        infoTextOb.transform.SetParent(updateContainer.transform);
                        Text infoText = infoTextOb.AddComponent<Text>();
                        infoText.text = res.GetProperty("info").GetString();
                        infoText.color = res.GetProperty("data").GetString() == "error" ? new Color(1, 0, 0, 1) : new Color(1, 1, 1, 1);
                        infoText.fontSize = 18;
                        infoText.font = arial;
                        infoText.fontStyle = FontStyle.Bold;
                        infoText.alignment = TextAnchor.MiddleCenter;
                        infoText.horizontalOverflow = HorizontalWrapMode.Overflow;
                        infoText.rectTransform.sizeDelta = new Vector2(0, 20);
                        infoTextOb.AddComponent<InfoCloser>();
                        infoTextOb.transform.localScale = multiplayerMenu.transform.localScale;
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
                            makeRoomListing(room.GetProperty("name").GetString(), room.GetProperty("id").GetInt16(), room.GetProperty("pass").GetBoolean(), room.GetProperty("players").GetInt16(), room.GetProperty("host").GetString());
                        }
                        break;

                    case "inRoom":
                        inRoom = res.GetProperty("inRoom").GetBoolean();
                        break;

                    case "addPlayer":
                        JsonElement recievedPlayer = res.GetProperty("player").EnumerateArray().ElementAt(0);
                        Player playerToAdd = new Player(recievedPlayer.GetProperty("name").GetString(), recievedPlayer.GetProperty("id").GetInt64(), recievedPlayer.GetProperty("scene").GetString(), recievedPlayer.GetProperty("host").GetBoolean(), this);
                        playersInRoom.Add(playerToAdd);
                        playerLookup.Add(playerToAdd.id, playerToAdd);
                        playerToAdd.ChangeScene(recievedPlayer.GetProperty("scene").GetString());

                        GameObject gam = new GameObject("TestOB");
                        GameObject.DontDestroyOnLoad(gam);
                        ThingTester tes = gam.AddComponent<ThingTester>();
                        tes.player = playerToAdd;
                        break;

                    case "removePlayer":
                        Player playerToRemove = playerLookup[res.GetProperty("id").GetInt64()];
                        playersInRoom.Remove(playerToRemove);
                        playerLookup.Remove(playerToRemove.id);
                        playerToRemove.Yeet();
                        playerToRemove = null;
                        break;

                    case "updatePlayerScene":
                        Player playerToUpdate = playerLookup[res.GetProperty("id").GetInt64()];
                        playerToUpdate.ChangeScene(res.GetProperty("scene").GetString());
                        break;

                    case "updatePlayerPosition":
                        Player playerToUpdate2 = playerLookup[res.GetProperty("id").GetInt64()];

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

                        playerToUpdate2.UpdatePosition(bodyPos, handLPos, handRPos, float.Parse(res.GetProperty("armStrechL").GetString()), float.Parse(res.GetProperty("armStrechR").GetString()), footLPos, footRPos, footLBendPos, footRBendPos, bodyRot, handLRot, handRRot, footLRot, footRRot);
                        break;
                }
            };

            ws.Connect();
        }

        public override void OnApplicationStart()
        {
            thisAssembly = MelonAssembly.Assembly;
            arial = new GameObject().AddComponent<TextMesh>().font;

            makingShadowPrefab = true;
            MelonCoroutines.Start(MakeShadowPrefab());
            makingShadowPrefab = false;

            string[] args = Environment.GetCommandLineArgs();

            for (int i = 1; i < args.Length; i++)
            {
                if (args[i] == "-bwf-debug")
                {
                    debugMode = true;
                } else if (args[i] == "-server" && i + 1 < args.Length)
                {
                    server = args[i + 1];
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

            LoggerInstance.Msg("Making Shadow Prefab Done");
        }

        public override void OnDeinitializeMelon()
        {
            if (!connected) return;
            ws.Send($"{{\"data\":\"yeet\", \"id\":{playerId}}}");
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
                ws.Send($"{{\"data\":\"getRooms\"}}");
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

            refreshButtonOb.SetActive(false);

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
                ws.Send($"{{\"data\":\"makeRoom\", \"id\":{playerId}, \"name\":\"{roomName.text}\", \"pass\":\"{roomName2.text}\"}}");
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

            playerContainer = new GameObject("Players");
            GameObject.DontDestroyOnLoad(playerContainer);
        }

        public void makeRoomListing(string name, int roomId, bool hasPass, int playerCount, string hostname)
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
                ws.Send($"{{\"data\":\"joinRoom\", \"id\":{playerId}, \"room\":{roomId}, \"pass\":\"{password}\"}}");
            });

            room.transform.localScale = multiplayerMenu.transform.localScale;
        }

        public override async void OnFixedUpdate()
        {
            if (makingShadowPrefab) return;

            if (SteamManager.Initialized && ws == null)
            {
                Connect();
            }

            if (!connected) return;

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

            if (inRoom && mePlayer != null && shadow != null && meClimbing != null)
            {
                //meFoots.legHolder.localPosition = new Vector3(0, meFoots.legHolderDefaultPos.localPosition.y, 0);

                string updateString = $"{{\"data\":\"updatePosition\", \"id\":{playerId}, " +
                    $"\"position\":[\"{shadow.transform.position.x}\",\"{shadow.transform.position.y}\",\"{shadow.transform.position.z}\"], " +
                    $"\"handL\":[\"{shadow.handIK_L.solver.arm.target.position.x}\",\"{shadow.handIK_L.solver.arm.target.position.y}\",\"{shadow.handIK_L.solver.arm.target.position.z}\"], " +
                    $"\"handR\":[\"{shadow.handIK_R.solver.arm.target.position.x}\",\"{shadow.handIK_R.solver.arm.target.position.y}\",\"{shadow.handIK_R.solver.arm.target.position.z}\"], " +
                    $"\"armStrechL\":\"{shadow.handIK_L.solver.arm.armLengthMlp}\", " +
                    $"\"armStrechR\":\"{shadow.handIK_R.solver.arm.armLengthMlp}\", " +
                    $"\"footL\":[\"{shadow.footIK_L.solver.target.transform.position.x}\",\"{shadow.footIK_L.solver.target.position.y}\",\"{shadow.footIK_L.solver.target.position.z}\"], " +
                    $"\"footR\":[\"{shadow.footIK_R.solver.target.position.x}\",\"{shadow.footIK_R.solver.target.position.y}\",\"{shadow.footIK_R.solver.target.position.z}\"], " +
                    $"\"footLBend\":[\"{shadow.realleftKnee.transform.position.x}\",\"{shadow.realleftKnee.transform.position.y}\",\"{shadow.realleftKnee.transform.position.z}\"], " +
                    $"\"footRBend\":[\"{shadow.realrightKnee.transform.position.x}\",\"{shadow.realrightKnee.transform.position.y}\",\"{shadow.realrightKnee.transform.position.z}\"], " +
                    $"\"rotation\":[\"{shadow.transform.rotation.x}\",\"{shadow.transform.rotation.y}\",\"{shadow.transform.rotation.z}\",\"{shadow.transform.rotation.w}\"], " +
                    $"\"handLRotation\":[\"{shadow.handIK_L.solver.arm.target.rotation.x}\",\"{shadow.handIK_L.solver.arm.target.rotation.y}\",\"{shadow.handIK_L.solver.arm.target.rotation.z}\",\"{shadow.handIK_L.solver.arm.target.rotation.w}\"], " +
                    $"\"handRRotation\":[\"{shadow.handIK_R.solver.arm.target.rotation.x}\",\"{shadow.handIK_R.solver.arm.target.rotation.y}\",\"{shadow.handIK_R.solver.arm.target.rotation.z}\",\"{shadow.handIK_R.solver.arm.target.rotation.w}\"], " +
                    $"\"footLRotation\":[\"{shadow.footIK_L.solver.target.rotation.x}\",\"{shadow.footIK_L.solver.target.rotation.y}\",\"{shadow.footIK_L.solver.target.rotation.z}\",\"{shadow.footIK_L.solver.target.rotation.w}\"], " +
                    $"\"footRRotation\":[\"{shadow.footIK_R.solver.target.rotation.x}\",\"{shadow.footIK_R.solver.target.rotation.y}\",\"{shadow.footIK_R.solver.target.rotation.z}\",\"{shadow.footIK_R.solver.target.rotation.w}\"]" +
                    $"}}";
                ws.Send(updateString);

                //meFoots.legHolder.localPosition = new Vector3(0, meFoots.legHolderDefaultPos.localPosition.y, meFoots.legHolderDefaultPos.localPosition.z);
            }

            foreach (Player player in playersInRoom)
            {
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
            }
        }

        public override void OnUpdate()
        {
            if (!connected) return;

            if (Input.GetKeyDown(KeyCode.Tab))
            {
                multiplayerMenu.SetActive(!multiplayerMenu.activeSelf);

                if (multiplayerMenu.activeSelf)
                {
                    mouseOnOpen = Cursor.lockState;
                } else if (InGameMenu.isCurrentlyNavigationMenu)
                {
                    mouseOnOpen = CursorLockMode.None;
                }

                Cursor.lockState = multiplayerMenu.activeSelf ? CursorLockMode.None : mouseOnOpen;
                Cursor.visible = multiplayerMenu.activeSelf || mouseOnOpen == CursorLockMode.None ? true : false;
            }

            if (Input.GetKeyDown(KeyCode.BackQuote))
            {
                Cursor.lockState = Cursor.lockState == CursorLockMode.Locked ? CursorLockMode.None : CursorLockMode.Locked;
                Cursor.visible = Cursor.lockState == CursorLockMode.None;
            }

            if (multiplayerMenu.activeSelf)
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
        }

        public override async void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            if (multiplayerMenuObject == null) {
                MakeMenu();
                multiplayerMenu.SetActive(false);
            }

            if (makingShadowPrefab) return;
            if (!connected) return;

            LoggerInstance.Msg("In scene " + sceneName);
            LoggerInstance.Msg("Shadow Prefab " + shadowPrefab);

            ws.Send($"{{\"data\":\"switchScene\", \"id\":{playerId}, \"scene\":\"{sceneName}\"}}");
            multiplayerMenu.SetActive(false);

            mePlayer = GameObject.Find("Player");

            PlayerShadow[] shads = Resources.FindObjectsOfTypeAll<PlayerShadow>();
            if (shads.Length != 0)
            {
                shadow = shads[0];
            }

            BodyTurning[] bodyTurns = Resources.FindObjectsOfTypeAll<BodyTurning>();
            if (bodyTurns.Length != 0)
            {
                meBodyTurning = bodyTurns[0];
            }

            Climbing[] climbings = Resources.FindObjectsOfTypeAll<Climbing>();
            if (climbings.Length != 0)
            {
                meClimbing = climbings[0];
            }

            Footplacement[] placements = Resources.FindObjectsOfTypeAll<Footplacement>();
            if (placements.Length != 0)
            {
                meFoots = placements[0];
            }

            foreach (Player player in playersInRoom)
            {
                LoggerInstance.Msg("fixing " + player.name + "'s scene");
                player.UpdateVisual(sceneName);
            }
        }

        public void giveInfo()
        {
            connected = true;
            LoggerInstance.Msg("giving identity");
            if (!debugMode)
            {
                playerId = SteamUser.GetSteamID().m_SteamID;
                playerName = SteamFriends.GetPersonaName();
            } else
            {
                playerId = (ulong)LongRandom(0, 100000000000000);
                playerName = RandomString(10);
            }
            ws.Send($"{{\"data\":\"identify\", \"id\":{playerId}, \"name\":\"{playerName}\", \"scene\":\"{SceneManager.GetActiveScene().name}\"}}");
            ws.Send($"{{\"data\":\"getRooms\"}}");
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
    }
}
