using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Main : MonoBehaviour {

	public static CardManager cardmanager = new CardManager ();
	public static CardManager opponentcardmanager = new CardManager ();
	public static UIManager uimanager = new UIManager();
    public static Textures textures = new Textures();
    public static ServerManager servermanager = new ServerManager();
    public static bool pausegame = false;

	void Start () {
        Application.targetFrameRate = 60;
        Application.runInBackground = true;

		textures.initiatetextures();
		textures.initiateparticles();
        uimanager.initiate();

        servermanager.initiate();
	}

	void Update () {
        if (!pausegame) {
            cardmanager.update();
            opponentcardmanager.update();
            uimanager.update();
        }
        servermanager.update();
	}

	public static bool collidewith(float x, float y, float x2, float y2, int width, int height) {
		return x >= x2 - width && x <= x2 + width && y >= y2 - height && y <= y2 + height;
	}
}