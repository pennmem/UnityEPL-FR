using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NetMQ;

public class Test : MonoBehaviour
{
	
	//this will be executed after the test session is complete to validate
	//the output from the session.
	public void RunTests()
	{
		//use simple reflection to run each of the methods beginning with "test"
		//these methods will throw errors if they discover something wrong
	}



	private void TestNondecreasingTimings()
	{
		//perform test
	}

	private void TestWordpoolCompleteness()
	{
		//perform test
	}

	private void TestDistractorLogic()
	{
		//perform test
	}

	private void TestKeystrokeDistractorMatching()
	{
		//perform test
	}

	private void TestRecordingCount()
	{
		//perform test
	}

	private void TestNonsilentRecordings()
	{
		//perform test
	}

	private void TestMatchesRamtransferInterface()
	{
		//perform test
	}




	private NetMQ.Sockets.PairSocket zmqSocket;

	void OnApplicationQuit()
	{
		if (zmqSocket != null)
			zmqSocket.Close ();
		NetMQConfig.Cleanup();
	}

	void Start ()
	{
		zmqSocket = new NetMQ.Sockets.PairSocket ();
		zmqSocket.Bind ("tcp://*:8889");
	}

	void Update()
	{
		//////////////////////////////////////////////////receive
		string receivedMessage = "";
		zmqSocket.TryReceiveFrameString(out receivedMessage);
		if (receivedMessage != null)
		{
			Debug.Log ("received: " + receivedMessage.ToString ());
		}




		/////////////////////////////////////////////////send
		if (Input.GetKeyDown (KeyCode.Space))
		{
			DataPoint connected = new DataPoint ("CONNECTED", DataReporter.RealWorldTime (), new Dictionary<string, string> ());
			string message = connected.ToJSON ();
			Debug.Log (message);
			Debug.Log (zmqSocket.TrySendFrame (message, more: false));
			InvokeRepeating ("SendHeartbeat", 0, 1);
		}

		if (Input.GetKeyDown (KeyCode.N))
		{
			System.Collections.Generic.Dictionary<string, string> sessionData = new Dictionary<string, string>();
			sessionData.Add ("name", "FR1");
			sessionData.Add ("version", Application.version);
			sessionData.Add ("subject", "R1337E");
			sessionData.Add ("session_number", "0");
			DataPoint sessionDataPoint = new DataPoint ("SESSION", DataReporter.RealWorldTime (), sessionData);
			string message = sessionDataPoint.ToJSON ();
			Debug.Log (message);
			Debug.Log (zmqSocket.TrySendFrame (message, more: false));

		}
	}

	private void SendHeartbeat()
	{
		DataPoint sessionDataPoint = new DataPoint ("HEARTBEAT", DataReporter.RealWorldTime (), null);
		SendMessageToRamulator (sessionDataPoint.ToJSON ());
	}

	private void SendMessageToRamulator(string message)
	{
		bool wouldNotHaveBlocked = zmqSocket.TrySendFrame(message, more: false);
		Debug.Log ("Tried to send a message: " + message + " \nWouldNotHaveBlocked: " + wouldNotHaveBlocked.ToString());
	}
}