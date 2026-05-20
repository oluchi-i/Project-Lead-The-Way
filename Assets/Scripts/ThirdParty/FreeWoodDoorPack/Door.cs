using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace DoorScript
{
	[RequireComponent(typeof(AudioSource))]


public class Door : MonoBehaviour {
	public bool open;
	public float smooth = 1.0f;
	float DoorOpenAngle = -90.0f;
    float DoorCloseAngle = 0.0f;
	public AudioSource asource;
	public AudioClip openDoor,closeDoor;
	[Range(0f, 2f)]
	public float soundVolume = 1.5f;
	[Range(0f, 1f)]
	public float spatialBlend = 0.2f;
	// Use this for initialization
	void Start () {
		asource = GetComponent<AudioSource> ();
		asource.playOnAwake = false;
		asource.volume = Mathf.Clamp01(soundVolume);
		asource.spatialBlend = spatialBlend;
	}
	
	// Update is called once per frame
	void Update () {
		if (open)
		{
            var target = Quaternion.Euler (0, DoorOpenAngle, 0);
            transform.localRotation = Quaternion.Slerp(transform.localRotation, target, Time.deltaTime * 5 * smooth);
	
		}
		else
		{
            var target1= Quaternion.Euler (0, DoorCloseAngle, 0);
            transform.localRotation = Quaternion.Slerp(transform.localRotation, target1, Time.deltaTime * 5 * smooth);
	
		}  
	}

	public void OpenDoor(){
		open =!open;
		var clip = open?openDoor:closeDoor;
		if (clip != null)
			asource.PlayOneShot (clip, soundVolume);
	}
}
}
