using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using PlayerIO.GameLibrary;
using System.Drawing;
using System.Xml;
using System.IO;
using System.Text;

namespace Spellfire {

	public class Player : BasePlayer {
        public int id = 0;
	}

	[RoomType("MyCode")]
	public class GameCode : Game<Player> {

        public List<Player> players = new List<Player>();
        public List<CardDetails> deckcards = new List<CardDetails>();
        public List<CardDetails> carddetails = new List<CardDetails>();
        public CardManager playercardmanager;
        public CardManager oppcardmanager;
        public Random random = new Random();

		//this method is called when an instance of your the game is created
		public override void GameStarted() {
			Console.WriteLine("Game is started: " + RoomId);

            //create xml object
            XmlDocument document = new XmlDocument();
            document.LoadXml(Encoding.UTF8.GetString(EmbeddedResource.GetBytes("carddetails.xml")));

            //parse xml
            int count = 0;
            while (true) {
                XmlNode node = document.SelectSingleNode("//creaturecard[@id='" + count + "']");
                if (node == null) { break; }

                //creates the carddetails object to store card details from the xml file
                CardDetails details = new CardDetails();

                details.health = int.Parse(node.Attributes["health"].Value);
                details.manacost = int.Parse(node.Attributes["mana"].Value);
                details.damage = int.Parse(node.Attributes["damage"].Value);
                carddetails.Add(details);
                ++count;
            }

            playercardmanager = new CardManager();
            playercardmanager.gamecode = this;
            playercardmanager.initiate(false);
            oppcardmanager = new CardManager();
            oppcardmanager.gamecode = this;
            oppcardmanager.initiate(true);

            AddTimer(playercardmanager.update, 1000);
            AddTimer(oppcardmanager.update, 1000);
		}

		//this method is called when the last player leaves the room, and it's closed down.
		public override void GameClosed() {
			Console.WriteLine("RoomId: " + RoomId);
		}

		//this method is called whenever a player joins the game
		public override void UserJoined(Player player) {
            if (players.Count >= 2) { player.Disconnect(); return; }
            Console.WriteLine("Player joined: " + player.Id);

            bool found = false;
            for (int n = 0; n < players.Count; ++n) {
                if (players[n] == player || players[n].Id == player.Id) {
                    found = true;
                    break;
                }
            }

            if (!found) {
                players.Add(player);
                if (players.Count >= 2) { players[0].Send("oppid", players[1].Id); }
            }
		}

        //this method is called when a player leaves the game
        public override void UserLeft(Player player) {
			Broadcast("PlayerLeft", player.ConnectUserId);
		}

		//this method is called when a player sends a message into the server code
		public override void GotMessage(Player player, Message message) {
            CardManager manager = oppcardmanager;
			switch(message.Type) {
                case "getid":
                    player.Send("myid", player.Id);
                    if (players.Count >= 2) { player.Send("oppid", getotherplayer(player).Id); }
                    break;
                case "pos":
                    getotherplayer(player).Send("pos", message.GetInt(0), message.GetInt(1), 
                        message.GetInt(2), message.GetInt(3));
                    break;
                case "stopdrag":
                    Broadcast("stopdrag", message.GetInt(0), player.Id);
                    Console.WriteLine(message.GetInt(0) + " stopped dragging");
                    break;
                case "startdrag":
                    getotherplayer(player).Send("startdrag", message.GetInt(0));
                    Console.WriteLine(message.GetInt(0) + " started dragging");
                    break;
                case "dropcard":
                    if (players[0] == player) { manager = playercardmanager; }
                    if (manager.slots[message.GetInt(1)] == null) {
                        CardDetails card = manager.cards[getcardindex(message.GetInt(0), manager)];
                        manager.slots[message.GetInt(1)] = card;
                        Broadcast("dropcard", message.GetInt(0), message.GetInt(1), player.Id);
                        card.slotid = message.GetInt(1);
                        card.onfield = true;
                        Console.WriteLine("slot " + message.GetInt(1) + " now has a card on it");
                        Console.WriteLine("card of id " + card.id + " is now on the " + manager.opponent + " field: ");
                        printslots(manager);
                        --manager.cardsinhand;
                    }else {
                        Broadcast("stopdrag", message.GetInt(0), player.Id);
                        Console.WriteLine("slot " + message.GetInt(1) + " is not empty");
                    }
                    break;
                case "settarget":
                    Console.WriteLine("setting target...");
                    CardManager oppositecardmanager = playercardmanager;
                    if (players[0] == player) { manager = playercardmanager;
                    oppositecardmanager = oppcardmanager; }

                    int index = getcardindex(message.GetInt(0), manager);
                    int oppindex = getcardindex(message.GetInt(1), oppositecardmanager);
                    if (index != -1 && oppindex != -1 && 
                        manager.cards[index].onfield && oppositecardmanager.cards[oppindex].onfield) {
                        Broadcast("settarget", message.GetInt(0), message.GetInt(1), player.Id);

                        Console.WriteLine("player card health: " + manager.cards[index].health);
                        Console.WriteLine("opponent card health: " + oppositecardmanager.cards[oppindex].health);

                        manager.cards[index].health -= oppositecardmanager.cards[oppindex].damage;
                        oppositecardmanager.cards[oppindex].health -= manager.cards[index].damage;
                        if (manager.cards[index].health <= 0) {
                            manager.slots[manager.cards[index].slotid] = null;
                            Console.WriteLine("destroyed player card " + manager.cards[index].type);
                            manager.cards.Remove(manager.cards[index]);
                        }
                        if (oppositecardmanager.cards[oppindex].health <= 0) {
                            oppositecardmanager.slots[oppositecardmanager.cards[oppindex].slotid] = null;
                            Console.WriteLine("destroyed opponent card " + oppositecardmanager.cards[oppindex].type);
                            oppositecardmanager.cards.Remove(oppositecardmanager.cards[oppindex]);
                        }
                        printslots(manager);
                        printslots(oppositecardmanager);
                    }else if (index == -1) {
                        Console.WriteLine("card of id " + message.GetInt(0) + " doesn't exist");
                    }else if (!manager.cards[index].onfield) {
                        Console.WriteLine("card of id " + manager.cards[index].id + " is not on the field");
                        Console.WriteLine("card index: " + index + " - " + manager.opponent);
                    }
                    break;
			}
		}

        public void printslots(CardManager manager) {
            string slots = "";
            for (int n = 0; n < manager.slots.Count; ++n) {
                if (manager.slots[n] == null) {
                    slots += "_ ";
                }else {
                    slots += manager.slots[n].type + " ";
                }
            }
            Console.WriteLine(manager.opponent + " slots: " + slots);
        }

        public int getcardindex(int fromid, CardManager manager) {
            for (int n = 0; n < manager.cards.Count; ++n) {
                if (manager.cards[n].id == fromid) {
                    return n;
                }
            }
            return -1;
        }

       public Player getotherplayer(Player player) {
            if (players.Count >= 2) {
                if (players[0] == player) { return players[1];
                }else { return players[0]; }
            }
            return null;
        }
	}
}