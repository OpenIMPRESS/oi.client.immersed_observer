using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using oi.plugin.rgbd;

public class BookmarkTest : MonoBehaviour {


    public class OIBookMark {
        public string name;
        public long startTime;
        public long endTime;
    }

    public class OIFile {
        public OIFile(string name) {
            this.name = name;
            this.recording = false;
            this.recorded = false;
            this.bookmarks = new List<OIBookMark>();
        }

        public string name;
        public bool recording;
        public bool recorded;
        public List<OIBookMark> bookmarks;
    }

    Dictionary<string, OIFile> files;

	// Use this for initialization
	void Start () {
        files = new Dictionary<string, OIFile>();
	}

    public void BTN_NewFile() {
        Debug.Log("NEW FILE");
    }


	// Update is called once per frame
	void Update () {
		
	}
}
