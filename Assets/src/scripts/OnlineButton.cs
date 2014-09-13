using UnityEngine;
using System.Collections;

public class OnlineButton : MonoBehaviour {

    void OnMouseDown() {
        Main.servermanager.startonline();
    }
}
