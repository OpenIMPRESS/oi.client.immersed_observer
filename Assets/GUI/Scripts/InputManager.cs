using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;


namespace ImmersiveObserverExperiment {
    public class InputManager : MonoBehaviour {

        public static InputManager instance { get { return _instance; } }
        private static InputManager _instance = null;

        public Wacki.LaserPointerInputModule laserInputModule;
        public StandaloneInputModule standaloneInputModule;
        public EventSystem eventSystem;
        public BaseInput baseInput;

        public bool toLaser;
        public bool toStandalone;

        public Camera ScreenUICam;

        private void Awake() {
            if (_instance != null) {
                Debug.LogWarning("Trying to instantiate multiple InputManager.");
                DestroyImmediate(this.gameObject);
            }
            _instance = this;
        }
        // Use this for initialization
        void Start() {
            toStandalone = true;
        }

        // Update is called once per frame
        void Update() {
            if (toLaser) {
                UseVR();
                toLaser = false;
            }

            if (toStandalone) {
                UseScreen();
                toStandalone = false;
            }
        }

        public void UseScreen() {
            laserInputModule.DeactivateModule();
            laserInputModule.enabled = false;
            laserInputModule.BindToOtherUICam(ScreenUICam);
            standaloneInputModule.enabled = true;
            standaloneInputModule.ActivateModule();
            ScreenUICam.depth = 1;
        }

        public void UseVR() {
            standaloneInputModule.DeactivateModule();
            standaloneInputModule.enabled = false;
            laserInputModule.enabled = true;
            laserInputModule.ActivateModule();
            laserInputModule.BindToVRUICam();
            ScreenUICam.depth = -3;
        }
    }
}