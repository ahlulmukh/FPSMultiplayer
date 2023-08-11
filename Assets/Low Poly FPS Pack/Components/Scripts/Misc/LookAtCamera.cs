﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LookAtCamera : MonoBehaviour {

	private void Start () 
	{
		//Fix inverted scale issue
		gameObject.transform.localScale = 
			new Vector3 (-1, 1, 1);
	}
		
	private void Update () 
	{
		//Object always face camera
		transform.LookAt (Camera.main.transform);
	}
}