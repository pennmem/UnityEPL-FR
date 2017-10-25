
using UnityEngine;
using System.Collections;

public class CameraMove01 : MonoBehaviour {

	public float moveSpeed;
	public GameObject mainCamera;

	// Use this for initialization
	void Start () {
		transform.position = new Vector3 (0, 3, 20);
		mainCamera.transform.localPosition = new Vector3 ( 0f, 0f, 0f );
		mainCamera.transform.localRotation = Quaternion.Euler ( 15f, 180f, 0f );
		moveSpeed = -10f;
	}
	
	// Update is called once per frame
	void Update () {

		
	}

	void FixedUpdate()
	{
		MoveObj ();
		
		if (Input.GetKeyDown (KeyCode.A)) {
			ChangeView01();
		}
		
		if (Input.GetKeyDown (KeyCode.S)) {
			ChangeView02();
		}
	}
	
	
	void MoveObj() {		
		float moveAmount = Time.smoothDeltaTime * moveSpeed;
		transform.Translate ( 0f, 0f, moveAmount );	
	}



	void ChangeView01() {
		transform.position = new Vector3 (0, 3, 20);
		// x:0, y:1, z:52
		mainCamera.transform.localPosition = new Vector3 ( -8, 2, 0 );
		mainCamera.transform.localRotation = Quaternion.Euler (25, 90, 0);
		moveSpeed = -10f;
	}

	void ChangeView02() {
		transform.position = new Vector3 (0, 3, 20);
		// x:0, y:1, z:52
		mainCamera.transform.localPosition = new Vector3 ( 0f, 0f, 0f );
		mainCamera.transform.localRotation = Quaternion.Euler ( 15f, 180f, 0f );
		moveSpeed = -10f;
		
	}
}























