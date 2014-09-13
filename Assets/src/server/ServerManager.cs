using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using PlayerIOClient;
using System.Threading;

public class ServerManager {

    //servermanager variables
    public TextMesh infotext;
    public Connection connection;
    public Client client;
    public Thread thread;
    public bool threadcomplete = false;
    public bool joined = false;
    public int id = 0;
    public int oppid = 0;
    public bool connected = false;
    public List<Message> messages = new List<Message>();
    public bool disconnected = false;
    public bool gamecreated = false;

    public void initiate() {
        Main.pausegame = true;
        GameObject.Find("floor").transform.position = new Vector3(0, 180, 412);
        infotext = GameObject.Find("infotext").GetComponent<TextMesh>();
        infotext.text = "";
        Main.uimanager.playerhealthtext.text = "";
        Main.uimanager.playermanatext.text = "";
        Main.uimanager.opphealthtext.text = "";
        Main.uimanager.oppmanatext.text = "";
    }

    public void connect() {
        infotext.text = "Connecting to yahoo games network...";
        Debug.Log("Connecting...");

        thread = new Thread(backgroundconnect);
        thread.Start();
        threadcomplete = false;
    }

    public void backgroundconnect() {
        client = PlayerIO.Connect(
            "spellfire-eldbsaqvukcbo0pzctkig",	// Game id (Get your own at gamesnet.yahoo.com. 1: Create user, 2:Goto control panel, 3:Create game, 4: Copy game id inside the "")
            "public",						// The id of the connection, as given in the settings section of the control panel. By default, a connection with id='public' is created on all games.
            "user-id",						// The id of the user connecting. This can be any string you like. For instance, it might be "fb10239" if you´re building a Facebook app and the user connecting has id 10239
            null,							// If the connection identified by the connection id only accepts authenticated requests, the auth value generated based on UserId is added here
            null							// The partnerid to tag the user with, if using PartnerPay
        );
        Debug.Log("Connected to Yahoo Games Network");

        //client.Multiplayer.DevelopmentServer = new ServerEndpoint("localhost", 8184);
        connection = client.Multiplayer.CreateJoinRoom("my-room-id", "MyCode", true, null, null);
        Debug.Log("Joined Multiplayer Room");
        threadcomplete = true;
        connected = true;
    }

    public void createconnectionevents() {
        connection.OnMessage += delegate(object sender, PlayerIOClient.Message m) {
            messages.Add(m);
        };

        connection.OnDisconnect += delegate(object sender, string reason) {
            disconnected = true;
        };
    }

    public void update() {
        if (threadcomplete) {
            createconnectionevents();
            threadcomplete = false;
            thread = null;
            infotext.text = "Connected. Waiting for other players...";
            connection.Send("getid");
            Debug.Log("Created connection events");
        }

        if (messages.Count >= 1) {
            for (int n = 0; n < messages.Count; ++n) {
                Message m = messages[n];
                string name = m.Type;
                int index = 0;
                CardManager cardmanager;
                switch (name) {
                    case "myid":
                        id = m.GetInt(0);
                        break;
                    case "oppid":
                        if (oppid == 0) {
                            oppid = m.GetInt(0);
                            startgame();
                        }
                        break;
                    case "pos":
                        if (!gamecreated) { break; }
                        index = getcardindex(m.GetInt(0), Main.opponentcardmanager);
                        if (index != -1) {
                            Card card = Main.opponentcardmanager.cards[index];
                            if (Main.opponentcardmanager.onlinedragging) {
                                card.destdrag.x = m.GetInt(1);
                                card.destdrag.y = m.GetInt(2);
                                card.destdrag.z = -m.GetInt(3) + 400;
                            }
                        }
                        break;
                    case "drawcard":
                        if (!gamecreated) { break; }
                        int tempid = m.GetInt(0);
                        if (id == tempid) {
                            Main.cardmanager.drawcard(true, m.GetInt(1), m.GetInt(2), m.GetInt(3), m.GetInt(4));
                        }else {
                            Main.opponentcardmanager.drawcard(true, m.GetInt(1), m.GetInt(2), m.GetInt(3), m.GetInt(4));
                        }
                        break;
                    case "stopdrag":
                        if (!gamecreated) { break; }
                        cardmanager = Main.opponentcardmanager;
                        if (m.GetInt(1) == id) { cardmanager = Main.cardmanager; }
                        index = getcardindex(m.GetInt(0), cardmanager);
                        if (index != -1) {
                            cardmanager.cards[index].destdragging = false;
                            cardmanager.cards[index].returningcard = true;
                            cardmanager.cards[index].resting = false;
                            cardmanager.draggingcard = false;
                            cardmanager.cardid = -1;
                            cardmanager.onlinedragging = false;
                            cardmanager.repositionhandcards();
                        }
                        break;
                    case "startdrag":
                        if (!gamecreated) { break; }
                        index = getcardindex(m.GetInt(0), Main.opponentcardmanager);
                        if (index != -1) {
                            Main.opponentcardmanager.cards[index].destdragging = true;
                            Main.opponentcardmanager.cards[index].returningcard = false;
                            Main.opponentcardmanager.cards[index].resting = false;
                            Main.opponentcardmanager.cardid = m.GetInt(0);
                            Main.opponentcardmanager.onlinedragging = true;
                        }
                        break;
                    case "dropcard":
                        if (!gamecreated) { break; }
                        cardmanager = Main.opponentcardmanager;
                        if (m.GetInt(2) == id) { cardmanager = Main.cardmanager; }
                        index = getcardindex(m.GetInt(0), cardmanager);
                        if (index != -1) {
                            cardmanager.cards[index].dropcardonslot(m.GetInt(1), true);
                            cardmanager.cards[index].destdragging = false;
                            cardmanager.cards[index].returningcard = true;
                            cardmanager.cards[index].resting = false;
                            cardmanager.draggingcard = false;
                            cardmanager.cardid = -1;
                            cardmanager.onlinedragging = false;
                            --cardmanager.cardsinhand;
                        }
                        break;
                    case "settarget":
                        if (!gamecreated) { break; }
                        CardManager oppositecardmanager = Main.cardmanager;
                        cardmanager = Main.opponentcardmanager;
                        if (m.GetInt(2) == id) { cardmanager = Main.cardmanager;
                        oppositecardmanager = Main.opponentcardmanager; }
                        index = getcardindex(m.GetInt(0), cardmanager);
                        int oppindex = getcardindex(m.GetInt(1), oppositecardmanager);
                        if (index != -1 && cardmanager.cards[index].onfield &&
                            oppindex != -1 && oppositecardmanager.cards[oppindex].onfield) {
                            cardmanager.cards[index].select(true);
                            cardmanager.cards[index].settarget(oppositecardmanager.cards[oppindex]);
                        }
                        break;
                }
            }
            messages.Clear();
        }

        if (disconnected) {
            Debug.Log("Disconnected");
            infotext.text = "Disconnected";
            joined = false;
            disconnected = false;
        }
    }

    private int getcardindex(int fromid, CardManager manager) {
        for (int n = 0; n < manager.cards.Count; ++n) {
            if (manager.cards[n].details.id == fromid) {
                return n;
            }
        }
        return -1;
    }

    public void startoffline() {
        if (!joined) {
            startgame();
            joined = true;
        }
    }

    public void startonline() {
        if (!joined) {
            connect();
            joined = true;
        }
    }

    public void startgame() {
        if (!gamecreated) {
            Main.cardmanager.initiate(false);
            Main.opponentcardmanager.initiate(true);

            Object.DestroyObject(GameObject.Find("offlinebutton"));
            Object.DestroyObject(GameObject.Find("onlinebutton"));

            Main.pausegame = false;
            GameObject.Find("floor").transform.position = new Vector3(0, -180, 412);
            infotext.text = "";
            gamecreated = true;
        }
    }
}