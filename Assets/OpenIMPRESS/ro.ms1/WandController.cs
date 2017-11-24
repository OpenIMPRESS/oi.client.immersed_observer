using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class WandController : MonoBehaviour {
    private Valve.VR.EVRButtonId gripButton = Valve.VR.EVRButtonId.k_EButton_Grip;
    private Valve.VR.EVRButtonId triggerButton = Valve.VR.EVRButtonId.k_EButton_SteamVR_Trigger;
    private Valve.VR.EVRButtonId padButton = Valve.VR.EVRButtonId.k_EButton_SteamVR_Touchpad;

    private SteamVR_Controller.Device controller { get { return SteamVR_Controller.Input((int)trackedObj.index); } }
    private SteamVR_TrackedObject trackedObj;


    public Vector3 prevPos;
    public Vector3 deltaPos = new Vector3();
    public bool interacting = false;

    public bool thumbPress = false;

//    private LineDrawCapture lineDrawCapture;

    // Use this for initialization
    void Awake() {
        trackedObj = GetComponent<SteamVR_TrackedObject>();
//        lineDrawCapture = GetComponentInChildren<LineDrawCapture>();
    }

    // Update is called once per frame
    void Update() {
        if (controller == null) {
            Debug.Log("Controller not initialized");
            return;
        }

        if (controller.GetPressDown(triggerButton)) {
//            if (lineDrawCapture != null)
//                lineDrawCapture.SetButtonDown(true);
        }

        if (controller.GetPressUp(triggerButton)) {
 //           if (lineDrawCapture != null)
 //               lineDrawCapture.SetButtonDown(false);
        }
    }

    public void UpdateDrag() {
        if (!gameObject.activeInHierarchy) return;
        if (controller.GetPressDown(gripButton) && !interacting) {
            interacting = true;
        }

        if (controller.GetPressUp(gripButton) && interacting) {
            interacting = false;
        }

        if (controller.GetPressDown(padButton) && !thumbPress) {
            thumbPress = true;
        }

        if (controller.GetPressUp(padButton) && thumbPress) {
            thumbPress = false;
        }

        if (interacting) {
            deltaPos = transform.position - prevPos;
        }


    }

    public void SavePos() {
        prevPos = transform.position;
    }

    private void StartInteraction() {
        interacting = true;
        prevPos = transform.position;
    }

    private void EndInteraction() {
        interacting = false;
    }
}
