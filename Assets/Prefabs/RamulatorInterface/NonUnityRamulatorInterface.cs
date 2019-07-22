using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using NetMQ;

public class NonUnityRamulatorInterface : EventLoop 
{
    public InterfaceManager manager;

    //This will be updated with warnings about the status of ramulator connectivity
    public UnityEngine.UI.Text ramulatorWarningText;
    //This will be activated when a warning needs to be displayed
    public GameObject ramulatorWarning;

    //how long to wait for ramulator to connect
    int timeoutDelay = 600;
    int heartbeatThresh = 8;

    private int unreceivedHeartbeats = 0;

    private NetMQ.Sockets.PairSocket zmqSocket;
    private string address = "tcp://*:8889";

    public NonUnityRamulatorInterface(dynamic config=null) {
        if(config != null) {
            // TODO: validate config
            address = config.address;
            timeoutDelay = config.timeoutDelay;
            heartbeatThresh = config.heartbeatThresh;
        }

        Start();
    }

    // Class to wrap built in NetMQ functions in
    // Event passing structure. Uses a delegate for
    // NetMQ functions and handles out and return
    // values using a class so that changes are 
    // visible in the scope creating the event
    class ReceivedMessage {
        public string msg="";
        public bool success=false;
        public delegate bool ListenerDelegate(out string str);

        ListenerDelegate Listener;

        public void Listen(ReceivedMessage msgObject) {
            this.success = Listener(out msg);
        }

        public ReceivedMessage(ListenerDelegate _Listener) {
            Listener = new ListenerDelegate(_Listener);
        }
    }

    // TODO:
    void OnApplicationQuit()
    {
        if (zmqSocket != null)
            zmqSocket.Close();
        NetMQConfig.Cleanup();
    }

    //this coroutine connects to ramulator and communicates how ramulator expects it to
    //in order to start the experiment session.  follow it up with BeginNewTrial and
    //SetState calls
    public void BeginNewSession(int sessionNumber)
    {
        //Connect to ramulator///////////////////////////////////////////////////////////////////
        zmqSocket = new NetMQ.Sockets.PairSocket();
        zmqSocket.Bind(address);
        Debug.Log ("socket bound");

        WaitForMessage("CONNECTED", "Ramulator not connected.");

        //SendSessionEvent//////////////////////////////////////////////////////////////////////
        System.Collections.Generic.Dictionary<string, object> sessionData = new Dictionary<string, object>();
        sessionData.Add("name", manager.getSetting("experimentName"));
        sessionData.Add("version", Application.version);
        sessionData.Add("subject", manager.getSetting("participantCode"));
        sessionData.Add("session_number", sessionNumber.ToString());
        DataPoint sessionDataPoint = new DataPoint("SESSION", DataReporter.RealWorldTime(), sessionData);
        SendMessageToRamulator(sessionDataPoint.ToJSON());
//        yield return null;


        // TODO: not currently supported by event system
        //Begin Heartbeats///////////////////////////////////////////////////////////////////////
//        InvokeRepeating("SendHeartbeat", 0, 1);


        //SendReadyEvent////////////////////////////////////////////////////////////////////
        DataPoint ready = new DataPoint("READY", DataReporter.RealWorldTime(), new Dictionary<string, object>());
        SendMessageToRamulator(ready.ToJSON());
//        yield return null;


//        yield return WaitForMessage("START", "Start signal not received");


//        InvokeRepeating("ReceiveHeartbeat", 0, 1);

    }

    private void WaitForMessage(string containingString, string errorMessage)
    {
        ramulatorWarning.SetActive(true);
        ramulatorWarningText.text = "Waiting on Ramulator";

        ReceivedMessage receivedMessage = new ReceivedMessage(zmqSocket.TryReceiveFrameString); 
        float startTime = Time.time;
        EventBase<ReceivedMessage> messageEvent = new EventBase<ReceivedMessage>(receivedMessage.Listen, receivedMessage);

        while (!receivedMessage.msg.Contains(containingString))
        {
            Do(messageEvent); // inherited
            if (receivedMessage.success)
            {
                string messageString = receivedMessage.msg; // TODO: needs ToString?
                Debug.Log("received: " + messageString);
                ReportMessage(messageString, false);
            }

            //if we have exceeded the timeout time, show warning and stop trying to connect
            if (Time.time > startTime + timeoutDelay)
            {
                ramulatorWarningText.text = errorMessage;
                Debug.LogWarning("Timed out waiting for ramulator");
                break;
            }
            
        }
        ramulatorWarning.SetActive(false);
    }

    //ramulator expects this before the beginning of a new list
    public void BeginNewTrial(int trialNumber)
    {
        System.Collections.Generic.Dictionary<string, object> sessionData = new Dictionary<string, object>();
        sessionData.Add("trial", trialNumber.ToString());
        DataPoint sessionDataPoint = new DataPoint("TRIAL", DataReporter.RealWorldTime(), sessionData);
        SendMessageToRamulator(sessionDataPoint.ToJSON());
    }

    //ramulator expects this when you display words to the subject.
    //for words, stateName is "WORD"
    public void SetState(string stateName, bool stateToggle, System.Collections.Generic.Dictionary<string, object> extraData)
    {
        extraData.Add("name", stateName);
        extraData.Add("value", stateToggle.ToString());
        DataPoint sessionDataPoint = new DataPoint("STATE", DataReporter.RealWorldTime(), extraData);
        SendMessageToRamulator(sessionDataPoint.ToJSON());
    }

    public void SendMathMessage(string problem, string response, int responseTimeMs, bool correct)
    {
        Dictionary<string, object> mathData = new Dictionary<string, object>();
        mathData.Add("problem", problem);
        mathData.Add("response", response);
        mathData.Add("response_time_ms", responseTimeMs.ToString());
        mathData.Add("correct", correct.ToString());
        DataPoint mathDataPoint = new DataPoint("MATH", DataReporter.RealWorldTime(), mathData);
        SendMessageToRamulator(mathDataPoint.ToJSON());
    }

    public void SendExitMessage()
    {
        var data = new Dictionary<string, object>();
        var msg = new DataPoint("EXIT", DataReporter.RealWorldTime(), data);
        SendMessageToRamulator(msg.ToJSON());
    }

    private void SendHeartbeat()
    {
        DataPoint sessionDataPoint = new DataPoint("HEARTBEAT", DataReporter.RealWorldTime(), null);
        SendMessageToRamulator(sessionDataPoint.ToJSON());
    }

    private void ReceiveHeartbeat()
    {
        unreceivedHeartbeats = unreceivedHeartbeats + 1;
        Debug.Log("Unreceived heartbeats: " + unreceivedHeartbeats.ToString());
        if (unreceivedHeartbeats > heartbeatThresh)
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
			Application.Quit();
#endif
        }

        string receivedMessage = "";
        float startTime = Time.time;
        zmqSocket.TryReceiveFrameString(out receivedMessage);
        if (receivedMessage != "" && receivedMessage != null)
        {
            string messageString = receivedMessage.ToString();
            //Debug.Log("heartbeat received: " + messageString);
            ReportMessage(messageString, false);
            unreceivedHeartbeats = 0;
        }
    }

    private void SendMessageToRamulator(string message)
    {
        if (zmqSocket != null)
            zmqSocket.TrySendFrame(message, more: false);

        ReportMessage(message, true);
    }

    private void ReportMessage(string message, bool sent)
    {
        Dictionary<string, object> messageDataDict = new Dictionary<string, object>();
        messageDataDict.Add("message", message);
        messageDataDict.Add("sent", sent.ToString());
        manager.scriptedInput.ReportScriptedEvent("network", messageDataDict);
    }
}