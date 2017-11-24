using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ImmersiveObserverExperiment {
    public class QItemLikert5 : QItem {

        public Text questionText;
        public Text labelOneText;
        public Text labelFiveText;

        public override void Init(string[] parameters) {
            string questionV = parameters[1].Trim();
            string aLabelV = parameters[2].Trim();
            string bLabelV = parameters[3].Trim();
            questionText.text = questionV;
            labelOneText.text = aLabelV;
            labelFiveText.text = bLabelV;
        }

        void Start() {
            if (questionText == null || labelOneText == null || labelFiveText == null) {
                Debug.LogError("QItemOptions2 component not configured properly.");
            } else {
                Reset();
            }

        }

        public override void Reset() {
            Toggle[] options = GetComponentsInChildren<Toggle>();
            foreach (Toggle option in options) {
                option.isOn = false;
            }
        }

        void Update() {

        }

        public override int GetResult() {
            Toggle[] options = GetComponentsInChildren<Toggle>();
            int n_true = 0;
            string lastTrueName = "";
            foreach (Toggle option in options) {
                if (option.isOn) {
                    n_true++;
                    lastTrueName = option.gameObject.name;
                }
            }

            if (n_true == 0) {
                return -1;
            } else if (n_true == 1) {
                return int.Parse(lastTrueName);
            }

            Debug.LogError("It shouldn't be possible to select multiple options.");
            return -1;
        }

    }
}