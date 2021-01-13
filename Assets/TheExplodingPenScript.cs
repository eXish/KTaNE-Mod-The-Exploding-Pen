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
    public TextMesh[] ColorblindText;

	private int NumIndicators = 0;
	private int LastSerialDigit = 0;

	private int AllowedTime = 0;
	private int PenClicks = 0;
	private int AllowedPenClicks = 0;

	private int PenColorIndex = 0;
	private int MessageColorIndex = 0;

	private bool ColorBlindEnabled;
    private bool Waiting;

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
	private string TwitchHelpMessage = "Press the pen with !{0} pen <seconds digits> <times>. Press the submit button with !{0} submit. Toggle colorblind mode with !{0} colorblind.";
	public IEnumerator ProcessTwitchCommand(string command)
	{
        if (Waiting)
        {
            yield return "sendtochaterror Cannot interact with the module while it is resetting.";
            yield break;
        }
        string tpcommand = command.ToUpper();
        if (tpcommand.StartsWith("PEN ") && tpcommand.Split(' ').Length == 3)
        {
            string[] TwitchCommands = tpcommand.Substring(4).Split(' ');
            int Seconds = -1;
            if (!int.TryParse(TwitchCommands[0], out Seconds) || TwitchCommands[0].Length != 2)
            {
                yield return "sendtochaterror The seconds digits '" + command.Split(' ')[1] + "' are invalid!";
                yield break;
            }
            if (Seconds < 0 || Seconds > 59)
            {
                yield return "sendtochaterror The seconds digits '" + command.Split(' ')[1] + "' are invalid!";
                yield break;
            }
            int Times = -1;
            if (!int.TryParse(TwitchCommands[1], out Times))
            {
                yield return "sendtochaterror The number of times '" + command.Split(' ')[2] + "' is invalid!";
                yield break;
            }
            if (Times < 1 || Times > 10)
            {
                yield return "sendtochaterror The number of times '" + command.Split(' ')[2] + "' is invalid!";
                yield break;
            }
            while (Mathf.FloorToInt(Info.GetTime()) % 60 != Seconds) yield return "trycancel The pen wasn't pressed due to a request to cancel.";
            for (int i = 0; i < Times; i++)
            {
                if (Mathf.FloorToInt(Info.GetTime()) % 60 != Seconds)
                {
                    yield return "sendtochat Halted pressing the pen due to a change in the seconds digits of the bomb timer. Successfully pressed the pen " + i + " times before halting.";
                    yield break;
                }
                Pen.OnInteract();
                yield return new WaitForSeconds(0.1f);
            }
        }

        if (tpcommand.Equals("SUBMIT"))
        {
            yield return null;
            Submit.OnInteract();
        }

        if (tpcommand.Equals("COLORBLIND") || tpcommand.Equals("COLOURBLIND"))
        {
            yield return null;
            if (ColorBlindEnabled == true)
            {
                ColorBlindEnabled = false;
                ColorblindText[0].text = "";
                ColorblindText[1].text = "";
            }
            else
            {
                ColorBlindEnabled = true;
                ColorblindOn();
            }
        }
    }

    public IEnumerator TwitchHandleForcedSolve()
    {
        if (PenClicks > AllowedPenClicks)
            PenClicks = 0;
        int times = AllowedPenClicks - PenClicks;
        while (Waiting) yield return true;
        for (int i = 0; i < times; i++)
        {
            while (Mathf.FloorToInt(Info.GetTime()) % 60 != AllowedTime) yield return true;
            Pen.OnInteract();
            yield return new WaitForSeconds(0.1f);
        }
        yield return null;
        Submit.OnInteract();
    }

	void Start () {
		//Colorblind Support
		if (ColorblindMode.ColorblindModeActive == true)
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
        Debug.LogFormat("[The Exploding Pen #{0}] Expected number of clicks: {1}", moduleId, AllowedPenClicks);

		//Debug.LogFormat("[The Exploding Pen #{0}] DEBUG: NumIndicators: " + NumIndicators, moduleId); //Debug Code
		//Debug.LogFormat("[The Exploding Pen #{0}] DEBUG: LastSerialDigit: " + LastSerialDigit, moduleId); //Debug Code
		//Debug.LogFormat("[The Exploding Pen #{0}] DEBUG: AllowedPenClicks: " + AllowedPenClicks, moduleId); //Debug Code

		//Generate a solution.
		GenerateSolution();
	}


	void GenerateSolution()
	{
		//Visual Setup
		PenColorIndex = Random.Range(0, Colors.Length);
		PenRender.material.color = Colors[PenColorIndex];

		MessageColorIndex = Random.Range(0, Colors.Length);
		NotepadRender.GetComponentInChildren<TextMesh>().color = Colors[MessageColorIndex];

		MessageTextIndex = Random.Range(0, MessageText.Length);
		NotepadRender.GetComponentInChildren<TextMesh>().text = MessageText[MessageTextIndex];

		//Visual Setup Logging
		Debug.LogFormat("[The Exploding Pen #{0}] The pen color is {1}.", moduleId, ColorsNames[PenColorIndex]);
		Debug.LogFormat("[The Exploding Pen #{0}] The ink color is {1}.", moduleId, ColorsNames[MessageColorIndex]);
		Debug.LogFormat("[The Exploding Pen #{0}] The person who left you the voice message was {1}.", moduleId, MessageText[MessageTextIndex]);

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
        Waiting = false;
	}


	void ColorblindOn(){
		if (ColorBlindEnabled == true)
		{
			ColorblindText[0].text = "Pen: " + ColorsNames[PenColorIndex];
            ColorblindText[1].text = "Ink: " + ColorsNames[MessageColorIndex];
        }
	}
	void PressedPen(){
		if (moduleSolved == false && !Waiting)
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
						Debug.LogFormat("[The Exploding Pen #{0}] You pressed the pen at XX:0{1}. Expected time XX:{2}. You have clicked the pen {3} times.", moduleId, TimePressed, AllowedTime.ToString("00"), PenClicks);
					}
					
					else
					{
						Debug.LogFormat("[The Exploding Pen #{0}] You pressed the pen at XX:{1}. Expected time XX:{2}. You have clicked the pen {3} times.", moduleId, TimePressed, AllowedTime.ToString("00"), PenClicks);
					}
					
				}

				else
				{
					Pen.AddInteractionPunch();
					Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, Pen.transform);
					Module.HandleStrike();
					NotepadRender.GetComponentInChildren<TextMesh>().text = "Module reset.";
                    if (ColorBlindEnabled == true)
                    {
                        ColorblindText[0].text = "";
                        ColorblindText[1].text = "";
                    }
					NotepadRender.GetComponentInChildren<TextMesh>().color = new Color32(0, 160, 0, 255); // Green
                    Waiting = true;
					Invoke("GenerateSolution", 2);

                if (TimePressed < 10)
                {
                    Debug.LogFormat("[The Exploding Pen #{0}] You pressed the pen at XX:0{1}. Expected time XX:{2}. Strike! Module reset.", moduleId, TimePressed, AllowedTime.ToString("00"));
                }

                else
                {
                    Debug.LogFormat("[The Exploding Pen #{0}] You pressed the pen at XX:{1}. Expected time XX:{2}. Strike! Module reset.", moduleId, TimePressed, AllowedTime.ToString("00"));
                }

            }
	}
		}

	void PressedSubmit(){
        if (moduleSolved == false && !Waiting)
        {
            if (AllowedPenClicks == PenClicks && PenClicks > 0)
            {
                Debug.LogFormat("[The Exploding Pen #{0}] You pressed the submit button. Expected number of clicks: {1}. You clicked the pen {2} times. GG Module solved!", moduleId, AllowedPenClicks, PenClicks);
                Submit.AddInteractionPunch();
                Module.HandlePass();
                Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, Submit.transform);
                Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, transform);
                NotepadRender.GetComponentInChildren<TextMesh>().text = "GG";
                if (ColorBlindEnabled == true)
                {
                    ColorblindText[0].text = "";
                    ColorblindText[1].text = "";
                }
                NotepadRender.GetComponentInChildren<TextMesh>().color = new Color32(0, 160, 0, 255);
                PenRender.material.color = new Color32(0, 160, 0, 255);
                moduleSolved = true;
            }

            else
            {
                Debug.LogFormat("[The Exploding Pen #{0}] You pressed the submit button. Expected number of clicks: {1}. You clicked the pen {2} times. Strike! Module reset", moduleId, AllowedPenClicks, PenClicks);
                PenClicks = 0;
                Submit.AddInteractionPunch();
                Module.HandleStrike();
                Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, Submit.transform);
                NotepadRender.GetComponentInChildren<TextMesh>().text = "Module reset.";
                if (ColorBlindEnabled == true)
                {
                    ColorblindText[0].text = "";
                    ColorblindText[1].text = "";
                }
                NotepadRender.GetComponentInChildren<TextMesh>().color = new Color32(0, 160, 0, 255);
                Waiting = true;
                Invoke("GenerateSolution", 2);
            }
        }
	}
}
