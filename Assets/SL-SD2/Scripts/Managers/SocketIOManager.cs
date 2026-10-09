
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Newtonsoft.Json;
using Best.SocketIO;
using Best.SocketIO.Events;
using DG.Tweening;

public class SocketIOManager : MonoBehaviour
{
  [SerializeField] private SlotController slotManager;
  [SerializeField] private UIManager uiManager;
  [SerializeField] internal JSFunctCalls JSManager;
  [SerializeField] private string testToken;
  [SerializeField] private GameObject RaycastBlocker;
  //internal List<string> bonusdata = null;
  internal GameData InitialData = null;
  internal UiData UIData = null;
  internal Root ResultData = null;
  internal Root BonusData = null;
  internal Features FeaturesData = null;
  internal Player PlayerData = null;
  internal bool isResultdone = false;
  internal bool isBonusdone = false;
  internal bool SetInit = false;

  private SocketManager manager;
  protected string SocketURI = null;
  // protected string TestSocketURI = "https://game-crm-rtp-backend.onrender.com/";
  protected string TestSocketURI = "https://devrealtime.dingdinghouse.com/";
  protected string nameSpace = "playground";
  private Socket gameSocket;
  protected string gameID = "SL-PCN";
  //protected string gameID = "";
  private const int maxReconnectionAttempts = 6;
  private readonly TimeSpan reconnectionDelay = TimeSpan.FromSeconds(10);
  string myAuth = null;

  private bool isConnected = false; //Back2 Start
  private bool hasEverConnected = false;
  private const int MaxReconnectAttempts = 5;
  private const float ReconnectDelaySeconds = 2f;

  private float lastPongTime = 0f;
  private float pingInterval = 2f;
  private float pongTimeout = 3f;
  private bool waitingForPong = false;
  private int missedPongs = 0;
  private const int MaxMissedPongs = 5;
  private Coroutine PingRoutine; //Back2 end

  private bool hasFocus = true;
  private float focusLostTime = 0f;
  private Coroutine focusCheckRoutine;
  private float maxBackgroundTime = 60f;
  private bool isExiting = false;
  private bool isBeingDestroyed = false;

  private void Awake()
  {
    //Debug.unityLogger.logEnabled = false;
    SetInit = false;
  }

  private void OnDestroy()
  {
    isBeingDestroyed = true;
  }

  internal void HandleFocusChange(bool focus)
  {
    hasFocus = focus;

    if (!focus)
    {
      focusLostTime = Time.time;
      if (focusCheckRoutine == null && !isExiting && !isBeingDestroyed)
        focusCheckRoutine = StartCoroutine(FocusTimeoutCheck());
    }
    else
    {
      if (focusCheckRoutine != null)
      {
        StopCoroutine(focusCheckRoutine);
        focusCheckRoutine = null;
      }
    }
  }

  private IEnumerator FocusTimeoutCheck()
  {
    while (!hasFocus && !isExiting && !isBeingDestroyed)
    {
      if (Time.time - focusLostTime >= maxBackgroundTime)
      {
        Debug.LogWarning("[SOCKET] Background timeout — closing connection");
        isConnected = false;
        ResetPingRoutine();

        if (manager != null)
        {
          try { manager.Close(); }
          catch (Exception e) { Debug.LogWarning($"[SOCKET] Focus close error: {e.Message}"); }
        }

        uiManager.DisconnectionPopup();
        focusCheckRoutine = null;
        yield break;
      }

      yield return new WaitForSecondsRealtime(1f);
    }

    focusCheckRoutine = null;
  }

  private void Start()
  {
    OpenSocket();
  }

  void CloseGame()
  {
    Debug.Log("Unity: Closing Game");
    StartCoroutine(CloseSocket());
  }

  void ReceiveAuthToken(string jsonData)
  {
    Debug.Log("Received data: " + jsonData);
    var data = JsonUtility.FromJson<AuthTokenData>(jsonData);
    SocketURI = data.socketURL;
    myAuth = data.cookie;
    nameSpace = data.nameSpace;
  }

  private void OpenSocket()
  {
    //Create and setup SocketOptions
    SocketOptions options = new SocketOptions(); //Back2 Start
    options.AutoConnect = false;
    options.Reconnection = false;
    options.Timeout = TimeSpan.FromSeconds(3); //Back2 end
    options.ConnectWith = Best.SocketIO.Transports.TransportTypes.WebSocket;

#if UNITY_WEBGL && !UNITY_EDITOR
        JSManager.SendCustomMessage("authToken");
        StartCoroutine(WaitForAuthToken(options));
#else
    object authFunction(SocketManager manager, Socket socket)
    {
      return new
      {
        token = testToken
      };
    }
    options.Auth = authFunction;
    SetupSocketManager(options);
#endif
  }

  private IEnumerator WaitForAuthToken(SocketOptions options)
  {
    // Wait until myAuth is not null
    while (myAuth == null)
    {
      Debug.Log("My Auth is null");
      yield return null;
    }
    while (SocketURI == null)
    {
      Debug.Log("My Socket is null");
      yield return null;
    }
    Debug.Log("My Auth is not null");
    // Once myAuth is set, configure the authFunction
    object authFunction(SocketManager manager, Socket socket)
    {
      return new
      {
        token = myAuth
      };
    }
    options.Auth = authFunction;
    Debug.Log("Auth function configured with token: " + myAuth);

    // Proceed with connecting to the server
    SetupSocketManager(options);

    yield return null;
  }

  private void SetupSocketManager(SocketOptions options)
  {
    Debug.Log("Setup socket manager");
    // Create and setup SocketManager
#if UNITY_EDITOR
    this.manager = new SocketManager(new Uri(TestSocketURI), options);
#else
    this.manager = new SocketManager(new Uri(SocketURI), options);
#endif

    if (string.IsNullOrEmpty(nameSpace))
    {
      gameSocket = this.manager.Socket;
    }
    else
    {
      Debug.Log("nameSpace: " + nameSpace);
      gameSocket = this.manager.GetSocket("/" + nameSpace);
    }
    // Set subscriptions
    gameSocket.On<ConnectResponse>(SocketIOEventTypes.Connect, OnConnected);
    gameSocket.On(SocketIOEventTypes.Disconnect, OnDisconnected); //Back2 Start
    gameSocket.On<Error>(SocketIOEventTypes.Error, OnError);
    gameSocket.On<string>("game:init", OnListenEvent);
    gameSocket.On<string>("result", OnResult);
    gameSocket.On<bool>("socketState", OnSocketState);
    gameSocket.On<string>("internalError", OnSocketError);
    gameSocket.On<string>("alert", OnSocketAlert);
    gameSocket.On<string>("pong", OnPongReceived); //Back2 Start
    gameSocket.On<string>("AnotherDevice", OnSocketOtherDevice);
    gameSocket.On<string>("balance:sync", OnBalanceSync);

    manager.Open(); //Back2 Start
  }

  // Connected event handler implementation
  void OnConnected(ConnectResponse resp) //Back2 Start
  {
    Debug.Log("✅ Connected to server.");

    if (hasEverConnected)
    {
      uiManager.CheckAndClosePopups();
    }

    isConnected = true;
    hasEverConnected = true;
    waitingForPong = false;
    missedPongs = 0;
    lastPongTime = Time.time;
    SendPing();
  } //Back2 end

  private void OnDisconnected() //Back2 Start
  {
    Debug.LogWarning("⚠️ Disconnected from server.");
    isConnected = false;
    ResetPingRoutine();
    uiManager.DisconnectionPopup();
  } //Back2 end

  private void OnPongReceived(string data) //Back2 Start
  {
    //// Debug.Log("✅ Received pong from server.");
    waitingForPong = false;
    missedPongs = 0;
    lastPongTime = Time.time;
    //    Debug.Log($"⏱️ Updated last pong time: {lastPongTime}");
    //  Debug.Log($"📦 Pong payload: {data}");
  } //Back2 end

  private void OnError(Error err)
  {
    Debug.LogError("Socket Error Message: " + err);
    if (!string.IsNullOrEmpty(err.message) && err.message.Contains("Session expired"))
    {
      Debug.LogWarning("Session expired detected");
      OnDisconnected();
#if UNITY_WEBGL && !UNITY_EDITOR
      JSManager.SendCustomMessage("session_expired");
#endif
    }
    else
    {
#if UNITY_WEBGL && !UNITY_EDITOR
      JSManager.SendCustomMessage("error");
#endif
    }
  }

  void OnResult(string data)
  {
    ParseResponse(data);
  }

  private void OnListenEvent(string data)
  {
    ParseResponse(data);
  }

  private void OnSocketState(bool state)
  {
    if (state)
    {
      Debug.Log("my state is " + state);
    }
  }
  private void OnSocketError(string data)
  {
    Debug.Log("Received error with data: " + data);
  }

  private void OnSocketAlert(string data)
  {
    Debug.Log("Received alert with data: " + data);
  }

  private void OnSocketOtherDevice(string data)
  {
    Debug.Log("Received Device Error with data: " + data);
    //  uiManager.ADfunction();
  }

  private void OnBalanceSync(string data)
  {
    BalanceSyncPayload syncPayload = JsonConvert.DeserializeObject<BalanceSyncPayload>(data);
    if (syncPayload == null) return;

    if (PlayerData == null) PlayerData = new Player();
    PlayerData.balance = syncPayload.balance;

    slotManager.UpdateBalanceDisplay(syncPayload.balance);
  }

  private void SendPing() //Back2 Start
  {
    ResetPingRoutine();
    PingRoutine = StartCoroutine(PingCheck());
  }

  void ResetPingRoutine()
  {
    if (PingRoutine != null)
    {
      StopCoroutine(PingRoutine);
    }
    PingRoutine = null;
  }

  private IEnumerator PingCheck()
  {
    while (true)
    {
      //  Debug.Log($"🟡 PingCheck | waitingForPong: {waitingForPong}, missedPongs: {missedPongs}, timeSinceLastPong: {Time.time - lastPongTime}");

      if (missedPongs == 0)
      {
        uiManager.CheckAndClosePopups();
      }

      // If waiting for pong, and timeout passed
      if (waitingForPong)
      {
        if (missedPongs == 2)
        {
          uiManager.ReconnectionPopup();
        }
        missedPongs++;
        // Debug.LogWarning($"⚠️ Pong missed #{missedPongs}/{MaxMissedPongs}");

        if (missedPongs >= MaxMissedPongs)
        {
          //   Debug.LogError("❌ Unable to connect to server — 5 consecutive pongs missed.");
          isConnected = false;
          uiManager.DisconnectionPopup();
          yield break;
        }
      }

      // Send next ping
      waitingForPong = true;
      lastPongTime = Time.time;
      //  Debug.Log("📤 Sending ping...");
      SendDataWithNamespace("ping");
      yield return new WaitForSeconds(pingInterval);
    }
  } //Back2 end
  internal void SendDataWithNamespace(string eventName, string json = null)
  {
    // Send the message
    if (gameSocket != null && gameSocket.IsOpen)
    {
      if (json != null)
      {
        gameSocket.Emit(eventName, json);
        Debug.Log("JSON data sent: " + json);
      }
      else
      {
        gameSocket.Emit(eventName);
      }
    }
    else
    {
      Debug.LogWarning("Socket is not connected.");
    }
  }

  internal IEnumerator CloseSocket() //Back2 Start
  {
    isExiting = true;
    RaycastBlocker.SetActive(true);
    ResetPingRoutine();

    Debug.Log("Closing Socket");

    manager?.Close();
    manager = null;

    Debug.Log("Waiting for socket to close");

    yield return new WaitForSeconds(0.5f);

    Debug.Log("Socket Closed");

#if UNITY_WEBGL && !UNITY_EDITOR
    JSManager.SendCustomMessage("OnExit"); //Telling the react platform user wants to quit and go back to homepage
#endif
  } //Back2 end

  private void ParseResponse(string jsonObject)
  {
    Debug.Log(jsonObject);
    Root myData = JsonConvert.DeserializeObject<Root>(jsonObject);

    string id = myData.id;

    switch (id)
    {
      case "initData":
        {
          InitialData = myData.gameData;
          UIData = myData.uiData;
          PlayerData = myData.player;
          FeaturesData = myData.features;

          if (!SetInit)
          {
            // List<string> LinesString = ConvertListListIntToListString(InitialData.lines);
            PopulateSlotSocket();
            SetInit = true;
          }
          else
          {
            RefreshUI();
          }
          break;
        }
      case "ResultData":
        {
          ResultData = myData;
          PlayerData = myData.player;
          isResultdone = true;
          break;
        }
      case "BonusResult":
        {
          BonusData = myData;
          PlayerData = myData.player;
          isBonusdone = true;
          if (BonusData.payload.isBonusFeatureActive) slotManager.switchtoBonusGame(true);
          else { slotManager.Bonusover(); }
          break;
        }
    }
  }

  List<string> GetBonusData(List<int> bonusData)
  {
    List<string> bonusDataString = new();
    foreach (int data in bonusData)
    {
      bonusDataString.Add(data.ToString());
    }
    return bonusDataString;
  }

  private void RefreshUI()
  {
    //uiManager.InitialiseUIData(UIData.paylines);
  }

  private void PopulateSlotSocket()
  {
    slotManager.UpdateUI(PlayerData.balance);

#if UNITY_WEBGL && !UNITY_EDITOR
    JSManager.SendCustomMessage("OnEnter");
#endif
    RaycastBlocker.SetActive(false);
  }

  internal void AccumulateResult(int currBet)
  {
    isResultdone = false;
    MessageData message = new();
    message.type = "SPIN";
    message.payload.betIndex = currBet;

    // Serialize message data to JSON
    string json = JsonUtility.ToJson(message);
    SendDataWithNamespace("request", json);
  }
  internal void AccumulateBonus(string type)
  {
    //isResultdone = false;
    MessageData message = new();
    message.type = "BONUS_DECISION";
    message.payload.decision = type;

    // Serialize message data to JSON
    string json = JsonUtility.ToJson(message);
    SendDataWithNamespace("request", json);
  }

  private List<string> ConvertListListIntToListString(List<List<int>> listOfLists)
  {
    List<string> resultList = new List<string>();

    foreach (List<int> innerList in listOfLists)
    {
      // Convert each integer in the inner list to string
      List<string> stringList = new List<string>();
      foreach (int number in innerList)
      {
        stringList.Add(number.ToString());
      }

      // Join the string representation of integers with ","
      string joinedString = string.Join(",", stringList.ToArray()).Trim();
      resultList.Add(joinedString);
    }

    return resultList;
  }
}




[Serializable]
public class GameData
{
  public List<double> bets { get; set; }
}

[Serializable]
public class MessageData
{
  public string type;
  public Data payload = new();

}

[Serializable]
public class Data
{
  public int betIndex;
  public string decision;
  // public int levelIndex;
  // public List<int> index;
  // public int option;
}

[Serializable]
public class AuthTokenData
{
  public string cookie;
  public string socketURL;
  public string nameSpace;
}

[Serializable]
public class BalanceSyncPayload
{
  public double balance;
}




[Serializable]
public class Root
{
  public string id { get; set; }
  public GameData gameData { get; set; }
  public Features features { get; set; }
  public UiData uiData { get; set; }
  public Player player { get; set; }
  public bool success { get; set; }
  public List<List<string>> matrix { get; set; }
  public Payload payload { get; set; }
}

[Serializable]
public class Symbol
{
  public int id { get; set; }
  public string name { get; set; }
  public List<object> multiplier { get; set; }
  public int? payout { get; set; }
  public string description { get; set; }
}

[Serializable]
public class FrozenIndex
{
  public List<int> position { get; set; }
  public string symbol { get; set; }
}

// [Serializable]
// public class Payload
// {
//   public int currentWinning { get; set; }
//   public bool isRedRespin { get; set; }
//   public bool isZeroRespin { get; set; }
//   public List<FrozenIndex> frozenIndices { get; set; }
// }

[Serializable]
public class UiData
{
  public Paylines paylines { get; set; }
}



// new


public class BonusFeature
{
  public int initialOffers { get; set; }
  public string triggerSymbol { get; set; }
  public bool autoAcceptLast { get; set; }
  public List<int> multiplierCount { get; set; }
  public List<int> multiplierValues { get; set; }
  public List<double> multiplierProbabilities { get; set; }
  public List<double> multiplierCountProbabilities { get; set; }
}
public class RewardsPerOfferMapping
{
  public int Bonus_2Picks { get; set; }
  public int Bonus_3Picks { get; set; }
  public int Bonus_4Picks { get; set; }
}
public class BonusSettings
{
  public List<double> weights { get; set; }
  public string _comment { get; set; }
  public int offerCount { get; set; }
  public List<double> multipliers { get; set; }
}

public class Examples
{
  [JsonProperty("2|0")]
  public int _20 { get; set; }

  [JsonProperty("10|0")]
  public int _100 { get; set; }

  [JsonProperty("5|Blank")]
  public int _5Blank { get; set; }
}

public class Features
{
  public MultiplierLockingFeature multiplierLockingFeature { get; set; }
  public BonusFeature bonusFeature { get; set; }
  public BonusSettings bonusSettings { get; set; }
  public NumberConstruction numberConstruction { get; set; }
}



public class MultiplierLockingFeature
{
  public bool enabled { get; set; }
  public int duration { get; set; }
  public string description { get; set; }
  public int lockReelIndex { get; set; }
  public List<string> allowedSymbols { get; set; }
}

public class NumberConstruction
{
  public bool enabled { get; set; }
  public string formula { get; set; }
  //public Examples examples { get; set; }
  public string description { get; set; }
}

public class Paylines
{
  public List<Symbol> symbols { get; set; }
}

public class PickMapping
{
  public int Bonus_2Picks { get; set; }
  public int Bonus_3Picks { get; set; }
  public int Bonus_4Picks { get; set; }
}

public class Player
{
  public double balance { get; set; }
}

[Serializable]
public class Payload
{
  public double winAmount { get; set; }
  public int constructedNumber { get; set; }
  public int appliedMultiplier { get; set; }
  public List<string> triggeredFeatures { get; set; }
  public bool isJackpot { get; set; }
  public object lockedMultiplier { get; set; }
  public bool isMultiplierLocked { get; set; }
  public bool isBonusFeatureActive { get; set; }
  public BonusData bonusData { get; set; }
  public bool lockActive { get; set; }
  public int megaWin { get; set; }
  public int majorWin { get; set; }
  public int minorWin { get; set; }
}


public class BonusData
{
  public bool isActive { get; set; }
  public int currentOfferIndex { get; set; }
  public int totalOffers { get; set; }
  public int bet { get; set; }
  public int accumulatedWin { get; set; }
  public List<Reward> rewards { get; set; }
}

public class Offer
{
  public List<Reward> rewards { get; set; }
}

public class Reward
{
  public string type { get; set; }
  public int value { get; set; }
  public int multiplier { get; set; }
  public int multiplierIndex { get; set; }
  public int position { get; set; }
  public bool isLocked { get; set; }
}


