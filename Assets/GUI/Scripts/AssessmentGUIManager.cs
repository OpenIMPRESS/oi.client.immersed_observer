using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using oi.plugin.rgbd;

namespace ImmersiveObserverExperiment {
    public class AssessmentGUIManager : MonoBehaviour {

        public enum AssessmentState {
            HIDDEN,
            INIT,
            REPLAY,
            ASSESSMENT,
            WAIT_FOR_NEXT
        };

        private AssessmentState _state;
        public QItemPanel itemPanel;
        public Button pauseReplayButton;
        public Button continueButton;

        public ExperimenterGUIMangager experimentGUI;

        private int[] sliceOrder;
        private int nextSlice;

        public bool randomize;
        public RGBDControl rgbdControl;
        private Assessment _assessmentResult;

        public Transform GUIParentTransformVR;
        public Transform GUIParentTransform2D;

        private void Awake() {
            itemPanel.assessmentGUIManager = this;
            SetGUI(AssessmentState.HIDDEN);
            rgbdControl.RGBDStreamEvent += OnRGBDStreamEvent;
        }

        void ConfigureVR() {
            Debug.Log("Configure VR");
            VRWorldDragger.instance.SetToReview();
            transform.parent = GUIParentTransformVR;
            RectTransform guiRect = transform.GetComponent<RectTransform>();
            guiRect.localPosition = new Vector3(0.0f, 0.0f, 0.32f);
            guiRect.localRotation = Quaternion.identity;
            guiRect.localScale = new Vector3(0.002f, 0.002f, 0.002f);
        }

        void Configure2D() {
            Debug.Log("Configure 2D");
            VRWorldDragger.instance.Reset();
            transform.parent = GUIParentTransform2D;
            RectTransform guiRect = transform.GetComponent<RectTransform>();
            guiRect.localPosition = new Vector3(0.0f, 0.0f, 0.32f);
            guiRect.localRotation = Quaternion.identity;
            guiRect.localScale = new Vector3(0.001f, 0.001f, 0.001f);
        }

        void Start() {
        }

        private void SetGUI(AssessmentState newState) {
            pauseReplayButton.gameObject.SetActive(false);
            continueButton.gameObject.SetActive(false);
            itemPanel.gameObject.SetActive(false);

            switch (newState) {
                case AssessmentState.HIDDEN:
                    break;
                case AssessmentState.INIT:
                    continueButton.gameObject.SetActive(true);
                    break;
                case AssessmentState.REPLAY:
                    continueButton.gameObject.SetActive(false);
                    itemPanel.gameObject.SetActive(false);
                    pauseReplayButton.gameObject.SetActive(true);
                    pauseReplayButton.GetComponentInChildren<Text>().text = "PAUSE";
                    break;
                case AssessmentState.ASSESSMENT:
                    continueButton.gameObject.SetActive(false);
                    pauseReplayButton.gameObject.SetActive(true);
                    pauseReplayButton.GetComponentInChildren<Text>().text = "REPLAY";
                    itemPanel.gameObject.SetActive(true);
                    break;
                case AssessmentState.WAIT_FOR_NEXT:
                    itemPanel.gameObject.SetActive(false);
                    pauseReplayButton.gameObject.SetActive(false);
                    continueButton.gameObject.SetActive(true);
                    break;
                default:
                    break;

            }
            _state = newState;
        }

        public void OnRGBDStreamEvent(object sender, RGBDStreamEventArgs ev) {
            if (_state == AssessmentState.REPLAY && ev.Message == "REPLAY_STOPPED") {
                SetGUI(AssessmentState.ASSESSMENT);
            }
        }

        public void StartAssessment(string aid, string view, string type, Recording r) {
            _assessmentResult = new Assessment(aid, view, type, r);
            _assessmentResult.qspec = itemPanel.qSpec.text;

            if (view == "VR") {
                ConfigureVR();
            } else {
                Configure2D();
            }

            List<int> _sliceOrder = new List<int>();
            for (int i = 0; i < r.slices.Length; i++) {
                _sliceOrder.Add(i);
            }

            if (randomize) { 
                System.Random rnd = new System.Random();
                sliceOrder = _sliceOrder.OrderBy(x => rnd.Next()).ToArray();
            } else {
                sliceOrder = _sliceOrder.ToArray();
            }
            nextSlice = 0;
            SetGUI(AssessmentState.INIT);
        }

        public void OnSubmit(int[] result) {
            // TODO: store result;
            string sliceId = _assessmentResult.recording.slices[sliceOrder[nextSlice]].GetSliceID();
            Debug.Log("Result for slice: " + sliceId);
            AssessmentEntry a = new AssessmentEntry(sliceId, result);
            _assessmentResult.AddEntry(a);
            nextSlice++;
            SetGUI(AssessmentState.WAIT_FOR_NEXT);
        }

        public void OnContinue() {
            if (nextSlice >= _assessmentResult.recording.slices.Length) {
                SetGUI(AssessmentState.HIDDEN);
                experimentGUI.OnAssessmentCompleted(_assessmentResult);
            } else { 
                int sliceIdx = sliceOrder[nextSlice];
                Slice slice = _assessmentResult.recording.slices[sliceIdx];
                rgbdControl.PlaySlice(_assessmentResult.recording.rid, slice.tStart, slice.tEnd);
                SetGUI(AssessmentState.REPLAY);
            }
        }

        public void OnPauseReplay() {
            if (_state == AssessmentState.REPLAY) {
                rgbdControl.StopPlay();
                SetGUI(AssessmentState.ASSESSMENT);
            } else if (_state == AssessmentState.ASSESSMENT) {
                OnContinue();
            }
        }
    }
}