using UnityEngine;
using System.Collections;

public class Crystal : MonoBehaviour {

	Vector3 rotation = new Vector3(0, 0, 0);

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
        //rotates around on the y axis slowly
		rotation.y += 2;
		transform.localEulerAngles = rotation;
	}
}
