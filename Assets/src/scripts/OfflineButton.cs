using UnityEngine;
using System.Collections;

public class OfflineButton : MonoBehaviour {

    void OnMouseDown() {
        Main.servermanager.startoffline();
    }
}
