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

    public Material AnimationWhite;
    public Material NoColor;
    public Material[] Colors;
    public List<GameObject> GridTiles;
    int[] currentStampColor = new int[] { -1, -1, -1, -1};
    public List<GameObject> Stamp;
    List<string> Sounds;

    enum ButtonNames
    {
        StampTop = 0,
        StampRight = 1,
        StampDown = 2,
        StampLeft = 3,
        RotateCW = 4,
        RotateCCW = 5
    }

    enum SoundeffectNames
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
    }

    void Update()
    { //Shit that happens at any point after initialization

    }

    void ChangeColorStamp(int index)
    {
        switch (index)
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
                break;
        }
    }

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
                        break;
                }
            }
        }
    }

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

    void Solve()
    {
        GetComponent<KMBombModule>().HandlePass();
    }

    void Strike()
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
