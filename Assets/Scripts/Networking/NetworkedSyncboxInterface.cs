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

//public abstract class IHostPC : EventLoop
//{
//    public abstract JObject SendAndWait(string type, Dictionary<string, object> data,
//                                        string response, int timeout);
//    public abstract void Connect();
//    public abstract void Disconnect();
//    public abstract void HandleMessage(NetMsg msg);
//    public abstract void SendMessage(string type, Dictionary<string, object> data);
//}

public interface ISyncBox {
    public void Init();
    public bool IsRunning();
    public void StartPulse();
    public void StopPulse();
    protected void Pulse();
}

public class NetworkedSyncboxInterface : IHostPC, ISyncBox {
    public InterfaceManager im;

    int messageTimeout = 1000;
    int heartbeatTimeout = 8000; // TODO: pull value from configuration

    private TcpClient tcpClient;
    private HostPCListener listener;
    private int heartbeatCount = 0;

    private const int PULSE_START_DELAY = 1000; // ms
    private const int TIME_BETWEEN_PULSES_MIN = 800;
    private const int TIME_BETWEEN_PULSES_MAX = 1200;

    private volatile bool stopped = true;

    // Changed
    public NetworkedSyncboxInterface(InterfaceManager _im) {
        im = _im;
        listener = new HostPCListener(this);
        Start();
    }

    ~NetworkedSyncboxInterface() {
        Disconnect();
    }

    // ------------
    // IHostPC
    // ------------

    // Changed
    public override void Connect() {
        tcpClient = new TcpClient();

        try {
            IAsyncResult result = tcpClient.BeginConnect("127.0.0.1", (int)im.GetSetting("freiburgSyncboxPort"), null, null);
            result.AsyncWaitHandle.WaitOne(messageTimeout);
            tcpClient.EndConnect(result);
        } catch (SocketException) {
            im.Do(new EventBase<string>(im.SetHostPCStatus, "ERROR"));
            throw new OperationCanceledException("Failed to connect to networked syncbox");
        }

        _ = listener.Listen(GetReadStream());
        SendAndWait("NSBOPENUSB", new Dictionary<string, object>(),
                    "NSBOPENUSB_OK", messageTimeout);

        // excepts if there's an issue with latency, else returns
        DoLatencyCheck();
    }

    // Changed
    public override void Disconnect() {
        listener?.CancelRead();
        tcpClient.Close();
        tcpClient.Dispose();
    }

    // Change to CSV?
    public override JObject SendAndWait(string type, Dictionary<string, object> data,
                                        string response, int timeout) {
        JObject json;
        var sw = new Stopwatch();
        var remainingWait = timeout;
        sw.Start();

        SendMessage(type, data);
        while (remainingWait > 0) {
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
            foreach (IEventBase ev in eventQueue) {
                // try to convert, null events are writes or other
                // actions
                var msgEv = ev as MessageEvent<NetMsg>;

                if (msgEv == null) { continue; }

                json = JObject.Parse(msgEv.msg.msg);
                if (json.GetValue("type").Value<string>() == response) {
                    // once this chain returns, Loop continue on to 
                    // process all messages in queue
                    return json;
                }
            }
        }

        throw new TimeoutException();
    }

    // Changed (only some exception text)
    public override void HandleMessage(NetMsg msg) {
        JObject json = JObject.Parse(msg.msg);
        string type = json.GetValue("type").Value<string>();
        ReportMessage(msg, false);

        if (type.Contains("ERROR")) {
            throw new Exception("Error received from Networked Syncbox.");
        }

        if (type == "EXIT") {
            // FIXME: call QUIT
            throw new Exception("Error received from Networked Syncbox.");
        }
    }

    // Change to CSV?
    public override void SendMessage(string type, Dictionary<string, object> data) {
        DataPoint point = new DataPoint(type, DataReporter.TimeStamp(), data);
        string message = point.ToJSON();

        UnityEngine.Debug.Log("Sent Message");
        UnityEngine.Debug.Log(message);

        Byte[] bytes = System.Text.Encoding.UTF8.GetBytes(message + "\n");

        NetworkStream stream = GetWriteStream();
        stream.Write(bytes, 0, bytes.Length);
        ReportMessage(new NetMsg(message, DataReporter.TimeStamp()), true);
    }

    // ------------
    // ISyncBox
    // ------------

    public void Init() {
        StopPulse();
        Disconnect();
        Do(new EventBase(Connect));
    }

    public bool IsRunning() {
        return !stopped;
    }

    public void StartPulse() {
        if (!IsRunning()) {
            stopped = false;
            DoIn(new EventBase(Pulse), PULSE_START_DELAY);
        }
    }

    public void StopPulse() {
        stopped = true;
    }

    protected void Pulse() {
        if (!stopped) {
            // Send a pulse
            im.Do(new EventBase<string, Dictionary<string, object>, DateTime>(
                im.ReportEvent, "syncPulse", null, DataReporter.TimeStamp()));

            SendMessage("NSBSYNCPULSE", new());

            // Wait a random interval between min and max
            int timeBetweenPulses = (int)(TIME_BETWEEN_PULSES_MIN + (int)(InterfaceManager.rnd.Value.NextDouble() * (TIME_BETWEEN_PULSES_MAX - TIME_BETWEEN_PULSES_MIN)));
            DoIn(new EventBase(Pulse), timeBetweenPulses);
        }
    }

    // ------------
    // NetworkedInterface
    // ------------

    protected void Heartbeat() {
        var data = new Dictionary<string, object>();
        data.Add("count", heartbeatCount);
        heartbeatCount++;
        SendAndWait("HEARTBEAT", data, "HEARTBEAT_OK", heartbeatTimeout);
    }

    protected void DoLatencyCheck() {
        // except if latency is unacceptable
        Stopwatch sw = new Stopwatch();
        float[] delay = new float[20];

        // send 20 heartbeats, except if max latency is out of tolerance
        for (int i = 0; i < 20; i++) {
            sw.Restart();
            Heartbeat();
            sw.Stop();

            // calculate manually to have sub millisecond resolution,
            // as ElapsedMilliseconds returns an integer number of
            // milliseconds.
            delay[i] = sw.ElapsedTicks * (1000f / Stopwatch.Frequency);

            if (delay[i] > 20) {
                throw new TimeoutException("Network Latency too large.");
            }

            // send new heartbeat every 50 ms
            Thread.Sleep(50 - (int)delay[i]);
        }

        float max = delay.Max();
        float mean = delay.Sum() / delay.Length;

        // the maximum resolution of the timer in nanoseconds
        long acc = (1000L * 1000L * 1000L) / Stopwatch.Frequency;

        Dictionary<string, object> dict = new Dictionary<string, object>();
        dict.Add("max_latency", max);
        dict.Add("mean_latency", mean);
        dict.Add("accuracy", acc);

        im.Do(new EventBase<string, Dictionary<string, object>>(im.ReportEvent, "latency check", dict));
    }

    protected void ReportMessage(NetMsg msg, bool sent) {
        Dictionary<string, object> messageDataDict = new Dictionary<string, object>();
        messageDataDict.Add("message", msg.msg);
        messageDataDict.Add("sent", sent);

        im.Do(new EventBase<string, Dictionary<string, object>, DateTime>(
            im.ReportEvent, "network", messageDataDict, msg.time));
    }

    protected NetworkStream GetWriteStream() {
        // only one writer can be active at a time
        if (tcpClient == null) {
            throw new InvalidOperationException("Socket not initialized.");
        }

        return tcpClient.GetStream();
    }

    protected NetworkStream GetReadStream() {
        // only one reader can be active at a time
        if (tcpClient == null) {
            throw new InvalidOperationException("Socket not initialized.");
        }

        return tcpClient.GetStream();
    }
}

public abstract class NetworkInterface : EventLoop {
    protected InterfaceManager im;

    private int messageTimeout = 1000;
    private int heartbeatTimeout = 8000;

    private TcpClient tcpClient;
    private HostPCListener listener;
    private int heartbeatCount = 0;

    NetworkInterface(InterfaceManager _im) {
        im = _im;
    }

    public abstract JObject SendAndWait(string type, Dictionary<string, object> data,
                                        string response, int timeout);
    public abstract void Connect();
    public abstract void Disconnect();
    public abstract void HandleMessage(NetMsg msg);
    public abstract void SendMessage(string type, Dictionary<string, object> data);

    protected void Heartbeat() {
        var data = new Dictionary<string, object>();
        data.Add("count", heartbeatCount);
        heartbeatCount++;
        SendAndWait("HEARTBEAT", data, "HEARTBEAT_OK", heartbeatTimeout);
    }

    protected void DoLatencyCheck() {
        // except if latency is unacceptable
        Stopwatch sw = new Stopwatch();
        float[] delay = new float[20];

        // send 20 heartbeats, except if max latency is out of tolerance
        for (int i = 0; i < 20; i++) {
            sw.Restart();
            Heartbeat();
            sw.Stop();

            // calculate manually to have sub millisecond resolution,
            // as ElapsedMilliseconds returns an integer number of
            // milliseconds.
            delay[i] = sw.ElapsedTicks * (1000f / Stopwatch.Frequency);

            if (delay[i] > 20) {
                throw new TimeoutException("Network Latency too large.");
            }

            // send new heartbeat every 50 ms
            Thread.Sleep(50 - (int)delay[i]);
        }

        float max = delay.Max();
        float mean = delay.Sum() / delay.Length;

        // the maximum resolution of the timer in nanoseconds
        long acc = (1000L * 1000L * 1000L) / Stopwatch.Frequency;

        Dictionary<string, object> dict = new Dictionary<string, object>();
        dict.Add("max_latency", max);
        dict.Add("mean_latency", mean);
        dict.Add("accuracy", acc);

        im.Do(new EventBase<string, Dictionary<string, object>>(im.ReportEvent, "latency check", dict));
    }

    protected void ReportMessage(NetMsg msg, bool sent) {
        Dictionary<string, object> messageDataDict = new Dictionary<string, object>();
        messageDataDict.Add("message", msg.msg);
        messageDataDict.Add("sent", sent);

        im.Do(new EventBase<string, Dictionary<string, object>, DateTime>(
            im.ReportEvent, "network", messageDataDict, msg.time));
    }

    protected NetworkStream GetWriteStream() {
        // only one writer can be active at a time
        if (tcpClient == null) {
            throw new InvalidOperationException("Socket not initialized.");
        }

        return tcpClient.GetStream();
    }

    protected NetworkStream GetReadStream() {
        // only one reader can be active at a time
        if (tcpClient == null) {
            throw new InvalidOperationException("Socket not initialized.");
        }

        return tcpClient.GetStream();
    }
}
