If you find anything that you think could be optimized or is wrong, dont wait to contact me^^ Discord: callme_zero


using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;
using Rnd = UnityEngine.Random;
using Math = ExMath;

public class UncoloredSimon : MonoBehaviour
{

    public KMBombInfo Bomb;
    public KMAudio Audio;

    static int ModuleIdCounter = 1;
    int ModuleId;
    private bool ModuleSolved;

    public KMSelectable[] Buttons;

    public Material AnimationWhite;	//White color used for animation purposes
    public Material NoColor;	//Placeholder color when no color is applied to a diamond
    public Material[] Colors;	//List of possible colors for the "Stamp"
	
    public List<GameObject> GridTiles;	//List of all objects that represent the 4x4 grid
    int[] currentStampColor = new int[] { -1, -1, -1, -1};	//Keeping track on which color the "Stamp" currently holds
    public List<GameObject> Stamp;	//List of objects that represent the 2x2 grid of diamonds, the so called Stamp
    List<string> Sounds;	//List of all sound names build from an enum for ease of use

    enum ButtonNames	//Index of each pressable object so i can refrence it by name
    {
        StampTop = 0,
        StampRight = 1,
        StampDown = 2,
        StampLeft = 3,
        RotateCW = 4,
        RotateCCW = 5
    }

    enum SoundeffectNames	//Sound names
    {
        Color1,
        Color2,
        Color3,
        Color4,
        Color5,
        Color6,
        Color7,
        Color8,
        RotateCW,
        RotateCCW,
        Correct,
        CheckBigGrid
    }

    void Awake()
    { //Avoid doing calculations in here regarding edgework. Just use this for setting up buttons for simplicity.
        ModuleId = ModuleIdCounter++;
        GetComponent<KMBombModule>().OnActivate += Activate;
        
        foreach (KMSelectable button in Buttons) {
            button.OnInteract += delegate () { InputHandler(button); return false; };
        }
        

        //button.OnInteract += delegate () { buttonPress(); return false; };

    }

    void OnDestroy()
    { //Shit you need to do when the bomb ends

    }

    void Activate()
    { //Shit that should happen when the bomb arrives (factory)/Lights turn on
        StartCoroutine(CheckBigGridAnimation());
    }

    void Start()
    { //Shit that you calculate, usually a majority if not all of the module
		//Convert enum to string and fill sounds List
        Sounds = new List<string> { SoundeffectNames.Color1.ToString(),
                                    SoundeffectNames.Color2.ToString(),
                                    SoundeffectNames.Color3.ToString(),
                                    SoundeffectNames.Color4.ToString(),
                                    SoundeffectNames.Color5.ToString(),
                                    SoundeffectNames.Color6.ToString(),
                                    SoundeffectNames.Color7.ToString(),
                                    SoundeffectNames.Color8.ToString(),
                                    SoundeffectNames.RotateCW.ToString(),
                                    SoundeffectNames.RotateCCW.ToString(),
                                    SoundeffectNames.CheckBigGrid.ToString(),
                                    SoundeffectNames.Correct.ToString(),
        };
		/*
		get stamp colors by current 4x4 gray scaled grid
		each quadrant stands for one stamp direction (Q1 = Top, Q2 = Right, Q3 = Down, Q4 = Left)
		go off by rules from manual to get the one color that should be put on the stamp
		once you put each color correctly into the stamp and submit it you will enter stamping phase
		in stamping phase you have to use the colors that you got in your stamp and use it to
		figure out the placements and rotations to fill in the 4x4 grid correctly
		you use the pips inbetween the diamonds to color in the sourunding 4
		once you colored in the grid correctly and press submit you start the simon says phase
		in the simon says phase you play simon says in each quadrant to restore the natural look of simon says
		these go off by rules that change depending on the color you stamped into the grid before
		once each quadrant is solved the module will solve it self and you can feel acomplished by yourself
		(maybe ill just let the player play a final round of 4x4 simon says)
		
		
		Goal is to not simply make lookup tables but rather flexible rules that dont feel like looking up a value
		the grayscale can be patterns or amounts of same gray or position of uniqe grays to get a color for for that position (maybe needs to include edgework to not make a huge table)
		Stamp colors could get ecycled into the same table with maybe some rulechanges, using only the same table sounds fun and you dont have to scroll back and forth
		
		*/
    }

    void Update()
    { //Shit that happens at any point after initialization

    }

	//Changing color for "Stamp" cycling between 8 colors
    void ChangeColorStamp(int stampIndex)
    {
        switch (stampIndex)
        {
            case (0):
                currentStampColor[(int)ButtonNames.StampTop] += 1;
                    Debug.Log(currentStampColor[(int)ButtonNames.StampTop]);
                if (currentStampColor[(int)ButtonNames.StampTop] >= Colors.Length)
                {
                    currentStampColor[(int)ButtonNames.StampTop] = 0;
                }
                Stamp[(int)ButtonNames.StampTop].GetComponent<MeshRenderer>().material = Colors[currentStampColor[(int)ButtonNames.StampTop]];
                Audio.PlaySoundAtTransform(Sounds[currentStampColor[(int)ButtonNames.StampTop]], transform);
                break;
            case (1):
                currentStampColor[(int)ButtonNames.StampRight] += 1;
                Debug.Log(currentStampColor[(int)ButtonNames.StampRight]);
                if (currentStampColor[(int)ButtonNames.StampRight] >= Colors.Length)
                {
                    currentStampColor[(int)ButtonNames.StampRight] = 0;
                }
                Stamp[(int)ButtonNames.StampRight].GetComponent<MeshRenderer>().material = Colors[currentStampColor[(int)ButtonNames.StampRight]];
                Audio.PlaySoundAtTransform(Sounds[currentStampColor[(int)ButtonNames.StampRight]], transform);
                break;
            case (2):
                currentStampColor[(int)ButtonNames.StampDown] += 1;
                Debug.Log(currentStampColor[(int)ButtonNames.StampDown]);
                if (currentStampColor[(int)ButtonNames.StampDown] >= Colors.Length)
                {
                    currentStampColor[(int)ButtonNames.StampDown] = 0;
                }
                Stamp[(int)ButtonNames.StampDown].GetComponent<MeshRenderer>().material = Colors[currentStampColor[(int)ButtonNames.StampDown]];
                Audio.PlaySoundAtTransform(Sounds[currentStampColor[(int)ButtonNames.StampDown]], transform);
                break;
            case (3):
                currentStampColor[(int)ButtonNames.StampLeft] += 1;
                Debug.Log(currentStampColor[(int)ButtonNames.StampLeft]);
                if (currentStampColor[(int)ButtonNames.StampLeft] >= Colors.Length)
                {
                    currentStampColor[(int)ButtonNames.StampLeft] = 0;
                }
                Stamp[(int)ButtonNames.StampLeft].GetComponent<MeshRenderer>().material = Colors[currentStampColor[(int)ButtonNames.StampLeft]];
                Audio.PlaySoundAtTransform(Sounds[currentStampColor[(int)ButtonNames.StampLeft]], transform);
                break;
            default:
			Debug.Log("An Error accured when trying to index the 'Stamp' location, invalid value: " + index);
                break;
        }
    }

	//Figure out which button has been pressed and continue with logik afterwards
    void InputHandler(KMSelectable button)
    {
        for (int i = 0; i < Buttons.Length; i++)
        {
            if(button == Buttons[i])
            {
                switch (i)
                {
                    case ((int)ButtonNames.StampTop):
                        ChangeColorStamp((int)ButtonNames.StampTop);
                        break;
                    case ((int)ButtonNames.StampRight):
                        ChangeColorStamp((int)ButtonNames.StampRight);
                        break;
                    case ((int)ButtonNames.StampDown):
                        ChangeColorStamp((int)ButtonNames.StampDown);
                        break;
                    case ((int)ButtonNames.StampLeft):
                        ChangeColorStamp((int)ButtonNames.StampLeft);
                        break;
                    case ((int)ButtonNames.RotateCW):
                        Audio.PlaySoundAtTransform(Sounds[8], transform);
                        break;
                    case ((int)ButtonNames.RotateCCW):
                        Audio.PlaySoundAtTransform(Sounds[9], transform);
                        break;
                    default:
					Debug.Log("Value: " + button + " not valid in the InputHandler");
                        break;
                }
            }
        }
    }

	//Startup animation toggling some diamonds to white and back to black while a tune is playing
    IEnumerator CheckBigGridAnimation()
    {
        Audio.PlaySoundAtTransform(Sounds[10], transform);
        for (int i = 0; i < GridTiles.Count; i++)
        {
            GridTiles[i].GetComponent<MeshRenderer>().material = AnimationWhite;
            yield return new WaitForSeconds(.122f);
            GridTiles[i].GetComponent<MeshRenderer>().material = NoColor;
        }
        yield return null;
    }

    void Solve()	//Solves module
    {
        GetComponent<KMBombModule>().HandlePass();
    }

    void Strike()	//Recieve a strike
    {
        GetComponent<KMBombModule>().HandleStrike();
    }
    /* Delete this if you dont want TP integration
#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"Use !{0} to do something.";
#pragma warning restore 414

    IEnumerator ProcessTwitchCommand(string Command)
    {
        yield return null;
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        yield return null;
    }*/
}
