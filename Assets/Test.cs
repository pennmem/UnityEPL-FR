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
			string message = "0.1;SESSION;{\"session\":1,\"subject\":\"{{ subject }}\",\"name\":\"{{ experiment }}\", \"version\":\"3.1.0\"}";
			Debug.Log (message);
			Debug.Log (zmqSocket.TrySendFrame (message, more: false));
		}
	}

}