using System.Collections;
using System.Collections.Generic;
using System;
using PlayerIO.GameLibrary;

namespace Spellfire {

    [RoomType("CardManager")]
    public class CardManager {

        public List<CardDetails> cards = new List<CardDetails>();
        public int drawtimer = 0;
        public int cardsinhand = 0;
        public bool opponent = false;
        public List<CardDetails> slots = new List<CardDetails>();
        public List<CardDetails> deckcards = new List<CardDetails>();
        public CardDetails lastselected;
        public CardManager oppmanager;
        public int drawncards = 0;
        public GameCode gamecode;
        public int playerid = 0;

        public void initiate(bool opponentcards) {
            //checks if this cardmanager is a player's or an opponents
            opponent = opponentcards;
            if (opponent) { playerid = 1; }

            for (int n = 0; n < 6; ++n) {
                slots.Add(null);
            }

            //creates 30 cards and places them in the deck
            for (int i = 0; i < 30; ++i) {
                CardDetails newcard = createcard((int)gamecode.random.Next(0, 4));
                deckcards.Add(newcard);
                newcard.id = i;
            }
        }

        public void update() {
            if (gamecode.players.Count >= 2) {
                ++drawtimer;
                if (cards.Count < 30 && drawtimer >= 2) { drawtimer = 0; drawcard(); }
            }
        }

        private CardDetails createcard(int type) {
            //creates a new card based on the provided type
            CardDetails newcard = new CardDetails();
            newcard.type = type;
            newcard.damage = gamecode.carddetails[type].damage;
            newcard.health = gamecode.carddetails[type].health;
            newcard.manacost = gamecode.carddetails[type].manacost;

            return newcard;
        }

        private CardDetails drawcard() {
            //draws a card from the deck
            if (cardsinhand >= 10 && drawncards < 30) { return null; }

            CardDetails newcard = deckcards[deckcards.Count - 1];
            cards.Add(newcard);
            deckcards.Remove(newcard);
            Player player = gamecode.players[playerid];
            player.Send("drawcard", player.Id, newcard.type,
                newcard.health, newcard.manacost, newcard.damage);
            gamecode.getotherplayer(player).Send("drawcard", player.Id, newcard.type,
                newcard.health, newcard.manacost, newcard.damage);

            ++cardsinhand;
            ++drawncards;

            return newcard;
        }
    }
}