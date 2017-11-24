using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ImmersiveObserverExperiment {
    [RequireComponent(typeof(RectTransform))]
    public class QItem : MonoBehaviour {


        void Start() {

        }

        void Update() {

        }


        public virtual void Init(string[] parameters) { }
        public virtual void Reset() { }

        public virtual int GetResult() {
            return -1;
        }
    }
}
