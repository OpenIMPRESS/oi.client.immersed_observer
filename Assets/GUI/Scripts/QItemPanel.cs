using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

namespace ImmersiveObserverExperiment {
    [RequireComponent(typeof(RectTransform))]
    public class QItemPanel : MonoBehaviour {

        public TextAsset qSpec;
        public QItem[] items;

        public GameObject[] itemStylePrefabs;

        public AssessmentGUIManager assessmentGUIManager;

        void Awake() {
            for (int i = 0; i < itemStylePrefabs.Length; i++) {
                Debug.Log(itemStylePrefabs[i].name);
            }

            if (items.Length == 0 && qSpec != null) {
                string[] lines = qSpec.text.Split('\n');
                List<QItem> generatedItems = new List<QItem>();
                foreach (string line in lines) {
                    string[] tokens = line.Split('|');
                    string itemType = tokens[0].Trim();
                    GameObject goPrefab = FindPrefab(itemType);
                    if (goPrefab == null) {
                        Debug.LogError("No prefab found matching item type: " + itemType);
                        return;
                    }
                    GameObject goItem = Instantiate(goPrefab);
                    QItem qItem = goItem.GetComponent<QItem>();
                    if (qItem == null) {
                        Debug.LogError("Prefab for type " + itemType + " doesn't have a QItem component");
                    }
                    qItem.Init(tokens);
                    generatedItems.Add(qItem);
                }
                items = generatedItems.ToArray();
                int i = 0;
                float yPos = 0;
                float padding = 3f;
                while (i < items.Length) {
                    RectTransform rectTransform = items[i].GetComponent<RectTransform>();
                    rectTransform.SetParent(transform, false);
                    rectTransform.anchoredPosition = new Vector2(0, -yPos);
                    yPos += rectTransform.sizeDelta.y + padding;
                    i++;
                }
            } else {
                items = GetComponentsInChildren<QItem>();
            }
        }

        void Start() {

        }

        GameObject FindPrefab(string type) {
            int i = 0;
            while (i < itemStylePrefabs.Length) {
                if (itemStylePrefabs[i].name == type) {
                    return itemStylePrefabs[i];
                }
                i++;
            }

            return null;
        }

        // Update is called once per frame
        void Update() {

        }

        public void Reset() {
            foreach (QItem item in items) {
                item.Reset();
            }
        }

        public void OnSubmit() {
            int[] result = new int[items.Length];
            int resIdx = 0;
            foreach (QItem item in items) {
                int iRes = item.GetResult();
                if (iRes >= 0) {
                    result[resIdx] = iRes;
                } else {
                    Debug.LogWarning("Submit not accepted - not all items answered.");
                    return;
                }

                resIdx++;
            }

            Debug.Log(string.Join(",", result.Select(x => x.ToString()).ToArray()));
            Reset();
            if (assessmentGUIManager != null) assessmentGUIManager.OnSubmit(result);
        }
    }
}