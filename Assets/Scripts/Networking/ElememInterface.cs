using System;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using System.Net.Sockets;
using Newtonsoft.Json.Linq;

public class ElememInterface : NetworkInterface {

    public ElememInterface(InterfaceManager _im) : base(_im) {
        Do(new EventBase(Connect));
    }

    ~ElememInterface() {
        Disconnect();
    }

    // ------------
    // NetworkInterface
    // ------------

    public override void Connect() {
        tcpClient = new TcpClient(); 

        try {
            IAsyncResult result = tcpClient.BeginConnect((string)im.GetSetting("elememServerIP"), (int)im.GetSetting("elememServerPort"), null, null);
            result.AsyncWaitHandle.WaitOne(messageTimeout);
            tcpClient.EndConnect(result);
        } catch(SocketException) {
            im.Do(new EventBase<string>(im.SetHostPCStatus, "ERROR")); 
            throw new OperationCanceledException("Failed to connect to elemem");
        }

        im.Do(new EventBase<string>(im.SetHostPCStatus, "INITIALIZING")); 

        _ = listener.Listen(GetReadStream());
        SendAndWait("CONNECTED", new(), "CONNECTED_OK", messageTimeout);

        Dictionary<string, object> configDict = new Dictionary<string, object>();
        configDict.Add("stim_mode", (string)im.GetSetting("stimMode"));
        configDict.Add("experiment", (string)im.GetSetting("experimentName"));
        configDict.Add("subject", (string)im.GetSetting("participantCode"));
        configDict.Add("session", (int)im.GetSetting("session"));
        SendAndWait("CONFIGURE", configDict, "CONFIGURE_OK", messageTimeout);

        // excepts if there's an issue with latency, else returns
        DoLatencyCheck();

        // start heartbeats
        int interval = (int)im.GetSetting("heartbeatInterval");
        DoRepeating(new EventBase<string>(Heartbeat, ""), -1, 0, interval);

        SendMessage("READY", new Dictionary<string, object>());
        im.Do(new EventBase<string>(im.SetHostPCStatus, "READY")); 
    }

    public override void Disconnect() {
        listener?.CancelRead();
        tcpClient.Close();
        tcpClient.Dispose();
    }

    public override void SendAndWait(string type, Dictionary<string, object> data,
                                        string response, int timeout) {
        var sw = new Stopwatch();
        var remainingWait = timeout;
        sw.Start();

        SendMessage(type, data);
        while(remainingWait > 0) {
            // inspect queue for incoming read messages, wait on
            // loop wait handle until timeout or message received.
            // This will block other reads/writes on the loop thread
            // until the message is received or times out. This doesn't
            // modify the queue, so operations are still ordered and
            // subsequent waits will complete successfully

            // wait on EventLoop wait handle, signalled when event added to queue
            wait.Reset();
            wait.Wait(remainingWait);

            remainingWait = remainingWait - (int)sw.ElapsedMilliseconds;

            // NOTE: this is inefficient due to looping over the full
            // collection on every wake, but this queue shouldn't ever
            // get beyond ~10 events.
            foreach(IEventBase ev in eventQueue) {
                // try to convert, null events are writes or other
                // actions
                var msgEv = ev as MessageEvent<NetMsg>;

                if(msgEv == null) {
                    continue;
                }

                var json = JObject.Parse(msgEv.msg.msg);
                if(json.GetValue("type").Value<string>() == response) {
                    // once this chain returns, Loop continue on to 
                    // process all messages in queue
                    return;
                }
            }
        }

        throw new TimeoutException();
    }

    public override void HandleMessage(NetMsg msg) {
        JObject json = JObject.Parse(msg.msg);
        string type = json.GetValue("type").Value<string>();
        ReportNetworkMessage(msg, false);

        if(type.Contains("ERROR")) {
            throw new Exception("Error received from Host PC.");
        }

        if(type == "EXIT") {
            // FIXME: call QUIT
            throw new Exception("Error received from Host PC.");
        }
    }

    public override void SendMessage(string type, Dictionary<string, object> data) {
        DataPoint point = new DataPoint(type, DataReporter.TimeStamp(), data);
        string message = point.ToJSON();

        UnityEngine.Debug.Log("Sent Message");
        UnityEngine.Debug.Log(message);

        Byte[] bytes = System.Text.Encoding.UTF8.GetBytes(message+"\n");

        NetworkStream stream = GetWriteStream();
        stream.Write(bytes, 0, bytes.Length);
        ReportNetworkMessage(new NetMsg(message, DataReporter.TimeStamp()), true);
    }
}
