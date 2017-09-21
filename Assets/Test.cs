using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NetMQ;

public class Test : MonoBehaviour
{
	void Start ()
	{
		NetMQ.Sockets.PairSocket zmqSocket = new NetMQ.Sockets.PairSocket ();
		zmqSocket.Bind ("tcp://*:8889");
		string message = "0.1;SESSION;{\"session\":1,\"subject\":\"{{ subject }}\",\"name\":\"{{ experiment }}\", \"version\":\"3.1.0\"}";
		Debug.Log (message);
		Debug.Log(zmqSocket.TrySendFrame(message, more: false));
	}
}