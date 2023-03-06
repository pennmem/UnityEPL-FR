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
using System.Runtime.Remoting.Messaging;
using NetMQ;

public interface ISyncBox {
    void Init();
    bool IsRunning();
    void StartPulse();
    void StopPulse();
}

public class NetworkedSyncboxInterface : NetworkInterface, ISyncBox {
    private const int PULSE_START_DELAY = 1000; // ms
    private const int TIME_BETWEEN_PULSES_MIN = 800;
    private const int TIME_BETWEEN_PULSES_MAX = 1200;

    private volatile bool stopped = true;

    // Changed
    public NetworkedSyncboxInterface(InterfaceManager _im) : base(_im) {
    }

    ~NetworkedSyncboxInterface() {
        Disconnect();
    }

    // ------------
    // NetworkInterface
    // ------------

    // Changed
    public override void Connect() {
        tcpClient = new TcpClient();

        try {
            IAsyncResult result = tcpClient.BeginConnect("127.0.0.1", (int)im.GetSetting("networkedSyncboxPort"), null, null);
            result.AsyncWaitHandle.WaitOne(messageTimeout);
            tcpClient.EndConnect(result);
        } catch (SocketException) {
            im.Do(new EventBase<string>(im.SetHostPCStatus, "ERROR"));
            throw new OperationCanceledException("Failed to connect to networked syncbox");
        }

        _ = listener.Listen(GetReadStream());
        SendAndWait("NSBOPENUSB", new(), "NSBOPENUSB_OK", messageTimeout);

        // excepts if there's an issue with latency, else returns
        DoLatencyCheck("NSB");
    }

    // Changed
    public override void Disconnect() {
        if (tcpClient != null && !tcpClient.Connected) {
            SendAndWait("NSBCLOSEUSB", new(), "NSBCLOSEUSB_OK", messageTimeout);
        }
        listener?.CancelRead();
        tcpClient?.Close();
        tcpClient?.Dispose();
    }

    // Change (for CSV)
    public override void SendAndWait(string type, Dictionary<string, object> data,
                                        string response, int timeout) {
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

                if (response == msgEv.msg.msg.Trim().Split(",")[0]) {
                    // once this chain returns, Loop continue on to 
                    // process all messages in queue
                    return;
                }
            }
        }

        throw new TimeoutException();
    }

    // Changed (for CSV)
    public override void HandleMessage(NetMsg msg) {
        string type = msg.msg.Trim().Split(",")[0];
        ReportNetworkMessage(msg, false);

        if (type.Contains("ERROR")) {
            throw new Exception("Error received from Networked Syncbox.");
        }

        if (type == "EXIT") {
            // FIXME: call QUIT
            throw new Exception("Error received from Networked Syncbox.");
        }
    }

    // Change (for CSV)
    public override void SendMessage(string type, Dictionary<string, object> data) {
        string message = type;

        UnityEngine.Debug.Log("Sent Message");
        UnityEngine.Debug.Log(message);

        Byte[] bytes = System.Text.Encoding.UTF8.GetBytes(message + "\n");

        NetworkStream stream = GetWriteStream();
        stream.Write(bytes, 0, bytes.Length);
        ReportNetworkMessage(new NetMsg(message, DataReporter.TimeStamp()), true);
    }

    // ------------
    // ISyncBox
    // ------------

    public void Init() {
        Do(new EventBase(() => InitHelper()));
    }
    public void InitHelper() {
        StopPulse();
        Connect();
    }

    // TODO: JPB: This is technically a race condition (should be a DoGet)
    public bool IsRunning() {
        return !stopped;
    }

    public void StartPulse() {
        Do(new EventBase(() => StartPulseHandler()));
    }
    public void StartPulseHandler() {
        if (!IsRunning()) {
            stopped = false;
            DoIn(new EventBase(Pulse), PULSE_START_DELAY);
        }
    }

    public void StopPulse() {
        Do(new EventBase(() => StopPulseHelper()));
    }
    public void StopPulseHelper() {
        stopped = true;
    }

    protected void Pulse() {
        if (!stopped) {
            // Send a pulse
            im.SendHostPCMessage("syncPulse", new());
            SendMessage("NSBSYNCPULSE", new());

            // Wait a random interval between min and max
            int timeBetweenPulses = (int)(TIME_BETWEEN_PULSES_MIN + (int)(InterfaceManager.rnd.Value.NextDouble() * (TIME_BETWEEN_PULSES_MAX - TIME_BETWEEN_PULSES_MIN)));
            DoIn(new EventBase(Pulse), timeBetweenPulses);
        }
    }
}

