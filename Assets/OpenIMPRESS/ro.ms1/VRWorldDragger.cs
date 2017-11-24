using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VRWorldDragger : MonoBehaviour {
    public static VRWorldDragger instance { get { return _instance; } }
    private static VRWorldDragger _instance = null;

    public float slerpBackSpeed = 5.0f;

    public Transform reviewVRTransform;
    public Transform manipulatedTransform;
    public WandController wandLeft;
    public WandController wandRight;

    public bool reset;
    public bool setToReview;

    private Vector3 startPosition;
    private Quaternion startRotation;

    void Awake() {
        if (_instance != null) {
            Debug.LogWarning("Trying to instantiate multiple VRWorldDragger.");
            DestroyImmediate(this.gameObject);
        }
        _instance = this;

        startPosition = manipulatedTransform.position;
        startRotation = manipulatedTransform.rotation;
        reset = true;
        setToReview = false;
    }

    public void SetToReview() {
        manipulatedTransform.position = reviewVRTransform.position;
        manipulatedTransform.rotation = reviewVRTransform.rotation;
    }

    public void Reset() {
        manipulatedTransform.position = startPosition;
        manipulatedTransform.rotation = startRotation;
    }

    void Update () {
        wandLeft.UpdateDrag();
        wandRight.UpdateDrag();

        bool left = wandLeft.interacting && wandLeft.gameObject.activeSelf;
        bool right = wandRight.interacting && wandRight.gameObject.activeSelf;

        bool left_T = wandLeft.thumbPress && wandLeft.gameObject.activeSelf;
        bool right_T = wandRight.thumbPress && wandRight.gameObject.activeSelf;

        if (left && right) {
            Vector3 prevDir = wandLeft.prevPos - wandRight.prevPos;
            Vector3 currDir = wandLeft.transform.position - wandRight.transform.position;
            prevDir.y = 0;
            currDir.y = 0;
            Quaternion rotation = Quaternion.FromToRotation(currDir, prevDir);
            float angle;
            Vector3 axis;
            rotation.ToAngleAxis(out angle, out axis);
            manipulatedTransform.RotateAround((wandLeft.transform.position * 0.5f + wandRight.transform.position * 0.5f), axis, angle);
            Vector3 delta = wandRight.deltaPos;
            delta.y = 0;
            manipulatedTransform.position -= (delta * 0.5f + delta * 0.5f);

        } else if (left ^ right) {
            WandController wand = (left) ? wandLeft : wandRight;
            Vector3 delta = wandRight.deltaPos;
            delta.y = 0;
            manipulatedTransform.position -= delta;
            //Debug.Log(left + "   " + right);
            //Debug.Log(wand.deltaPos*10000);
        } else if (left_T && right_T) {
            manipulatedTransform.position = Vector3.Slerp(manipulatedTransform.position, startPosition, Time.time * slerpBackSpeed);
            manipulatedTransform.rotation = Quaternion.Slerp(manipulatedTransform.rotation, startRotation, Time.time * slerpBackSpeed);
        } else if (setToReview) {
            SetToReview();
            setToReview = false;
        } else if (reset) {
            Reset();
            reset = false;
        }

        wandLeft.SavePos();
        wandRight.SavePos();
    }
}
