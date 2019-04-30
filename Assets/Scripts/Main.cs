using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Main : MonoBehaviour {

    public EventSystem eventSystem;
    public GraphicRaycaster graphicRaycaster;
    // Use this for initialization
    void Start () {
		
	}
	
    public void OnClick()
    {
        FreeCameraController.Instance.ResetCamera();
    }
}
