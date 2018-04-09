using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InstantiatePrefabOnSky : MonoBehaviour {
	public List<GameObject> prefabs = new List<GameObject>();

	public void OnClick()
	{
		int index = Random.Range(0, prefabs.Count);
		var obj = Instantiate(prefabs[index]) as GameObject;
		obj.transform.position = new Vector3(Random.Range(0f, 3f), Random.Range(3f, 10f), Random.Range(0f, 3f));
	}

}
