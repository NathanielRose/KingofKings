using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CardManager {

	private Card card;
	public List<Card> cards = new List<Card> ();
	public bool draggingcard = false;
	public int cardid = 0;
	private Plane plane = new Plane(Vector3.up, 0);
	private float distance = 1;
	private int drawtimer = 0;
	public int cardsinhand = 0;
    public bool opponent = false;
    public List<GameObject> collisionslots = new List<GameObject>();
	public List<GameObject> slots = new List<GameObject>();
	public List<Card> deckcards = new List<Card>();
	private GameObject deck;
	public GameObject cardborder;
	public Card lastselected;
    public CardManager oppmanager;
    public int drawncards = 0;
    public bool onlinedragging = false;

	public void initiate(bool opponentcards) {
        //checks if this cardmanager is a player's or an opponents
		opponent = opponentcards;
		if (this == Main.cardmanager) { oppmanager = Main.opponentcardmanager;
		}else if (this == Main.opponentcardmanager) { oppmanager = Main.cardmanager; }

        //creates a placeholder card to be cloned later to create cards
		card = new Card (false);
		card.core = GameObject.Find ("card");
        card.core.renderer.enabled = false;

        //gets the first slot that belongs to this cardmanager
		string oppstring = "player";
		if (opponent) { oppstring = "opp"; }
		GameObject slot = GameObject.Find(oppstring + "slot1");
        collisionslots.Add(slot); slots.Add(null);

        //clone and create 5 slots based off a original slot
		for (int n = 0; n < 3; ++n) {
			GameObject newslot = (GameObject)Object.Instantiate(slot);
			newslot.name = oppstring + "slot" + (n + 2);
            newslot.transform.Translate(-150 * (n + 1), 0, 0);
            collisionslots.Add(newslot); slots.Add(null);
		}

        //get the deck that belongs to this cardmanager
        deck = GameObject.Find(oppstring + "deck");

        //creates 30 cards and places them in the deck
		for (int i = 0; i < 30; ++i) {
			Card newcard = createcard ((int)Random.Range (0, Main.textures.carddetails.Count));
			deckcards.Add (newcard);
			newcard.originposition = deck.transform.position;
            newcard.originposition.y += i * 1; newcard.originposition.z += Random.Range(-5, 5);
            newcard.details.id = i;
		}

        //get the border card object
        cardborder = GameObject.Find("bordercard");
		cardborder.renderer.enabled = false;
	}

    public void update() {
        if (!Main.servermanager.connected) {
            ++drawtimer;
            if (cards.Count < 30 && drawtimer >= 150) { drawtimer = 0; drawcard(); }

            if (opponent && Random.Range(0, 100) >= 99 && cards.Count >= 1) {
                dropcard();
            }
        }

        //updates animations and more of the cards
        for (int n = 0; n < cards.Count; ++n) {
            cards[n].update();
        }
        for (int i = 0; i < deckcards.Count; ++i) {
            deckcards[i].update();
        }

        //mouse click
        if (Input.GetMouseButtonDown(0) && cards.Count >= 1) {
            //if clicking a card that is not on the field, start dragging it
            draggingcard = false;
            if (!opponent && cardid != -1 && !cards[cardid].onfield) {
                if (Main.servermanager.connected) { Main.servermanager.connection.Send("startdrag", cards[cardid].details.id); }
                --cardsinhand; repositionhandcards(); draggingcard = true;
            }

            //deselects the selected card when clicking anywhere except the enemy cards
            if (lastselected != null && oppmanager.cardcollision() == -1) { lastselected.deselect(); }

            //if clicking a card, select it
            if (cardid != -1 && cards[cardid].onfield) {
                cards[cardid].select(); lastselected = cards[cardid];
            }
        }else if (Input.GetMouseButtonUp(0) && draggingcard) { //mouseup
            //drop a card on the field if it's colliding with a slot
            ++cardsinhand;
            if (cardid != -1 && !cards[cardid].onfield) {
                cards[cardid].drop();
                if (cardid != -1 && cards[cardid].onfield) { --cardsinhand; }
            }
            if (!Main.servermanager.connected) {
                draggingcard = false;
                cardid = -1;
                repositionhandcards();
            }
        }

        //makes the card the player selected move towards the mouse
        if (draggingcard && cardid != -1 && cardid <= cards.Count - 1) {
            cards[cardid].drag(plane);
        }else {
            //bounces card when hovering the mouse over it
            int lastcardid = cardid;
            if (cardcollision() != -1) {
                if (lastcardid != -1 && cardid != lastcardid) {
                    cards[lastcardid].resting = true; cards[lastcardid].resetaccel();
                }
                if (!opponent || cards[cardid].onfield) { cards[cardid].enlarge(); }
            }
            if (cardid == -1 && lastcardid != -1 && cards.Count >= 1 && lastcardid <= cards.Count - 1) {
                cards[lastcardid].resting = true; cards[lastcardid].resetaccel();
            }
        }
    }

    private void dropcard() {
        //finds a random slot and gets a random card in the hand to place on the slot
        for (int i = 0; i < slots.Count; ++i) {
            if (slots[i] == null) {
                List<int> handcards = new List<int>();
                for (int n = 0; n < cards.Count; ++n) {
                    if (!cards[n].onfield) {
                        handcards.Add(n);
                    }
                }
                if (handcards.Count >= 1) {
                    int num = handcards[Random.Range(0, handcards.Count)];
                    cards[num].dropcardonslot(i);
                    if (cards[num].onfield) { --cardsinhand; }
                }
                handcards.Clear();
                handcards = null;
                return;
            }
        }
    }

    private Card createcard(int type) {
        //creates a new card based on the provided type
		Card newcard = new Card (opponent);
		newcard.core = (GameObject)Object.Instantiate (card.core);
		newcard.core.renderer.material.SetTexture (0, Main.textures.backcard);
		newcard.core.renderer.enabled = true;
		newcard.returningcard = true;
		newcard.manager = this;
        newcard.type = type;
        //noob clone cuz i cbf properly cloning
        CardDetails details = new CardDetails();
        details.damage = Main.textures.carddetails[type].damage;
        details.health = Main.textures.carddetails[type].health;
        details.manacost = Main.textures.carddetails[type].manacost;
        details.texture = Main.textures.carddetails[type].texture;
        newcard.details = details;

        return newcard;
	}

	public Card drawcard(bool newvalues = false, int type = 0, int health = 0, int mana = 0, int damage = 0) {
        //draws a card from the deck
		if (cardsinhand >= 10 && drawncards < 30) { return null; }

        Card newcard = deckcards[deckcards.Count - 1];
        if (newvalues) {
            newcard.type = type;
            newcard.details.type = type;
            newcard.details.health = health;
            newcard.details.manacost = mana;
            newcard.details.damage = damage;
            newcard.details.texture = Main.textures.carddetails[type].texture;
        }
		cards.Add (newcard);
		deckcards.Remove(newcard);
        newcard.createtext();
		if (!opponent) { newcard.settexture(); } //card is hidden if it's an opponent card

		++cardsinhand;
		repositionhandcards ();
        newcard.rotation.y = 180;
        ++drawncards;

		return newcard;
	}

	public void repositionhandcards() {
        //sorts out all the cards in the hand by rotating and aligning them correctly
        //calculates the span between all the cards in the hand
		int span = 100 - (cardsinhand * 8);
		if (span <= 40) {
			span = 40;
		}
        //calculates the first card's starting position and rotation
		int startx = -((cardsinhand - 1) * span) / 2;
		int turn = -((cardsinhand - 1) * 10) / 2;
		for (int n = 0; n < cards.Count; ++n) {
            //sets positions for cards
			if (cards[n].onfield || cardid == n) { continue; }
			cards [n].originposition.x = startx;
			cards [n].originposition.y = -34 - (n * 2);
			cards [n].originposition.z = -125;
			if (opponent) { cards[n].originposition.z = 580; }
			cards [n].returningcard = true;

            //makes sure the rotation doesn't go over 28 or under -28
			int tempturn = turn;
			if (tempturn <= -28) {
				tempturn = -28;
			}else if (tempturn >= 28) {
				tempturn = 28;
			}
            cards[n].originalrotation.y = tempturn;
			if (opponent) { cards [n].originalrotation.y = -tempturn; }

            //applies new positions and rotations
			cards [n].rotation = cards [n].originalrotation;
			cards [n].core.transform.localEulerAngles = cards [n].originalrotation;
			startx += span;
			turn += 10;
		}
	}

	public int cardcollision() {
		//collision with card
		Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        plane.Raycast(ray, out distance); Vector3 point = ray.GetPoint(distance);

        //goes through every card to see which one is closest to the mouse and returns the id
		float closestdistance = 9999;
		for (int i = 0; i < cards.Count; ++i) {
			if (Main.collidewith (cards[i].originposition.x, cards[i].originposition.z, 
			                      point.x, point.z, 50, 70)) {
				//calculate closest card to the mouse
				float dist = Mathf.Abs((point.x - cards[i].originposition.x) + 
				                       (point.z - cards[i].originposition.z));
				if (dist < closestdistance) { closestdistance = dist; cardid = i; }
			}
		}
		if (closestdistance != 9999) { return cardid; }

		cardid = -1;
		return cardid;
	}
}