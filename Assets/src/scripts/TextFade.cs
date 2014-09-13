using UnityEngine;
using System.Collections;

public class TextFade : MonoBehaviour {

	public const float fadespeed = .01f;
    private Color colour = new Color();
    public bool fadeenabled = false;

	void Start () {
        //resets alpha to 1 (255)
		colour = transform.renderer.material.color;
		colour.a = 1;
	}

	void Update () {
        if (fadeenabled) {
            //changes the renderer's material's alpha channel to fade away quickly
            colour.a -= fadespeed;
            renderer.material.color = colour;
            transform.Translate(0, 0, -1);
            if (colour.a <= 0) {
                Object.DestroyObject(this);
            }
        }
	}
}
