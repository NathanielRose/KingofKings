using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Card {

	//card variables
	public GameObject core;
	public bool onfield = false;
	public bool opponent = false;
	public CardManager manager;
	public int type = 0;
	public bool selected = false;
	public Card targetcard;
	public bool attacking = false;
	public CardDetails details;
	public int slotid = 0;

	//transform variables
	public Vector3 position;
	public Vector3 originposition;
	public Vector3 onfieldpos;
	public Vector3 originalrotation = new Vector3 (90, 180, 0);
	public Vector3 originalscale = new Vector3 (100, 140, 100);
	public Vector3 rotation = new Vector3 (90, 180, 0);
	public Vector3 scale = new Vector3 (100, 140, 100);

	//movement variables
	private float cardaccel = 0;
	private const float scalespeed = 10f;
    private float returnspeed = .1f;
	private float cardaccelspeed;
	public bool resting = false;
	private float lastx = 0;
	private float lastz = 0;
	private float distance = 1;
    public bool returningcard = false;
    public Vector3 destdrag = Vector3.zero;
    public bool destdragging = false;

	//text variables
	private TextMesh manatext;
	private TextMesh healthtext;
    private TextMesh damagetext;

	public Card(bool opponentcard) {
		opponent = opponentcard;
		cardaccelspeed = scalespeed;
	}

	public void update() {
        if (destdragging) {
            position = Vector3.Lerp(position, destdrag, .1f);
            core.transform.position = position;
            dragcardrotation();
            returningcard = false;
            resting = false;
        }

		if (selected) {
            //when this card is selected a card border will go behind it until it is deselected
			position.y -= 1;
			scale.x += 5; scale.y += 5; scale.z += 5;
			manager.cardborder.transform.position = position;
			manager.cardborder.transform.localScale = scale;
			manager.cardborder.transform.localEulerAngles = rotation;
			position.y += 1;
            scale.x -= 5; scale.y -= 5; scale.z -= 5;
		}

		if (resting) { rest (); }
		if (returningcard) { returncard (); }
	}

	public void bounce() {
        //bouncing animation
		core.transform.localScale = scale;
		cardaccel += cardaccelspeed;
		scale.x += cardaccel; scale.y += cardaccel; scale.z += cardaccel;
		if (cardaccelspeed > 0 && scale.x >= 12) { cardaccelspeed = -.01f;
		}else if (cardaccelspeed < 0 && scale.x <= 12) { cardaccelspeed = .02f; }
	}

	public void enlarge() {
        //enlargens the card on mouseover
		if (!returningcard) {
            //checks whether the card is in the hand or on the field and make changes to enlarge
			if (!onfield) { position.y = 20; }
			resting = false;
			if (!onfield && cardaccelspeed > 0) {
				if (!opponent) { position.z += 15; }else { position.z -= 5; }
			}
            rotation.x = 90; rotation.y = 0; rotation.z = 0;
            //applies position, rotation and scale
			core.transform.position = position;
			core.transform.localEulerAngles = rotation;
            core.transform.localScale = scale;
            //scale accelerates bigger
			cardaccel += cardaccelspeed;
			scale.x += cardaccel;
			scale.y += cardaccel;
            scale.z += cardaccel * 1.5f;
            //scale resets or stops when it hits a limit
			if (cardaccelspeed > 0 && scale.x >= originalscale.x + 60 ||
			    onfield && cardaccelspeed > 0 && scale.x >= originalscale.x + 20) { cardaccelspeed = -scalespeed;
			}else if (cardaccelspeed < 0 && cardaccel < 0) { cardaccelspeed = 0; cardaccel = 0; }

            if (!onfield) {
                healthtext.renderer.enabled = true;
                damagetext.renderer.enabled = true;
                manatext.renderer.enabled = true;
            }
		}
	}

	public void resetaccel() {
		cardaccel = 0; cardaccelspeed = scalespeed;
	}

	public void rest() {
        if (!onfield) {
            if (damagetext != null && manatext != null) {
                healthtext.renderer.enabled = false;
                damagetext.renderer.enabled = false;
                manatext.renderer.enabled = false;
            }
        }

        //smooth transition to original position, rotation and scale vectors
        position = Vector3.Lerp(position, originposition, .1f);
        scale = Vector3.Lerp(scale, originalscale, .1f);
        if (!returningcard) {
            //smooths rotation if the card is not returning
            rotation = Vector3.Lerp(rotation, originalrotation, .1f);
        }
        //check if the position and scale are near their original values when smoothing
        if (Vector3.Distance(position, originposition) <= 1 && Vector3.Distance(scale, originalscale) <= 1) {
            //resets the position, scale and rotation if the original scale is near the new scale
            resting = false; position = originposition; scale = originalscale; rotation = originalrotation;
        }
		core.transform.localScale = scale;
        core.transform.position = position;
        core.transform.localEulerAngles = rotation;
	}

	public void drop() {
        //drops the card onto a slot
        List<GameObject> slots = manager.slots; //gets the manager's slot array
		for (int n = 0; n < slots.Count; ++n) {
            //searches through all the slots to see if there if the slot is empty
			if (slots[n] == null) {
                //does a collision with the empty slot to see if the card is colliding with it
				if (Main.collidewith(manager.collisionslots[n].transform.position.x, 
                    manager.collisionslots[n].transform.position.z, position.x, position.z, 50, 70)) {
                    dropcardonslot(n);
                    return;
				}
			}
		}
        if (Main.servermanager.connected && !opponent) {
            Main.servermanager.connection.Send("stopdrag", details.id);
        }
	}

    public void requiremana() {
        if (!opponent) {
            //create require mana text
            GameObject requiremanatext = (GameObject)Object.Instantiate(Main.uimanager.damagetext);
            requiremanatext.transform.position = new Vector3(-82, position.y, position.z);
            requiremanatext.transform.Translate(0, 20, 0);
            requiremanatext.renderer.enabled = true;
            requiremanatext.GetComponent<TextMesh>().text = "You need more mana";
            requiremanatext.GetComponent<TextFade>().fadeenabled = true;
            requiremanatext.GetComponent<TextMesh>().color = Color.blue;
        }
    }

    public void dropcardonslot(int id, bool servermessage = false) {
        if (servermessage || !Main.servermanager.connected) {
            if (!opponent) {
                if (Main.uimanager.playermana >= details.manacost) {
                    Main.uimanager.playermana -= details.manacost; Main.uimanager.updateui();
                }else { requiremana(); return; }
            }else if (opponent) {
                if (Main.uimanager.oppmana >= details.manacost) {
                    Main.uimanager.oppmana -= details.manacost; Main.uimanager.updateui();
                }else { requiremana(); return; }
            }

            //smooth the card onto the slot
            originposition = manager.collisionslots[id].transform.position;
            originposition.x -= 4; originposition.y = -46;
            originalrotation.x = 90; originalrotation.y = 0; originalrotation.z = 0;
            originalscale.x = 140; originalscale.y = 200; originalscale.z = 100;
            scale = originalscale;
            core.transform.localScale = scale;
            core.transform.localEulerAngles = rotation;
            onfield = true;
            returningcard = false;
            resting = true;
            slotid = id;
            manager.slots[id] = core;
            //creates particles when the card is down
            GameObject particle = (GameObject)Object.Instantiate(Main.textures.energyparticles);
            particle.transform.position = originposition; particle.transform.Translate(0, 20, 0); particle = null;
            if (opponent) { settexture(); }

            healthtext.renderer.enabled = true;
            damagetext.renderer.enabled = true;
            manatext.renderer.enabled = true;
            return;
        }

        if (Main.servermanager.connected) {
            if (!opponent) {
                if (Main.uimanager.playermana - details.manacost < 0) { requiremana(); return; }
            }else if (opponent) {
                if (Main.uimanager.oppmana - details.manacost < 0) { requiremana(); return; }
            }
        }

        if (Main.servermanager.connected && !servermessage) { manager.draggingcard = false; manager.cardid = -1; manager.onlinedragging = false; 
            destdragging = false; resting = false; Main.servermanager.connection.Send("dropcard", details.id, id); }
    }

	public void returncard() {
        if (!onfield) {
            if (damagetext != null) {
                healthtext.renderer.enabled = false;
                damagetext.renderer.enabled = false;
                manatext.renderer.enabled = false;
            }
        }

        //smooth transition to original position, rotation and scale vectors
        position = Vector3.Lerp(position, originposition, returnspeed);
        rotation = Vector3.Lerp(rotation, originalrotation, returnspeed);
        scale = Vector3.Lerp(scale, originalscale, returnspeed);
		core.transform.position = position;
        core.transform.localEulerAngles = rotation;
        core.transform.localScale = scale;

        //check if the position and scale are near their original values when smoothing
        if (Vector3.Distance(position, originposition) <= .1) {
            //resets the position, scale and rotation if the original scale is near the new scale
            returningcard = false; position = originposition; scale = originalscale; rotation = originalrotation;

            if (targetcard != null) {
                if (attacking) {
                    targetcard.takedamage(this); returningcard = true;
                    originposition = onfieldpos; attacking = false; returnspeed = .2f;
                }else { targetcard = null; returnspeed = .1f; }
            }
        }
	}

	public void reset() {
		resting = false; returningcard = false;
		scale.x = 10; scale.y = 1; scale.z = 14; cardaccel = 0; cardaccelspeed = .01f;
        core.transform.localScale = scale;
	}

	public void drag(Plane plane) {
        healthtext.renderer.enabled = true;
        damagetext.renderer.enabled = true;
        manatext.renderer.enabled = true;

        //makes the card follow the mouse
		returningcard = false;
		scale.x -= (scale.x - 140) / 4; scale.y -= (scale.y - 200) / 4;
        core.transform.localScale = scale;

        //performs a raycast on an invisible plane to calculate the mouse position
		Ray ray = Camera.main.ScreenPointToRay (Input.mousePosition);
		if (plane.Raycast (ray, out distance)) {
            //rotates the card depending on the mouse direction and power
            Vector3 pos = ray.GetPoint(distance);
            dragcardrotation();
            core.transform.position = pos;
            position = pos;

            if (!opponent && Main.servermanager.connected && manager.draggingcard) {
                Main.servermanager.connection.Send("pos", details.id, position.x, position.y, position.z);
            }
		}
	}

    public void dragcardrotation() {
        rotation.x = 90 - ((lastz - position.z) / 2) - (lastx - position.x) / 5;
        rotation.y = -(lastx - position.x) / 5;
        if (rotation.x >= 120) { rotation.x = 120; }else if (rotation.x <= 60) { rotation.x = 60; }
        if (rotation.y >= 20) { rotation.y = 20; }else if (rotation.y <= -20) { rotation.y = -20; }
        core.transform.localEulerAngles = rotation;
        lastx -= (lastx - position.x) / 5;
        lastz -= (lastz - position.z) / 5;
    }

	public void settexture() {
        //sets the texture to the bitmap in the card details
		core.renderer.material.SetTexture (0, details.texture);
	}

	public void select(bool servermessage = false) {
        //selects this card or selects the enemy card to attack
        if (manager.oppmanager.lastselected != null && opponent && !servermessage) {
            if (Main.servermanager.connected) {
                Main.servermanager.connection.Send("settarget", Main.cardmanager.lastselected.details.id, details.id);
            }else { manager.oppmanager.lastselected.settarget(this); }
        }else if (!opponent || servermessage) {
            manager.cardborder.renderer.enabled = true;
            selected = true;
        }
	}

	public void deselect() {
        //deselects this card
		selected = false; manager.cardborder.renderer.enabled = false; manager.lastselected = null;
	}

	public void settarget(Card newtarget) {
        //makes the card fly towards its target
		if (targetcard == null) {
			targetcard = newtarget;
			onfieldpos = originposition;
			originposition = targetcard.originposition;
			originposition.y += 10;
			returningcard = true;
            returnspeed = .4f;
			attacking = true;
		}
	}

	public void takedamage(Card from, bool attack = true) {
        //positions of one taking damage and one giving damage
        Vector3 pos = originposition;
        if (!attack) { pos = onfieldpos; }

		//create particle
		GameObject particle = (GameObject)Object.Instantiate(Main.textures.bleedparticles);
        particle.transform.position = pos; particle.transform.Translate(0, 20, 0); particle = null;

		//create damage text
		GameObject damagefadetext = (GameObject)Object.Instantiate(Main.uimanager.damagetext);
        damagefadetext.transform.position = pos; damagefadetext.transform.Translate(0, 20, 0);
		damagefadetext.renderer.enabled = true;
        damagefadetext.GetComponent<TextMesh>().text = from.details.damage.ToString();
        damagefadetext.GetComponent<TextFade>().fadeenabled = true;

        //deal damage to crystals
        int damage = from.details.damage;
        if (details.health - damage < 0) { damage = Mathf.Abs(details.health - damage); }else { damage = 0; }
        if (!opponent) { Main.uimanager.playerhealth -= damage;
        }else { Main.uimanager.opphealth -= damage; }
        Main.uimanager.updateui();

		//take away health
        details.health -= from.details.damage;
        healthtext.text = details.health.ToString();
        damagetext.text = details.damage.ToString();
        manatext.text = details.manacost.ToString();

		if (details.health <= 0) {
			destroy();
		}

        //return the attack from the card
        if (attack) {
            from.takedamage(this, false);
        }
	}

	public void createtext() {
        //create a mana text overlay on the card
        healthtext = ((GameObject)Object.Instantiate(Main.uimanager.cardtext)).GetComponent<TextMesh>();
        healthtext.renderer.enabled = true;
        healthtext.transform.parent = core.transform;
        healthtext.transform.localPosition = new Vector3(.24f, -.30f, -.01f);
        healthtext.transform.localEulerAngles = new Vector3(0, 0, 0);
        healthtext.text = details.health.ToString();
        healthtext.renderer.enabled = false;

        //create a damage text overlay on the card
		damagetext = ((GameObject)Object.Instantiate(Main.uimanager.cardtext)).GetComponent<TextMesh>();
		damagetext.renderer.enabled = true;
		damagetext.transform.parent = core.transform;
        damagetext.transform.localPosition = new Vector3(-.38f, -.30f, -.01f);
        damagetext.transform.localEulerAngles = new Vector3(0, 0, 0);
        damagetext.text = details.damage.ToString();
        damagetext.renderer.enabled = false;

        //create a mana text overlay on the card
        manatext = ((GameObject)Object.Instantiate(Main.uimanager.cardtext)).GetComponent<TextMesh>();
        manatext.renderer.enabled = true;
        manatext.transform.parent = core.transform;
        manatext.transform.localPosition = new Vector3(-.44f, .50f, -.01f);
        manatext.transform.localEulerAngles = new Vector3(0, 0, 0);
        manatext.text = details.manacost.ToString();
        manatext.renderer.enabled = false;
	}

	public void destroy() {
        //destroys this card and clears the slot if it was on one
        Object.DestroyObject(healthtext);
        healthtext = null;
        Object.DestroyObject(damagetext);
        damagetext = null;
        Object.DestroyObject(manatext);
        manatext = null;
		manager.cards.Remove(this);
		manager.slots[slotid] = null;
		deselect ();
		Object.DestroyObject(core);
	}
}