using System;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using UnityEngine;
using System.Net.Sockets;
using Newtonsoft.Json.Linq;

// TODO: class containing constructors for elemem messages

public abstract class IHostPC : EventLoop {
    public abstract JObject WaitForMessage(string type, int timeout);
    public abstract void Connect();
    public abstract NetworkStream GetReadStream();
    public abstract NetworkStream GetWriteStream();
    public abstract void HandleMessage(string message, DateTime time);
    public abstract void SendMessage(string type, Dictionary<string, object> data);
}

public class ElememListener {
    ElememInterface elemem;
    Byte[] buffer; 
    const Int32 bufferSize = 2048;

    private volatile ManualResetEventSlim callbackWaitHandle;
    private ConcurrentQueue<string> queue = null;

    string messageBuffer = "";
    public ElememListener(ElememInterface _elemem) {
        elemem = _elemem;
        buffer = new Byte[bufferSize];
        callbackWaitHandle = new ManualResetEventSlim(true);
    }

    public bool IsListening() {
        return !callbackWaitHandle.IsSet;
    }

    public ManualResetEventSlim GetListenHandle() {
        return callbackWaitHandle;
    }

    public void RegisterMessageQueue(ConcurrentQueue<string> messages) {
        queue = messages;
    }

    public void RemoveMessageQueue() {
        queue = null;
    }

    public void Listen() {
        if(IsListening()) {
            throw new AccessViolationException("Already Listening");
        }

        NetworkStream stream = elemem.GetReadStream();
        callbackWaitHandle.Reset();
        stream.BeginRead(buffer, 0, bufferSize, Callback, 
                        new Tuple<NetworkStream, ManualResetEventSlim, ConcurrentQueue<string>>
                            (stream, callbackWaitHandle, queue));
    } 

    private void Callback(IAsyncResult ar) {
        NetworkStream stream;
        ConcurrentQueue<string> queue;
        ManualResetEventSlim handle;
        int bytesRead;

        Tuple<NetworkStream, ManualResetEventSlim, ConcurrentQueue<string>> state = (Tuple<NetworkStream, ManualResetEventSlim, ConcurrentQueue<string>>)ar.AsyncState;
        stream = state.Item1;
        handle = state.Item2;
        queue = state.Item3;

        bytesRead = stream.EndRead(ar);

        foreach(string msg in ParseBuffer(bytesRead)) {
            queue?.Enqueue(msg); // queue may be deleted by this point, if wait has ended
        }

        handle.Set();
    }
    
    private List<string> ParseBuffer(int bytesRead) {
        messageBuffer += System.Text.Encoding.UTF8.GetString(buffer, 0, bytesRead);
        List<string> received = new List<string>();

        UnityEngine.Debug.Log("ParseBuffer");
        UnityEngine.Debug.Log(messageBuffer);

        while(messageBuffer.IndexOf("\n") != -1) {
            string message = messageBuffer.Substring(0, messageBuffer.IndexOf("\n") + 1);
            received.Add(message);
            messageBuffer = messageBuffer.Substring(messageBuffer.IndexOf("\n") + 1);
            
            ReportMessage(message);
        }

        return received;
    }

    private void ReportMessage(string message) {
        elemem.Do(new EventBase<string, DateTime>(elemem.HandleMessage, message, DataReporter.TimeStamp()));
    }
}

// NOTE: the gotcha here is avoiding deadlocks when there's an error
// message in the queue and some blocking wait in the EventLoop thread
public class ElememInterface : IHostPC 
{
    public IInterfaceManager im;

    int messageTimeout = 1000;
    private Stopwatch heartbeatTimer;
    int heartbeatTimeout = 8000; // TODO: configuration

    private TcpClient elemem;
    private ElememListener listener;
    private int heartbeatCount = 0;

    public ElememInterface(IInterfaceManager _im) {
        im = _im;
        listener = new ElememListener(this);
        Start();
        Do(new EventBase(Connect));
    }

    ~ElememInterface() {
        elemem.Close();
    }


    public override NetworkStream GetWriteStream() {
        // implement locking here
        if(elemem == null) {
            throw new InvalidOperationException("Socket not initialized.");
        }

        return elemem.GetStream();
    }

    public override NetworkStream GetReadStream() {
        // implement locking here
        if(elemem == null) {
            throw new InvalidOperationException("Socket not initialized.");
        }

        return elemem.GetStream();
    }

    public override void Connect() {
        elemem = new TcpClient(); 

        try {
            IAsyncResult result = elemem.BeginConnect((string)im.GetSetting("ip"), (int)im.GetSetting("port"), null, null);
            
            result.AsyncWaitHandle.WaitOne(messageTimeout);
            elemem.EndConnect(result);
        }
        catch(SocketException e) {    // TODO: set hostpc state on task side
            im.Do(new EventBase<string>(im.SetHostPCStatus, "ERROR")); 
            throw new OperationCanceledException("Failed to Connect");
        }

        im.Do(new EventBase<string>(im.SetHostPCStatus, "INITIALIZING")); 

        SendMessage("CONNECTED", new Dictionary<string, object>()); // Awake
        WaitForMessage("CONNECTED_OK", messageTimeout);

        Dictionary<string, object> configDict = new Dictionary<string, object>();
        configDict.Add("stim_mode", (string)im.GetSetting("stimMode"));
        configDict.Add("experiment", (string)im.GetSetting("experimentName"));
        configDict.Add("subject", (string)im.GetSetting("participantCode"));
        configDict.Add("session", (int)im.GetSetting("session"));
        SendMessage("CONFIGURE", configDict);
        WaitForMessage("CONFIGURE_OK", messageTimeout);

        // excepts if there's an issue with latency, else returns
        DoLatencyCheck();

        // start heartbeats
        heartbeatTimer = new Stopwatch();
        heartbeatTimer.Start();
        int interval = (int)im.GetSetting("heartbeatInterval");
        DoRepeating(new RepeatingEvent(SendHeartbeat, -1, 0, interval));
        DoRepeating(new RepeatingEvent(CheckHeartbeat, -1, interval / 2, interval));

        SendMessage("READY", new Dictionary<string, object>());
        im.Do(new EventBase<string>(im.SetHostPCStatus, "READY")); 
    }

    private void DoLatencyCheck() {
        // except if latency is unacceptable
        Stopwatch sw = new Stopwatch();
        float[] delay = new float[20];

        for(int i=0; i < 20; i++) {
            sw.Restart();
            SendHeartbeat();
            WaitForMessage("HEARTBEAT_OK", 20);
            sw.Stop();

            delay[i] = sw.ElapsedTicks * (1000f / Stopwatch.Frequency);
            if(delay[i] > 20) {
                break;
            }

            Thread.Sleep(50 - (int)delay[i]);
        }
        
        float max = delay.Max();
        float mean = delay.Sum() / delay.Length;
        float acc = (1000L*1000L*1000L) / Stopwatch.Frequency;

        Dictionary<string, object> dict = new Dictionary<string, object>();
        dict.Add("max_latency", max);
        dict.Add("mean_latency", mean);
        dict.Add("accuracy", acc);

        im.Do(new EventBase<string, Dictionary<string, object>>(im.ReportEvent, "latency check", dict));
    }

    public override JObject WaitForMessage(string type, int timeout) {
        Stopwatch sw = new Stopwatch();
        sw.Start();

        ManualResetEventSlim wait;
        int waitDuration;
        ConcurrentQueue<string> queue = new ConcurrentQueue<string>();
        JObject json;

        listener.RegisterMessageQueue(queue);
        while(sw.ElapsedMilliseconds < timeout) {
            listener.Listen();
            wait = listener.GetListenHandle();
            waitDuration =  timeout - (int)sw.ElapsedMilliseconds;
            waitDuration =  waitDuration > 0 ? waitDuration : 0;

            wait.Wait(waitDuration);

            string message;
            while(queue.TryDequeue(out message)) {
                json = JObject.Parse(message);
                if(json.GetValue("type").Value<string>() == type) {
                    listener.RemoveMessageQueue();
                    return json;
                }
            }
        }
        
        listener.RemoveMessageQueue();
        throw new TimeoutException("Timed out waiting for response");
    }

    public override void HandleMessage(string message, DateTime time) {
        JObject json = JObject.Parse(message);
        json.Add("task pc time", time);

        string type = json.GetValue("type").Value<string>();
        ReportMessage(json.ToString(), false);

        if(type.Contains("ERROR")) {
            new ErrorNotification().Notify(new Exception("Error received from Host PC."));
            return;
        }
        if(type == "EXIT") {
            return;
        }
        else if(type == "HEARTBEAT_OK") {
            heartbeatTimer.Restart();
        }

        // start listener if not running
        if(!listener.IsListening()) {
            listener.Listen();
        }
    }

    public override void SendMessage(string type, Dictionary<string, object> data) {
        DataPoint point = new DataPoint(type, DataReporter.TimeStamp(), data);
        string message = point.ToJSON();

        UnityEngine.Debug.Log("Sent Message");
        UnityEngine.Debug.Log(message);

        Byte[] bytes = System.Text.Encoding.UTF8.GetBytes(message);

        NetworkStream stream = GetWriteStream();
        stream.Write(bytes, 0, bytes.Length);
        ReportMessage(message, true);
    }

    private void SendHeartbeat()
    {
        var data = new Dictionary<string, object>();
        data.Add("count", heartbeatCount);
        heartbeatCount++;
        SendMessage("HEARTBEAT", data);
    }

    private void CheckHeartbeat()
    {
        if(heartbeatTimer.ElapsedMilliseconds > heartbeatTimeout) {
            im.Do(new EventBase<string>(im.SetHostPCStatus, "ERROR"));
            throw new TimeoutException("Elemem connection timed out.");
        }
    }

    private void ReportMessage(string message, bool sent)
    {
        Dictionary<string, object> messageDataDict = new Dictionary<string, object>();
        messageDataDict.Add("message", message);
        messageDataDict.Add("sent", sent.ToString());

        im.Do(new EventBase<string, Dictionary<string, object>, DateTime>(im.ReportEvent, "network", 
                                messageDataDict, DataReporter.TimeStamp()));
    }
}