using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using KModkit;

public class TheExplodingPenScript : MonoBehaviour {

	public KMAudio Audio;
	public KMBombModule Module;
	public KMBombInfo Info;
	public KMColorblindMode ColorblindMode;

	public KMSelectable Pen;
	public KMSelectable Submit;

	public MeshRenderer PenRender;
	public MeshRenderer NotepadRender;

	private int NumIndicators = 0;
	private int LastSerialDigit = 0;

	private int AllowedTime = 0;
	private int PenClicks = 0;
	private int AllowedPenClicks = 0;

	private int PenColorIndex = 0;
	private int MessageColorIndex = 0;

	private bool ColorBlindEnabled;

	private static Color[] Colors = new Color[]{

			new Color32(255, 60, 60, 255), //Red
			new Color32(80, 158, 247, 255), //Blue
			new Color32(0, 0, 0, 255), //Black
			new Color32(144, 105, 253, 255) //Purple

	};

	private static string[] ColorsNames = new string[]{
			"Red",
			"Blue",
			"Black",
			"Purple"
 	};

	private int MessageTextIndex = 0;
	private static string[] MessageText = new string[]{
			"Danielstigman",
			"DJHero2903",
			"OEGamer",
			"Riddick",
			"Tathra",
			"Trigger"
	};

	// Tables
	private static readonly int[,] DanielstigmanTable = new int[4, 4]{
			{ 48, 44, 19, 00 },
			{ 10, 18, 55, 59 },
			{ 15, 58, 14, 49 },
			{ 01, 21, 23, 47 }
	};

	private static readonly int[,] DJHero2903Table = new int[4, 4]{
			{ 15, 59, 29, 03 },
			{ 02, 49, 42, 40 },
			{ 56, 48, 19, 06 },
			{ 35, 28, 08, 41 }
	};

	private static readonly int[,] OEGamerTable = new int[4, 4]{
			{ 12, 40, 21, 37 },
			{ 11, 07, 08, 15 },
			{ 25, 36, 49, 35 },
			{ 27, 42, 34, 59 }
	};

	private static readonly int[,] RiddickTable = new int[4, 4]{
			{ 19, 44, 35, 13 },
			{ 07, 49, 59, 09 },
			{ 54, 26, 36, 48 },
			{ 10, 12, 22, 27 }
	};

	private static readonly int[,] TathraTable = new int[4, 4]{
			{ 33, 47, 30, 53 },
			{ 35, 25, 29, 18 },
			{ 11, 06, 31, 08 },
			{ 03, 00, 50, 10 }
	};

	private static readonly int[,] TriggerTable = new int[4, 4]{
			{ 42, 31, 00, 49 },
			{ 08, 07, 37, 06 },
			{ 02, 34, 38, 01 },
			{ 48, 40, 29, 23 }
	};


	//Logging
	static int moduleIdCounter = 1;
	int moduleId;
	private bool moduleSolved;

	void Awake(){
		moduleId = moduleIdCounter++;
		Pen.OnInteract += delegate () { PressedPen(); return false; };
		Submit.OnInteract += delegate () { PressedSubmit(); return false; };
	}

	
	//Twitch Plays Support.
	private string TwitchHelpMessage = "Press the pen with !{0} <seconds digets>. Press the submit button with !{0} submit. Enable colorblind with !{0} colorblind.";
	public IEnumerator ProcessTwitchCommand(string command)
	{
			command = command.ToUpper();
			if (command.Contains("PEN"))
			{
				string TwitchCommand = command.TrimStart('P', 'E', 'N', ' ');
				int Seconds = System.Int16.Parse(TwitchCommand);
				while(Mathf.FloorToInt(Info.GetTime())%60 != Seconds) yield return "trycancel Pen wasn't pressed due to request to cancel";
				yield return new KMSelectable[] {Pen};
			}

			if (command.Contains("SUBMIT"))
			{
				yield return new KMSelectable[] {Submit};
			}

			if (command.Contains("COLORBLIND"))
			{
				ColorBlindEnabled = true;
				ColorblindOn();
			}
	}

	void Start () {
		//Colorblind Support
		if (GetComponent<KMColorblindMode>().ColorblindModeActive == true)
		{
			ColorBlindEnabled = true;
			ColorblindOn();
		}
		
		//Get Edgework
		LastSerialDigit = Info.GetSerialNumberNumbers().Last();
		NumIndicators = Info.GetIndicators().Count();

		if (NumIndicators == 0){
			NumIndicators = 10;
		}

		if (LastSerialDigit == 0){
		LastSerialDigit = 10;
		}

		//Calculate number of clicks.
		AllowedPenClicks = ((LastSerialDigit * NumIndicators) % 10) + 1;
		Debug.LogFormat("[The Exploding Pen #{0}] Expected number of clicks: " + AllowedPenClicks, moduleId);

		//Debug.LogFormat("[The Exploding Pen #{0}] DEBUG: NumIndicators: " + NumIndicators, moduleId); //Debug Code
		//Debug.LogFormat("[The Exploding Pen #{0}] DEBUG: LastSerialDigit: " + LastSerialDigit, moduleId); //Debug Code
		//Debug.LogFormat("[The Exploding Pen #{0}] DEBUG: AllowedPenClicks: " + AllowedPenClicks, moduleId); //Debug Code

		//Generate a solution.
		GenerateSolution();
	}


	void GenerateSolution()
	{
		//Visual Setup
		PenColorIndex = UnityEngine.Random.Range(0, Colors.Length);
		PenRender.material.color = Colors[PenColorIndex];

		MessageColorIndex = UnityEngine.Random.Range(0, Colors.Length);
		NotepadRender.GetComponentInChildren<TextMesh>().color = Colors[MessageColorIndex];

		MessageTextIndex = UnityEngine.Random.Range(0, MessageText.Length);
		NotepadRender.GetComponentInChildren<TextMesh>().text = MessageText[MessageTextIndex];

		//Visual Setup Logging
		Debug.LogFormat("[The Exploding Pen #{0}] The pen color is, " +  ColorsNames[PenColorIndex] + ".", moduleId);
		Debug.LogFormat("[The Exploding Pen #{0}] The ink color is, " +  ColorsNames[MessageColorIndex] + ".", moduleId);
		Debug.LogFormat("[The Exploding Pen #{0}] The the person who called you was, " +  MessageText[MessageTextIndex] + ".", moduleId);

		//Submit Time Setup
		if(MessageText[MessageTextIndex] == "Danielstigman"){
			AllowedTime = DanielstigmanTable[PenColorIndex, MessageColorIndex];
			//Debug.LogFormat("[The Exploding Pen #{0}] DEBUG: Danielstigman AllowedTime XX:" + AllowedTime + " .", moduleId); //Debug Code.
		}

		if(MessageText[MessageTextIndex] == "DJHero2903"){
			AllowedTime = DJHero2903Table[PenColorIndex, MessageColorIndex];
			//Debug.LogFormat("[The Exploding Pen #{0}] DEBUG: DJHero2903 AllowedTime XX:" + AllowedTime + " .", moduleId); //Debug Code.
		}

		if(MessageText[MessageTextIndex] == "OEGamer"){
			AllowedTime = OEGamerTable[PenColorIndex, MessageColorIndex];
			//Debug.LogFormat("[The Exploding Pen #{0}] DEBUG: OEGamer AllowedTime XX:" + AllowedTime + " .", moduleId); //Debug Code.
		}

		if(MessageText[MessageTextIndex] == "Riddick"){
			AllowedTime = RiddickTable[PenColorIndex, MessageColorIndex];
			//Debug.LogFormat("[The Exploding Pen #{0}] DEBUG: Riddick AllowedTime XX:" + AllowedTime + " .", moduleId); //Debug Code.
		}

		if(MessageText[MessageTextIndex] == "Tathra"){
			AllowedTime = TathraTable[PenColorIndex, MessageColorIndex];
			//Debug.LogFormat("[The Exploding Pen #{0}] DEBUG: Tathra AllowedTime XX:" + AllowedTime + " .", moduleId); //Debug Code.
		}

		if(MessageText[MessageTextIndex] == "Trigger"){
			AllowedTime = TriggerTable[PenColorIndex, MessageColorIndex];
			//Debug.LogFormat("[The Exploding Pen #{0}] DEBUG: Trigger AllowedTime XX:" + AllowedTime + " .", moduleId); //Debug Code.
		}

		ColorblindOn();
	}


	void ColorblindOn(){
		if (ColorBlindEnabled == true)
		{
			NotepadRender.GetComponentInChildren<TextMesh>().text = "Pen: " + ColorsNames[PenColorIndex] + "\n" + MessageText[MessageTextIndex] + "\n Ink: " + ColorsNames[MessageColorIndex];
			NotepadRender.GetComponentInChildren<TextMesh>().color = Color.black;
		}
	}
	void PressedPen(){
		if (moduleSolved == false)
		{
			int TimePressed = (int) Info.GetTime() % 60;
			//Debug.LogFormat("[The Exploding Pen #{0}] DEBUG: TimePressed XX:" + TimePressed, moduleId); //Debug Code.

				if(TimePressed == AllowedTime)
				{
					PenClicks++;
					Pen.AddInteractionPunch();
					Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, Pen.transform);
					
					if (TimePressed < 10)
					{
						Debug.LogFormat("[The Exploding Pen #{0}] You pressed the pen at XX:" + "0" + TimePressed + ". Expected time XX:" + AllowedTime + " You have clicked the pen " + PenClicks + " times.", moduleId);
					}
					
					else
					{
						Debug.LogFormat("[The Exploding Pen #{0}] You pressed the pen at XX:" + TimePressed + ". Expected time XX:" + AllowedTime + " You have clicked the pen " + PenClicks + " times.", moduleId);
					}
					
				}

				else
				{
					Pen.AddInteractionPunch();
					Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, Pen.transform);
					Module.HandleStrike();
					NotepadRender.GetComponentInChildren<TextMesh>().text = "Module reset.";
					NotepadRender.GetComponentInChildren<TextMesh>().color = new Color32(0, 160, 0, 255); // Green
					Invoke("GenerateSolution", 2);

					if (TimePressed < 10)
					{
						Debug.LogFormat("[The Exploding Pen #{0}] You pressed the pen at XX:" + "0" + TimePressed + ". Expected time XX:" + AllowedTime + " Strike! Module reset.", moduleId);
					}
					
					else
					{
						Debug.LogFormat("[The Exploding Pen #{0}] You pressed the pen at XX:" + TimePressed + ". Expected time XX:" + AllowedTime + " Strike! Module reset.", moduleId);
					}
					
				}
	}
		}

	void PressedSubmit(){
		if (AllowedPenClicks == PenClicks && PenClicks > 0) {
			Debug.LogFormat("[The Exploding Pen #{0}] You pressed the submit button. Expected number of clicks: " + AllowedPenClicks + " You clicked the pen " + PenClicks + " times. GG Module solved!", moduleId);
			Submit.AddInteractionPunch();
			Module.HandlePass();
			Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, Submit.transform);
			NotepadRender.GetComponentInChildren<TextMesh>().text = "GG";
			NotepadRender.GetComponentInChildren<TextMesh>().color = new Color32(0, 160, 0, 255);
			PenRender.material.color = new Color32(0, 160, 0, 255);
			moduleSolved = true;
		}

		else
		{
			Debug.LogFormat("[The Exploding Pen #{0}] You pressed the submit button. Expected number of clicks: " + AllowedPenClicks + " You clicked the pen " + PenClicks + " times. Strike! Module reset", moduleId);
			PenClicks = 0;
			Submit.AddInteractionPunch();
			Module.HandleStrike();
			Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, Submit.transform);
			NotepadRender.GetComponentInChildren<TextMesh>().text = "Module reset.";
			NotepadRender.GetComponentInChildren<TextMesh>().color = new Color32(0, 160, 0, 255);
			Invoke("GenerateSolution", 2);
		}
	}
}
