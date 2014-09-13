using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class UIManager {

	public TextMesh playerhealthtext;
	public TextMesh playermanatext;
    public float playerhealth = 250;
    public float playermana = 100;
	public TextMesh opphealthtext;
	public TextMesh oppmanatext;
    public float opphealth = 250;
    public float oppmana = 100;
	public GameObject cardtext;
    public GameObject damagetext;
    public GameObject wintext;

	public void initiate() {
        //gets the cardtext to be used later for displaying card details
		cardtext = GameObject.Find("cardtext");
        cardtext.renderer.enabled = false;
        //get damage text to be shown when damage is taken or given
		damagetext = GameObject.Find("damagetext");
        damagetext.renderer.enabled = false;
        damagetext.GetComponent<TextFade>().fadeenabled = false;
        //get win text
        wintext = GameObject.Find("wintext");
        wintext.renderer.enabled = false;

        //gets the text meshes of the player and opponent's health and mana textfields
		playerhealthtext = (TextMesh)GameObject.Find("playerhealthtext").GetComponent<TextMesh>();
		playermanatext = (TextMesh)GameObject.Find("playermanatext").GetComponent<TextMesh>();
		opphealthtext = (TextMesh)GameObject.Find("opphealthtext").GetComponent<TextMesh>();
		oppmanatext = (TextMesh)GameObject.Find("oppmanatext").GetComponent<TextMesh>();
		updateui();
	}

	public void updateui() {
        //updates ui on the board
        if (playerhealth <= 0) { playerhealth = 0; }
        if (playermana <= 0) { playermana = 0; }
        if (opphealth <= 0) { opphealth = 0; }
        if (oppmana <= 0) { oppmana = 0; }
        if (playermana >= 100) { playermana = 100; }
        if (oppmana >= 100) { oppmana = 100; }

        playerhealthtext.text = Mathf.Round(playerhealth).ToString();
        playermanatext.text = Mathf.Round(playermana).ToString();
        opphealthtext.text = Mathf.Round(opphealth).ToString();
        oppmanatext.text = Mathf.Round(oppmana).ToString();

        if (playerhealth <= 0) {
            wintext.renderer.enabled = true;
            wintext.GetComponent<TextMesh>().text = "Defeat";
        }else if (opphealth <= 0) {
            wintext.renderer.enabled = true;
            wintext.GetComponent<TextMesh>().text = "Victory";
        }
	}

    public void update() {
        playermana += .1f;
        oppmana += .1f;
        updateui();
    }
}