using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using oi.plugin.rgbd;
using System.Linq;

namespace ImmersiveObserverExperiment {

    public enum ExperimentGUIState {
        MAIN,
        RECORDING_INIT,
        RECORDING_REC,
        RECORDING_DONE,
        ASSESSMENT_INIT,
        ASSESSMENT_VR,
        ASSESSMENT_2D,
        ASSESSMENT_DONE
    };

    public class ExperimenterGUIMangager : MonoBehaviour {
        public static ExperimenterGUIMangager instance { get { return _instance; } }
        private static ExperimenterGUIMangager _instance = null;

        public ulong sliceCrop = 5000;
        public ulong sliceLength = 30000;

        public AssessmentGUIManager assessmentGUIManager;
        private ExperimentGUIState eguistate;
        public RGBDControl rgbdControl;
        private string dataFolder;
        private string recSuffix = ".rec.txt";
        private string assSuffix = ".ass.txt";

        public SteamVR_TrackedObject[] requiredTrackers;

        public Button backButton;
        public Button newAssessmentButton;
        public Button assessmentStartButton;
        public Button newRecordingButton;
        public Button startRecordingButton;
        public Button stopRecordingButton;
        public Text stateLabel;
        public Text recInfo;
        public Dropdown selectRecordingDropdown;
        public ToggleGroup assessmentTypeGroup;
        public ToggleGroup assessmentViewGroup;

        private Recording recording;
        private Recording _assessment_recording;
        private string _rid;
        private string _aid;
        private List<string> _recFiles;

        private void Awake() {
            if (_instance != null) {
                Debug.LogWarning("Trying to instantiate multiple VRWorldDragger.");
                DestroyImmediate(this.gameObject);
            }
            _instance = this;

            dataFolder = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop)
                + System.IO.Path.DirectorySeparatorChar + "ImmObsData" + System.IO.Path.DirectorySeparatorChar;
            Debug.Log(dataFolder);
            if (!System.IO.Directory.Exists(dataFolder))
                System.IO.Directory.CreateDirectory(dataFolder);
        }

        void Start() {
            SetGUI(ExperimentGUIState.MAIN);
            recording = null;
        }

        private void SetGUI(ExperimentGUIState newState) {
            backButton.gameObject.SetActive(false);
            newAssessmentButton.gameObject.SetActive(false);
            assessmentStartButton.gameObject.SetActive(false);
            newRecordingButton.gameObject.SetActive(false);
            startRecordingButton.gameObject.SetActive(false);
            stopRecordingButton.gameObject.SetActive(false);
            assessmentTypeGroup.gameObject.SetActive(false);
            assessmentViewGroup.gameObject.SetActive(false);
            stateLabel.transform.parent.gameObject.SetActive(false);
            selectRecordingDropdown.gameObject.SetActive(false);
            stateLabel.text = "";
            recInfo.text = "";
            recInfo.transform.parent.gameObject.SetActive(false);
            backButton.transform.GetComponentInChildren<Text>().text = "BACK";

            _assessment_recording = null;
            _recFiles = null;

            if (newState != ExperimentGUIState.ASSESSMENT_VR) {
                InputManager.instance.UseScreen();
            } else {
                InputManager.instance.UseVR();
            }

            switch (newState) {
                case ExperimentGUIState.MAIN:
                    stateLabel.text = "Welcome";
                    newAssessmentButton.gameObject.SetActive(true);
                    newRecordingButton.gameObject.SetActive(true);
                    stateLabel.transform.parent.gameObject.SetActive(true);
                    backButton.gameObject.SetActive(true);
                    backButton.transform.GetComponentInChildren<Text>().text = "EXIT";
                    break;
                case ExperimentGUIState.RECORDING_INIT:
                    stateLabel.text = "New Recording: " + _rid;
                    startRecordingButton.gameObject.SetActive(true);
                    backButton.gameObject.SetActive(true);
                    stateLabel.transform.parent.gameObject.SetActive(true);
                    break;
                case ExperimentGUIState.RECORDING_REC:
                    if (recording != null) stateLabel.text = "Recording: " + recording.rid;
                    else stateLabel.text = "Recording: ????";
                    stopRecordingButton.gameObject.SetActive(true);
                    stateLabel.transform.parent.gameObject.SetActive(true);
                    break;
                case ExperimentGUIState.RECORDING_DONE:
                    if (recording != null) stateLabel.text = "Recording Completed: " + recording.rid;
                    else stateLabel.text = "Recording Completed: ????";
                    backButton.gameObject.SetActive(true);
                    stateLabel.transform.parent.gameObject.SetActive(true);
                    break;
                case ExperimentGUIState.ASSESSMENT_INIT:
                    stateLabel.text = "New Assessment: " + _aid;
                    selectRecordingDropdown.gameObject.SetActive(true);
                    assessmentTypeGroup.gameObject.SetActive(true);
                    assessmentViewGroup.gameObject.SetActive(true);
                    assessmentStartButton.gameObject.SetActive(true);
                    stateLabel.transform.parent.gameObject.SetActive(true);
                    backButton.gameObject.SetActive(true);
                    recInfo.transform.parent.gameObject.SetActive(true);
                    _recFiles = ListRecordings();
                    selectRecordingDropdown.AddOptions(_recFiles);
                    OnRecordingSelected();
                    break;
                case ExperimentGUIState.ASSESSMENT_VR:
                    break;
                case ExperimentGUIState.ASSESSMENT_2D:
                    break;
                case ExperimentGUIState.ASSESSMENT_DONE:
                    stateLabel.text = "Assessment Completed";
                    backButton.gameObject.SetActive(true);
                    stateLabel.transform.parent.gameObject.SetActive(true);
                    break;
                default:
                    break;

            }
            eguistate = newState;
        }

        void Update() {
        }

        // AID is system time, but all other timestamps are UTC
        private string GenerateNewRecordingID() {
            System.DateTime dateTime = System.DateTime.Now;
            return "R" + dateTime.ToString("yyyyMMddHHmmss");
        }

        // PID is system time, but all other timestamps are UTC
        private string GenerateNewAssessmentID() {
            System.DateTime dateTime = System.DateTime.Now;
            return "A" + dateTime.ToString("yyyyMMddHHmmss");
        }

        public void StartRecording() {
            if (recording != null && !recording.closed) {
                Debug.LogError("Starting recording while having an unclosed recording.");
                StopRecording();
                return;
            }

            foreach (SteamVR_TrackedObject to in requiredTrackers) {
                if (!to.isActiveAndEnabled) {
                    Debug.LogWarning("Need tracker "+to.transform.name+" active before starting recording");
                    return;
                }
            }

            string pid;
            if (_rid != "") {
                pid = _rid;
                _rid = "";
            } else {
                pid = GenerateNewRecordingID();
            }

            ulong tStart = RGBDControl.NOW() + 520;
            recording = new Recording(pid, tStart);
            rgbdControl.StartRecording(tStart, pid);
            SetGUI(ExperimentGUIState.RECORDING_REC);
        }

        public void SaveRecording() {
            if (recording != null && recording.closed) {
                string data = recording.Serialize();
                if (recording.slices.Length > 0) {
                    string fileName = dataFolder + recording.rid + recSuffix;
                    System.IO.File.WriteAllText(fileName, data);
                    Debug.Log("Recording saved to " + fileName);
                } else {
                    Debug.LogWarning("Not saving recording with empty slices.");
                }
            } else {
                Debug.LogWarning("Cannot save no (unclosed) recording.");
            }
        }

        public Recording LoadRecording(string rid) {
            string[] lines = System.IO.File.ReadAllLines(dataFolder + rid + recSuffix);
            if (lines.Length > 0) {
                Debug.Log("Loading Recording: " + lines[0]);
                return recording = new Recording(lines[0]);
            } else {
                Debug.LogError("Failed to load recording: " + rid);
                return null;
            }
        }

        public List<string> ListRecordings() {
            string[] files = System.IO.Directory.GetFiles(dataFolder);
            List<string> recFiles = new List<string>();
            foreach (string file in files) {
                Debug.Log("FILE: " + file);
                if (file.EndsWith(recSuffix)) {
                    recFiles.Add(file.Substring(dataFolder.Length, file.Length - (dataFolder.Length + recSuffix.Length)));
                }
            }
            return recFiles;
        }


        public void StopRecording() {
            SetGUI(ExperimentGUIState.RECORDING_DONE);
            if (recording != null && !recording.closed) {
                ulong tEnd = RGBDControl.NOW() + 500;
                rgbdControl.StopRecording(tEnd);
                recording.CloseRecording(tEnd);
                SaveRecording();
            } else {
                Debug.LogWarning("Nothing to stop.");
            }
        }

        public void LogManualEvent(ulong t, int evtype) {
            if (recording != null && !recording.closed) {
                recording.AddManualEvent(t, evtype);
            } else {
                Debug.LogWarning("Cannot log event while not recording");
            }
        }

        public void OnNewAssessment() {
            Debug.Log("INIT ASSESSMENT");
            selectRecordingDropdown.ClearOptions();
            _aid = GenerateNewAssessmentID();
            rgbdControl.DisableDevice();
            SetGUI(ExperimentGUIState.ASSESSMENT_INIT);
        }

        public void OnStartAssessment() {
            string type = "";
            string view = "";
            foreach (Toggle typeToggle in assessmentTypeGroup.ActiveToggles()) {
                type = typeToggle.name;
            }
            foreach (Toggle viewToggle in assessmentViewGroup.ActiveToggles()) {
                view = viewToggle.name;
            }
            if (_assessment_recording == null || type == "" || view == "") {
                Debug.LogWarning("Select recording, type & view first.");
                return;
            } else

            Debug.Log(_assessment_recording.rid + " " + type + " " + view);
            string assessmentId = "";
            if (_aid == "") assessmentId = GenerateNewAssessmentID();
            else {
                assessmentId = _aid;
                _aid = "";
            }

            assessmentGUIManager.StartAssessment(assessmentId, view, type, _assessment_recording);

            if (view == "VR") {
                Debug.Log("START VR ASSESSMENT");
                SetGUI(ExperimentGUIState.ASSESSMENT_VR);
            } else if (view == "2D") {
                Debug.Log("START 2D ASSESSMENT");
                SetGUI(ExperimentGUIState.ASSESSMENT_2D);
            }
        }

        public void OnNewRecording() {
            Debug.Log("INIT RECORDING");
            _rid = GenerateNewRecordingID();
            rgbdControl.EnableDevice();
            SetGUI(ExperimentGUIState.RECORDING_INIT);
        }

        public void OnBack() {
            if (eguistate == ExperimentGUIState.MAIN) {
                Application.Quit();
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#endif
            } else {
                SetGUI(ExperimentGUIState.MAIN);
            }
        }

        public void OnAssessmentCompleted(Assessment ass) {
            string data = ass.Serialize();
            System.IO.File.WriteAllText(dataFolder + ass.assessmentId + assSuffix, data);
            SetGUI(ExperimentGUIState.ASSESSMENT_DONE);
        }

        public void OnRecordingSelected() {
            if (_recFiles == null || _recFiles.Count <= selectRecordingDropdown.value) {
                recInfo.text = "ERROR";
                Debug.LogError("Dropdown list out of sync.");
                return;
            }
            string _raid = _recFiles[selectRecordingDropdown.value];
            Debug.Log("Selected: "+ _raid);
            _assessment_recording = LoadRecording(_raid);

            recInfo.text  = "Recording "+_assessment_recording.rid+"\n";
            recInfo.text += "Total Duration " + (_assessment_recording.tEnd-_assessment_recording.tStart)/1000 + "s\n";
            recInfo.text += "Slices: " + _assessment_recording.slices.Length;
            if (_assessment_recording.slices.Length > 0) {
                recInfo.text += " @ " + (_assessment_recording.slices[0].tEnd - _assessment_recording.slices[0].tStart)+ "ms length";
            }
            recInfo.text += "\n";
            recInfo.text += "Events: " + _assessment_recording.manualEvents.Length + "\n";
        }

        public void OnApplicationQuit() {
            StopRecording();
        }

    }






















    public class Assessment {
        public string assessmentId { get; private set; }
        public string type { get; private set; }
        public string view { get; private set; }
        public Recording recording { get; private set; }
        public Dictionary<string, AssessmentEntry> assessments { get; private set; }
        public string qspec;

        public Assessment(string aid, string view, string type, Recording recording) {
            this.assessmentId = aid;
            this.view = view;
            this.type = type;
            this.recording = recording;
            this.assessments = new Dictionary<string, AssessmentEntry>();
        }

        public void AddEntry(AssessmentEntry e) {
            assessments.Add(e.sliceId, e);
        }

        public string Serialize() {
            string res = "";
            string rowPrefix = assessmentId + "," + view + "," + type;
            // Header:
            res += recording.rid + "\n" + qspec + "\n\n";
            for (int i = 0; i < recording.slices.Length; i++) {
                string sliceID = recording.slices[i].GetSliceID();
                if (!assessments.ContainsKey(sliceID)) {
                    Debug.LogWarning("Got no assessment for slice: "+sliceID);
                    continue;
                }
                AssessmentEntry sliceAssessment = assessments[sliceID];
                res += rowPrefix + "," + sliceID + "," + string.Join(",", sliceAssessment.qres.Select(x => x.ToString()).ToArray());
                res += "\n";
            }
            return res;
        }
    }

    public class AssessmentEntry {
        public int[] qres { get; private set; }
        public string sliceId { get; private set; }

        public AssessmentEntry(string id, int[] res) {
            this.sliceId = id;
            this.qres = res;
        }
    }

    public class Recording {
        public string rid { get; private set; }
        public ulong tStart { get; private set; }
        public ulong tEnd { get; private set; }
        public bool closed { get; private set; }
        public Slice[] slices;
        public ManualEvent[] manualEvents;

        private List<ManualEvent> _manualEvents;

        // Init from string
        public Recording(string serialized) {
            string[] tokens = serialized.Split(new char[] { '|' }, System.StringSplitOptions.RemoveEmptyEntries);
            this.rid = tokens[0];
            this.tStart = ulong.Parse(tokens[1]);
            this.tEnd = ulong.Parse(tokens[2]);

            List<Slice> _slices = new List<Slice>();
            if (tokens.Length >= 4) {
                string[] sliceTokens = tokens[3].Split(new char[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries);

                for (int s = 0; s < sliceTokens.Length; s++) {
                    _slices.Add(new Slice(this.rid, sliceTokens[s]));
                }
            }
            this.slices = _slices.ToArray();


            _manualEvents = new List<ManualEvent>();
            if (tokens.Length >= 5) { 
                string[] sliceEvents = tokens[4].Split(new char[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries);
                for (int e = 0; e < sliceEvents.Length; e++) {
                    _manualEvents.Add(new ManualEvent(sliceEvents[e]));
                }
            }
            this.manualEvents = _manualEvents.ToArray();
            closed = true;
        }

        // Create new
        public Recording(string pid, ulong tStart) {
            _manualEvents = new List<ManualEvent>();
            this.rid = pid;
            this.tStart = tStart;
        }

        public void AddManualEvent(ulong t, int ev) {
            if (closed) {
                Debug.LogWarning("Cannot add event to closed recording");
                return;
            }
            _manualEvents.Add(new ManualEvent(t, ev));
        }

        public void CloseRecording(ulong tEnd) {
            if (closed) {
                Debug.LogWarning("Cannot close an already closed recording");
                return;
            }
            this.tEnd = tEnd;
            closed = true;
            manualEvents = _manualEvents.ToArray();
            GenerateSlices();
        }

        private void GenerateSlices() {
            ulong total_duration = tEnd - tStart;
            List<Slice> _slices = new List<Slice>();
            if (total_duration <= ExperimenterGUIMangager.instance.sliceCrop * 2 + ExperimenterGUIMangager.instance.sliceLength) {
                this.slices = _slices.ToArray();
                return;
            }
            ulong tMax = total_duration - ExperimenterGUIMangager.instance.sliceCrop; 
            ulong tMin = ExperimenterGUIMangager.instance.sliceCrop;
            ulong maxDuration = tMax - tMin;
            ulong sliceLength = ExperimenterGUIMangager.instance.sliceLength;
            ulong remainder = maxDuration % sliceLength;
            ulong n_slices = maxDuration / sliceLength;
            ulong t0 = tMin + remainder / 2;
            
            for (ulong i = 0; i < n_slices; i++) {
                _slices.Add(new Slice(this.rid, t0, t0+sliceLength));
                t0 += sliceLength;
            }
            this.slices = _slices.ToArray();
        }

        public string Serialize() {
            if (!closed) {
                Debug.LogWarning("Cannot serialize an open recording");
                return "";
            }

            if (slices.Length == 0) GenerateSlices();

            string res = rid + "|" + tStart.ToString() + "|" + tEnd.ToString();
            string res_slices = "|";
            for (int s = 0; s < slices.Length; s++) {
                res_slices += slices[s].Serialize();
                if ((s+1)<slices.Length) {
                    res_slices += ",";
                }
            }
            string res_events = "|";
            for (int e = 0; e < manualEvents.Length; e++) {
                res_events += manualEvents[e].Serialize();
                if ((e + 1) < manualEvents.Length) {
                    res_events += ",";
                }
            }

            return res + res_slices + res_events;
        }
    }

    public class ManualEvent {
        public ulong t { get; private set; }
        public int evId { get; private set; }

        public ManualEvent(string serialized) {
            string[] tokens = serialized.Split(new char[] { '_' }, System.StringSplitOptions.RemoveEmptyEntries);
            this.t = ulong.Parse(tokens[0]);
            this.evId = int.Parse(tokens[1]);
        }

        public ManualEvent(ulong t, int evId) {
            this.t = t;
            this.evId = evId;
        }

        public string Serialize() {
            // should not contain ',' or '|' characters!
            return t.ToString() + "_" + evId;
        }
    }

    public class Slice {
        public string pid { get; private set; }
        public ulong tStart { get; private set; }
        public ulong tEnd { get; private set; }


        public Slice(string pid, string serialized) {
            string[] tokens = serialized.Split(new char[] { '_' }, System.StringSplitOptions.RemoveEmptyEntries);
            this.pid = pid;
            this.tStart = ulong.Parse(tokens[0]);
            this.tEnd = ulong.Parse(tokens[1]);
        }

        public Slice(string pid, ulong tStart, ulong tEnd) {
            this.pid = pid;
            this.tStart = tStart;
            this.tEnd = tEnd;
        }

        public string GetSliceID() {
            return pid + "_" + Serialize();
        }

        public string Serialize() {
            // should not contain ',' or '|' characters!
            return tStart.ToString()+"_"+tEnd.ToString();
        }
    }
}