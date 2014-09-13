using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.IO;
using System.Text;

public class Textures {

	//card textures
	public List<CardDetails> carddetails = new List<CardDetails> ();
    public Texture backcard;

	//particle resources
	public Object energyparticles;
	public Object bleedparticles;

	public void initiatetextures() {
		//create xml object
		XmlReader reader = XmlReader.Create (new StringReader((Resources.Load("carddetails") as TextAsset).text));

		//parse xml
		while (reader.Read ()) {
			if (reader.NodeType == XmlNodeType.Element) {
				string path = reader.GetAttribute("texture"); //get texture path from tag data
				//continue to the next line if the tag encountered doesn't have a texture
				if (path == null) { continue; }

				//creates the carddetails object to store card details from the xml file
				CardDetails details = new CardDetails();
				details.texture = Resources.Load(path) as Texture;

                details.health = int.Parse(reader.GetAttribute("health"));
                details.manacost = int.Parse(reader.GetAttribute("mana"));
                details.damage = int.Parse(reader.GetAttribute("damage"));
				carddetails.Add(details);
			}
		}
		reader.Close ();

		//other cards
		backcard = Resources.Load ("backcard") as Texture;
	}

	public void initiateparticles() {
		energyparticles = Resources.Load("particles/energyparticles") as Object;
		bleedparticles = Resources.Load("particles/bleedparticles") as Object;
	}
}