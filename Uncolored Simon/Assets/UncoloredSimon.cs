using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Enums;

public class UncoloredSimon : MonoBehaviour
{
    #region Public Unity Fields
    public KMBombInfo Bomb;
    public KMAudio Audio;
    public KMColorblindMode Colorblind;
    public Transform ModuleTransform;
    public KMSelectable[] Buttons;
    public Material NoColor;
    public Material[] unlitColors;
    public Material[] litColors;
    public Material[] Grays;
    public List<GameObject> GridTiles;
    public List<GameObject> Stamp;
    #endregion

    #region Private Fields
    static int ModuleIdCounter = 1;
    int ModuleId;
    bool ModuleSolved = false;
    bool twoPair = false;
    bool isActivated;
    bool isStampCheckInProgress = false;
    bool playSimon = false;
    bool isGridCheckInProgress = false;
    bool clickBufferCooldown = true;
    private bool isColorBlindActive;
    ModulePhase currentPhase = ModulePhase.Gray;

    int[] currentStampColor = new int[] { -1, -1, -1, -1 };
    int[] oldStamp;
    Dictionary<ColorNames, List<int>> StampOrderLists;
    Dictionary<int, List<Material>> QColors;
    MeshRenderer[] gridRenderers;
    MeshRenderer[] stampRenderers;
    List<int> stampOrder = new List<int>();
    List<GameObject> playedSimonPhase = new List<GameObject>();
    List<string> SimonPhaseAnswer = new List<string>();
    List<string> SimonPhaseAnswerClone = new List<string>();
    List<Material> ColoredGrid = new List<Material>();
    List<Material> correctStamp = new List<Material>();
    Material[] Grayscale;
    Material[,] stampTable;

    private char[] gridCBLetters = new char[16];
    #endregion

    #region Unity Lifecycle
    void Awake()
    {
        ModuleId = ModuleIdCounter++;
        GetComponent<KMBombModule>().OnActivate += Activate;

        isColorBlindActive = Colorblind.ColorblindModeActive;

        foreach (KMSelectable button in Buttons)
            button.OnInteract += delegate () { InputHandler(button); return false; };
    }

    void Start()
    {
        gridRenderers = GridTiles.Select(obj => obj.GetComponent<MeshRenderer>()).ToArray();
        stampRenderers = Stamp.Select(obj => obj.GetComponent<MeshRenderer>()).ToArray();

        ResetCB();

        stampTable = new Material[4, 4]
        {
            { unlitColors[(int)ColorNames.Cyan],    unlitColors[(int)ColorNames.Red],    unlitColors[(int)ColorNames.Brown],   unlitColors[(int)ColorNames.Purple] },
            { unlitColors[(int)ColorNames.Purple],  unlitColors[(int)ColorNames.Yellow], unlitColors[(int)ColorNames.Magenta], unlitColors[(int)ColorNames.Green] },
            { unlitColors[(int)ColorNames.Brown],   unlitColors[(int)ColorNames.Green],  unlitColors[(int)ColorNames.Cyan],    unlitColors[(int)ColorNames.Blue] },
            { unlitColors[(int)ColorNames.Magenta], unlitColors[(int)ColorNames.Blue],   unlitColors[(int)ColorNames.Red],     unlitColors[(int)ColorNames.Yellow] }
        };

        GenerateGrayscale();

        StampOrderLists = new Dictionary<ColorNames, List<int>>
        {
            { ColorNames.Blue,    new List<int> { 4, 9, 2, 7, 6, 1, 3, 5, 8 } },
            { ColorNames.Brown,   new List<int> { 6, 3, 1, 9, 8, 2, 5, 4, 7 } },
            { ColorNames.Cyan,    new List<int> { 2, 5, 7, 1, 9, 6, 8, 3, 4 } },
            { ColorNames.Green,   new List<int> { 8, 7, 6, 3, 2, 4, 9, 1, 5 } },
            { ColorNames.Magenta, new List<int> { 7, 8, 3, 2, 5, 9, 4, 6, 1 } },
            { ColorNames.Purple,  new List<int> { 3, 6, 9, 5, 4, 8, 1, 7, 2 } },
            { ColorNames.Red,     new List<int> { 5, 2, 4, 6, 1, 3, 7, 8, 9 } },
            { ColorNames.Yellow,  new List<int> { 1, 4, 5, 8, 3, 7, 2, 9, 6 } },
        };
    }

    void Activate()
    {
        StartCoroutine(DisplayGrayscale());
        isActivated = true;
    }
    #endregion

    #region Coroutines
    IEnumerator DisplayGrayscale()
    {
        Playsound(SoundeffectNames.CheckBigGrid);
        for (int i = 0; i < GridTiles.Count; i++)
        {
            gridRenderers[i].material = Grayscale[i];
            yield return new WaitForSeconds(1.77f / 16f);
        }

        FillQuadrantsWithCurrentColor();
        GetCorrectStampColors();
    }

    IEnumerator CheckStampAnimation()
    {
        if (isStampCheckInProgress) yield break;
        isStampCheckInProgress = true;
        for (int i = 0; i < 4; i++)
        {

            int index = Array.FindIndex(unlitColors, m => m.color == stampRenderers[i].material.color);
            if (correctStamp[i] == unlitColors[currentStampColor[i]])
            {
                stampRenderers[i].material = litColors[index];
                Playsound((SoundeffectNames)index);
                yield return new WaitForSeconds(.5f);
                continue;
            }
            Strike();
            ChangeStampColor();
            isStampCheckInProgress = false;
            yield break;
        }
        currentPhase = ModulePhase.Stamp;
        isStampCheckInProgress = false;
        Playsound(SoundeffectNames.ShortCorrect);
        ChangeStampColor();
        StartCoroutine(InitiateStampPhaseAnimation());
        GenerateStampPositions();
        GetCorrectGridColors();
    }

    IEnumerator CheckGridAnimation()
    {
        if (isGridCheckInProgress) yield break;
        isGridCheckInProgress = true;
        for (int i = 0; i < gridRenderers.Length; i++)
        {

            int index = Array.FindIndex(unlitColors, m => m.color == gridRenderers[i].material.color);
            if (ColoredGrid[i].name == unlitColors[index].name)
            {
                gridRenderers[i].material = litColors[index];
                Playsound((SoundeffectNames)index);
                yield return new WaitForSeconds(.2f);
                continue;
            }
            Strike();
            GridToUnlit();
            isGridCheckInProgress = false;
            yield break;
        }
        currentPhase = ModulePhase.Simon1;
        isGridCheckInProgress = false;
        Playsound(SoundeffectNames.ShortCorrect);
        GridToUnlit();
        foreach (MeshRenderer mr in stampRenderers)
        {
            mr.material = NoColor;
        }
        var stampCB = Enumerable.Range(0, Stamp.Count()).Select(x => Buttons[x].GetComponentInChildren<TextMesh>()).ToArray();

        foreach (var cbText in stampCB)
            cbText.text = string.Empty;
        FillQuadrantsWithCurrentColor();
        GenerateSimonPhase();
        playSimon = true;
        yield return new WaitForSeconds(1.5f);
        StartCoroutine(PlaySimonPhase());
    }

    IEnumerator InitiateStampPhaseAnimation()
    {
        for (int i = 0; i < 16; i++)
        {
            gridRenderers[i].material = NoColor;
            yield return new WaitForSeconds(.015f);
        }
    }

    IEnumerator PlaySimonPhase()
    {
        while (playSimon)
        {
            foreach (GameObject obj in playedSimonPhase)
            {
                var renderer = obj.GetComponent<MeshRenderer>();
                int index = Array.FindIndex(unlitColors, m => m.color == renderer.sharedMaterial.color);
                renderer.material = litColors[index];
                Playsound((SoundeffectNames)index);
                yield return new WaitForSeconds(0.8f);
                renderer.material = unlitColors[index];
            }
            yield return new WaitForSeconds(2.5f);
        }
    }

    IEnumerator LightUpOnClick(int diamondIndex)
    {
        if (clickBufferCooldown)
        {
            clickBufferCooldown = false;
            Material diamondMat = Buttons[diamondIndex].GetComponent<MeshRenderer>().material;
            int index = Array.FindIndex(unlitColors, m => m.color == diamondMat.color);
            Buttons[diamondIndex].GetComponent<MeshRenderer>().material = litColors[index];
            yield return new WaitForSeconds(.05f);
            Buttons[diamondIndex].GetComponent<MeshRenderer>().material = unlitColors[index];
        }
    }

    IEnumerator CheckAFK()
    {
        yield return new WaitForSeconds(3f);
        playSimon = true;
        StartCoroutine(PlaySimonPhase());
    }
    #endregion

    #region Input Handling
    void InputHandler(KMSelectable button)
    {
        if (ModuleSolved || !isActivated) return;
        //Simon Phase play handler
        if (currentPhase >= ModulePhase.Simon1)
        {
            foreach (GameObject Obj in playedSimonPhase)
            {
                int indexColor = Array.IndexOf(litColors, Obj.GetComponent<MeshRenderer>().sharedMaterial);
                if (indexColor != -1) Obj.GetComponent<MeshRenderer>().material = unlitColors[indexColor];
            }
            StopAllCoroutines();
            StartCoroutine(CheckAFK());
        }

        //Get index from array
        int index = Array.IndexOf(Buttons, button);

        //Logic for what button pressed
        ButtonNames btn = (ButtonNames)index;
        switch (btn)
        {
            //Stamp color change
            case ButtonNames.StampTop:
            case ButtonNames.StampRight:
            case ButtonNames.StampDown:
            case ButtonNames.StampLeft:
                if (currentPhase != ModulePhase.Gray || isStampCheckInProgress) return;
                ChangeStampIndex(index);
                break;

            //Stamp rotation
            case ButtonNames.RotateCW:
            case ButtonNames.RotateCCW:
                if (currentPhase != ModulePhase.Stamp) return;
                Playsound((ButtonNames)index == ButtonNames.RotateCW
                    ? SoundeffectNames.RotateCW
                    : SoundeffectNames.RotateCCW);
                RotateStamp(index);
                break;
            case ButtonNames.StampSpotTop:
            case ButtonNames.StampSpotTopLeft:
            case ButtonNames.StampSpotTopRight:
            case ButtonNames.StampSpotLeft:
            case ButtonNames.StampSpotMiddle:
            case ButtonNames.StampSpotRight:
            case ButtonNames.StampSpotDownLeft:
            case ButtonNames.StampSpotDownRight:
            case ButtonNames.StampSpotDown:
                if (currentPhase != ModulePhase.Stamp) return;
                StampInGrid(index - 5);
                break;
            case ButtonNames.Q1Top:
            case ButtonNames.Q1Down:
            case ButtonNames.Q1Left:
            case ButtonNames.Q1Right:
            case ButtonNames.Q2Top:
            case ButtonNames.Q2Down:
            case ButtonNames.Q2Left:
            case ButtonNames.Q2Right:
            case ButtonNames.Q3Top:
            case ButtonNames.Q3Down:
            case ButtonNames.Q3Left:
            case ButtonNames.Q3Right:
            case ButtonNames.Q4Top:
            case ButtonNames.Q4Down:
            case ButtonNames.Q4Left:
            case ButtonNames.Q4Right:
                if (currentPhase < ModulePhase.Simon1) return;
                GameObject pressedButton = Buttons[index].gameObject;
                int colorIndexOfButton = Array.IndexOf(unlitColors.Select(c => c.color).ToArray(), pressedButton.GetComponent<MeshRenderer>().material.color);
                Playsound((SoundeffectNames)colorIndexOfButton);
                clickBufferCooldown = true;
                StartCoroutine(LightUpOnClick(index));
                InputSimonPhase(btn);
                break;
            case ButtonNames.ResetButton:
                if (currentPhase != ModulePhase.Stamp) return;
                ResetStampPhase();
                break;
            case ButtonNames.Submit:
                if (currentPhase >= ModulePhase.Simon1) return;
                CheckCurrentPhase();
                break;
        }
    }
    #endregion

    #region Stamp Logic
    void ChangeStampIndex(int index)
    {
        if (currentPhase != ModulePhase.Gray) { return; }

        currentStampColor[index] = (currentStampColor[index] + 1) % unlitColors.Length;

        oldStamp = currentStampColor;

        ChangeStampColor();

        Playsound((SoundeffectNames)currentStampColor[index]);
    }

    void ChangeStampColor(bool reset = false)
    {

        for (int i = 0; i < 4; i++)
        {
            if (currentStampColor[i] < 0)
            {
                stampRenderers[i].material = NoColor;
                continue;
            }
            stampRenderers[i].material = unlitColors[currentStampColor[i]];
        }

        ToggleColorblind(reset: reset);
    }

    void RotateStamp(int buttonIndex)
    {
        int[] newColors = new int[4];
        if (buttonIndex == (int)ButtonNames.RotateCW)
        {
            newColors[0] = currentStampColor[3];
            newColors[1] = currentStampColor[0];
            newColors[2] = currentStampColor[1];
            newColors[3] = currentStampColor[2];
        }
        else
        {
            newColors[0] = currentStampColor[1];
            newColors[1] = currentStampColor[2];
            newColors[2] = currentStampColor[3];
            newColors[3] = currentStampColor[0];
        }
        currentStampColor = newColors;
        ChangeStampColor();
    }

    void GetCorrectStampColors()
    {
        for (int index = 0; index < QColors.Count; index++)
        {
            int column = GetTableColumn(index);
            int row = GetTableRow(index);
            correctStamp.Add(stampTable[row, column]);
        }
        Debug.LogFormat("[Uncolored Simon #{0}] Correct stamp colors selected: {1}", ModuleId,
        string.Join(", ", correctStamp.Select(c => c.name.Replace(" (Instance)", "").Replace("Unlit", "")).ToArray()));

    }

    void StampInGrid(int pip)
    {
        switch (pip)
        {
            case 1:
                gridRenderers[0].material = stampRenderers[0].sharedMaterial;
                gridRenderers[2].material = stampRenderers[1].sharedMaterial;
                gridRenderers[4].material = stampRenderers[2].sharedMaterial;
                gridRenderers[1].material = stampRenderers[3].sharedMaterial;
                break;
            case 2:
                gridRenderers[1].material = stampRenderers[0].sharedMaterial;
                gridRenderers[4].material = stampRenderers[1].sharedMaterial;
                gridRenderers[7].material = stampRenderers[2].sharedMaterial;
                gridRenderers[3].material = stampRenderers[3].sharedMaterial;
                break;
            case 3:
                gridRenderers[3].material = stampRenderers[0].sharedMaterial;
                gridRenderers[7].material = stampRenderers[1].sharedMaterial;
                gridRenderers[10].material = stampRenderers[2].sharedMaterial;
                gridRenderers[6].material = stampRenderers[3].sharedMaterial;
                break;
            case 4:
                gridRenderers[7].material = stampRenderers[0].sharedMaterial;
                gridRenderers[11].material = stampRenderers[1].sharedMaterial;
                gridRenderers[13].material = stampRenderers[2].sharedMaterial;
                gridRenderers[10].material = stampRenderers[3].sharedMaterial;
                break;
            case 5:
                gridRenderers[11].material = stampRenderers[0].sharedMaterial;
                gridRenderers[14].material = stampRenderers[1].sharedMaterial;
                gridRenderers[15].material = stampRenderers[2].sharedMaterial;
                gridRenderers[13].material = stampRenderers[3].sharedMaterial;
                break;
            case 6:
                gridRenderers[8].material = stampRenderers[0].sharedMaterial;
                gridRenderers[12].material = stampRenderers[1].sharedMaterial;
                gridRenderers[14].material = stampRenderers[2].sharedMaterial;
                gridRenderers[11].material = stampRenderers[3].sharedMaterial;
                break;
            case 7:
                gridRenderers[5].material = stampRenderers[0].sharedMaterial;
                gridRenderers[9].material = stampRenderers[1].sharedMaterial;
                gridRenderers[12].material = stampRenderers[2].sharedMaterial;
                gridRenderers[8].material = stampRenderers[3].sharedMaterial;
                break;
            case 8:
                gridRenderers[2].material = stampRenderers[0].sharedMaterial;
                gridRenderers[5].material = stampRenderers[1].sharedMaterial;
                gridRenderers[8].material = stampRenderers[2].sharedMaterial;
                gridRenderers[4].material = stampRenderers[3].sharedMaterial;
                break;
            case 9:
                gridRenderers[4].material = stampRenderers[0].sharedMaterial;
                gridRenderers[8].material = stampRenderers[1].sharedMaterial;
                gridRenderers[11].material = stampRenderers[2].sharedMaterial;
                gridRenderers[7].material = stampRenderers[3].sharedMaterial;
                break;
        }

        ToggleColorblind(pip - 1, reset: false);
    }

    void ToggleColorblind(int? pip = null, bool tpToggle = false, bool reset = false)
    {
        var getCB = Enumerable.Range(0, 4).Select(x => Buttons[x].GetComponentInChildren<TextMesh>()).ToArray();

        for (int i = 0; i < 4; i++)
        {
            if (currentStampColor[i] < 0)
            {
                getCB[i].text = string.Empty;
                continue;
            }

            var colorName = (ColorNames)currentStampColor[i];

            var cbLetter = colorName == ColorNames.Brown ? 'N' : colorName.ToString()[0];

            getCB[i].text = isColorBlindActive ? cbLetter.ToString() : string.Empty;
            getCB[i].color = cbLetter == 'Y' ? Color.black : Color.white;
        }

        var restCB = GridTiles.Select(x => x.GetComponentInChildren<TextMesh>()).ToArray();

        if (pip == null)
        {
            if (reset)
                foreach (var cb in restCB)
                    cb.text = string.Empty;

            return;
        }
        
        var getColorLetters = new char[4];

        for (int i = 0; i < 4; i++)
        {
            var colorName = (ColorNames)currentStampColor[i];

            getColorLetters[i] = colorName == ColorNames.Brown ? 'N' : colorName.ToString()[0];
        }

        var cbTextsByPip = new[]
        {
            Enumerable.Range(0, 4).ToArray(),
            new[] { 3, 2, 13, 12 },
            new[] { 12, 13, 14, 15 },
            new[] { 13, 8, 11, 14 },
            new[] { 8, 9, 10, 11 },
            new[] { 7, 6, 9, 8 },
            new[] { 4, 5, 6, 7 },
            new[] { 1, 4, 7, 2 },
            new[] { 2, 7, 8, 13 }
        };

        if (!tpToggle)
        {
            for (int i = 0; i < 4; i++)
                gridCBLetters[cbTextsByPip[pip.Value][i]] = getColorLetters[i];
        }

        for (int i = 0; i < 16; i++)
        {
            restCB[i].text = isColorBlindActive ? gridCBLetters[i].ToString() : string.Empty;
            restCB[i].GetComponentInChildren<TextMesh>().color = gridCBLetters[i] == 'Y' ? Color.black : Color.white;
        }
    }

    void ResetCB()
    {
        var filterNonCB = new HashSet<int>
        {
            0, 1, 2, 3,
            15, 16, 17, 18,
            19, 20, 21, 22,
            23, 24, 25, 26,
            27, 28, 29, 30
        };

        var getCB = Enumerable.Range(0, Buttons.Length).Where(filterNonCB.Contains).Select(x => Buttons[x].GetComponentInChildren<TextMesh>()).ToArray();

        foreach (var cbText in getCB)
            cbText.text = string.Empty;
    }

    void ResetStampPhase()
    {
        currentStampColor = oldStamp;
        ChangeStampColor(true);

        for (int i = 0; i < 16; i++)
        {
            gridRenderers[i].material = NoColor;
            gridCBLetters[i] = ' ';
        }
        Playsound(SoundeffectNames.RotateCCW);
    }

    void GenerateStampPositions()
    {
        HashSet<int> used = new HashSet<int>();
        List<List<int>> sequences = new List<List<int>>();
        foreach (int i in currentStampColor)
        {
            sequences.Add(StampOrderLists[(ColorNames)i]);
        }

        for (int pos = 0; stampOrder.Count < 9 ; pos++)
        {
            List<int> candidates = new List<int>();

            foreach (var i in sequences)
            {
                if (pos < i.Count && !used.Contains(i[pos]))
                    candidates.Add(i[pos]);
            }

            if (candidates.Count > 0)
            {
                int min = candidates.Min();
                stampOrder.Add(min);
                used.Add(min);
            }
            else
            {
                for (int n = 1; n <= 9; n++)
                {
                    if (!used.Contains(n))
                    {
                        stampOrder.Add(n);
                        used.Add(n);
                        break;
                    }
                }
            }
        }

        //Logging
        var logParts = new List<string>();
        for (int i = 0; i < stampOrder.Count; i++)
        {
            logParts.Add(stampOrder[i].ToString());
            if (i < stampOrder.Count - 1)
            {
                // Check parity of current number to decide label between current and next
                logParts.Add(stampOrder[i] % 2 == 0 ? "cw" : "ccw");
            }
        }

        Debug.LogFormat("[Uncolored Simon #{0}] Correct pip press sequence: {1}", ModuleId, string.Join(" → ", logParts.ToArray()));

    }

    #endregion

    #region Grid & Color Logic
    void GenerateGrayscale()
    {
        Grayscale = new Material[0];
        for (int i = 0; i < 4; i++)
            Grayscale = Grayscale.Concat(Grays).ToArray();
        Grayscale = Grayscale.Shuffle();
    }

    void FillQuadrantsWithCurrentColor()
    {
        QColors = new Dictionary<int, List<Material>>();
        for (int i = 0; i < 4; i++)
            QColors[i] = new List<Material>();

        string[] quadrantOrder = { "Q1", "Q2", "Q3", "Q4" };
        string[] posOrder = { "Top", "Right", "Down", "Left" };

        GridTiles = GridTiles
            .OrderBy(tile =>
            {
                string name = tile.name;

                int quadIndex = Array.FindIndex(quadrantOrder, q => name.Contains(q));
                int posIndex = Array.FindIndex(posOrder, p => name.Contains(p));

                // Use combined weight to sort: Q1Top < Q1Right < Q1Down < ...
                return quadIndex * 4 + posIndex;
            })
            .ToList();

        foreach (GameObject go in GridTiles)
        {
            Material mat = go.GetComponent<MeshRenderer>().material;
            if (go.name.Contains("Q1")) QColors[0].Add(mat);
            else if (go.name.Contains("Q2")) QColors[1].Add(mat);
            else if (go.name.Contains("Q3")) QColors[2].Add(mat);
            else QColors[3].Add(mat);
        }
    }

    int GetTableColumn(int index)
    {
        var counts = QColors[index]
            .GroupBy(m => m.name)
            .Select(g => g.Count())
            .OrderByDescending(c => c)
            .ToList();

        if (counts.Contains(4)) { twoPair = false; return 3; }
        if (counts.Contains(3)) { twoPair = false; return 2; }
        if (counts.Contains(2) && counts.Count == 2) { twoPair = true; return 1; }
        if (counts.Contains(2)) { twoPair = false; return 1; }

        twoPair = false;
        return 0;
    }

    int GetTableRow(int index)
    {
        var otherMats = QColors.Where(kvp => kvp.Key != index).SelectMany(kvp => kvp.Value).ToList();
        var mostFrequent = ZList.GetMostFrequentBy(otherMats, mat => mat.name);
        Func<Material, int> grayscaleIndex = m => Array.FindIndex(Grays, g => g.color == m.color);

        mostFrequent = twoPair
            ? mostFrequent.OrderByDescending(grayscaleIndex).ToList()
            : mostFrequent.OrderBy(grayscaleIndex).ToList();

        return grayscaleIndex(mostFrequent.First());
    }

    void GetCorrectGridColors()
    {
        ColoredGrid = new List<Material>(new Material[gridRenderers.Length]); // 16 tiles
        int[] simulatedStamp = (int[])currentStampColor.Clone(); // Start with correct stamp
        int[] rotation = new int[] { 0, 1, 2, 3 }; // top, right, down, left indices

        foreach (int pip in stampOrder)
        {
            // Place the stamp at the pip location
            PlaceStampInSimulatedGrid(pip, simulatedStamp, rotation);

            // Determine rotation direction
            if (pip % 2 == 0)
            {
                // Even → CW rotation
                rotation = new int[] {
                rotation[3],
                rotation[0],
                rotation[1],
                rotation[2]
            };
            }
            else
            {
                // Odd → CCW rotation
                rotation = new int[] {
                rotation[1],
                rotation[2],
                rotation[3],
                rotation[0]
            };
            }
        }
    }
    void PlaceStampInSimulatedGrid(int pip, int[] simulatedStamp, int[] rotation)
    {
        int top = simulatedStamp[rotation[0]];
        int right = simulatedStamp[rotation[1]];
        int bottom = simulatedStamp[rotation[2]];
        int left = simulatedStamp[rotation[3]];

        switch (pip)
        {
            case 1:
                AssignGrid(0, top);
                AssignGrid(2, right);
                AssignGrid(4, bottom);
                AssignGrid(1, left);
                break;
            case 2:
                AssignGrid(1, top);
                AssignGrid(4, right);
                AssignGrid(7, bottom);
                AssignGrid(3, left);
                break;
            case 3:
                AssignGrid(3, top);
                AssignGrid(7, right);
                AssignGrid(10, bottom);
                AssignGrid(6, left);
                break;
            case 4:
                AssignGrid(7, top);
                AssignGrid(11, right);
                AssignGrid(13, bottom);
                AssignGrid(10, left);
                break;
            case 5:
                AssignGrid(11, top);
                AssignGrid(14, right);
                AssignGrid(15, bottom);
                AssignGrid(13, left);
                break;
            case 6:
                AssignGrid(8, top);
                AssignGrid(12, right);
                AssignGrid(14, bottom);
                AssignGrid(11, left);
                break;
            case 7:
                AssignGrid(5, top);
                AssignGrid(9, right);
                AssignGrid(12, bottom);
                AssignGrid(8, left);
                break;
            case 8:
                AssignGrid(2, top);
                AssignGrid(5, right);
                AssignGrid(8, bottom);
                AssignGrid(4, left);
                break;
            case 9:
                AssignGrid(4, top);
                AssignGrid(8, right);
                AssignGrid(11, bottom);
                AssignGrid(7, left);
                break;
        }
    }
    void AssignGrid(int gridIndex, int colorIndex)
    {
        if (ColoredGrid[gridIndex] == null)
            ColoredGrid[gridIndex] = unlitColors[colorIndex];
        else
            ColoredGrid[gridIndex] = unlitColors[colorIndex];
    }
    void GridToUnlit()
    {
        for (int i = 0; i < gridRenderers.Length; i++)
        {

            int index = Array.IndexOf(litColors, gridRenderers[i].sharedMaterial);
            if (index != -1)
            {
                gridRenderers[i].material = unlitColors[index];
                continue;
            }
            return;
        }
    }
    #endregion

    #region Simon Logic

    void GenerateSimonPhase()
    {
        playedSimonPhase = GridTiles.Shuffle().Take(5).ToList();
        Debug.LogFormat("[Uncolored Simon #{0}] Simon Phase {1} - Given sequence: {2}", ModuleId,
        currentPhase,
        string.Join(" → ", playedSimonPhase.Select(g => g.name).ToArray()));
        GetCorrectSimonAnswer();
    }

    void GetCorrectSimonAnswer()
    {
        List<PosInQuadrant> posInQuadrants = new List<PosInQuadrant>();
        List<ColorNames> colors = new List<ColorNames>();
        SimonPhaseAnswer.Clear();

        foreach (GameObject go in playedSimonPhase)
        {
            // Invert position in quadrant
            string pos = go.name;
            if (pos.Contains("Top")) posInQuadrants.Add(PosInQuadrant.Down);
            else if (pos.Contains("Right")) posInQuadrants.Add(PosInQuadrant.Left);
            else if (pos.Contains("Down")) posInQuadrants.Add(PosInQuadrant.Top);
            else posInQuadrants.Add(PosInQuadrant.Right);

            // Extract color from material name
            string color = go.GetComponent<MeshRenderer>().material.name;
            if (color.Contains("Blue")) colors.Add(ColorNames.Blue);
            else if (color.Contains("Brown")) colors.Add(ColorNames.Brown);
            else if (color.Contains("Cyan")) colors.Add(ColorNames.Cyan);
            else if (color.Contains("Green")) colors.Add(ColorNames.Green);
            else if (color.Contains("Magenta")) colors.Add(ColorNames.Magenta);
            else if (color.Contains("Purple")) colors.Add(ColorNames.Purple);
            else if (color.Contains("Red")) colors.Add(ColorNames.Red);
            else colors.Add(ColorNames.Yellow);
        }

        colors.Reverse();

        for (int i = 0; i < 5; i++)
        {
            PosInQuadrant startPos = posInQuadrants[i];
            ColorNames color = colors[i];
            int currentQuadrant = (int)startPos;

            bool clockwise = color == ColorNames.Red || color == ColorNames.Blue || color == ColorNames.Yellow;
            int[] dir = clockwise ? new[] { 0, 1, 2, 3 } : new[] { 0, 3, 2, 1 };

            while (true)
            {
                if (!QColors.ContainsKey(currentQuadrant) || QColors[currentQuadrant].Count < 4)
                    break;

                List<Material> quadrantMats = QColors[currentQuadrant];

                foreach (int d in dir)
                {
                    if (quadrantMats[d].name.ToLower().Contains(color.ToString().ToLower()))
                    {
                        string quadStr = $"Q{currentQuadrant + 1}";
                        string[] posNames = { "Top", "Right", "Down", "Left" };
                        string targetName = quadStr + posNames[d];

                        GameObject target = GridTiles.FirstOrDefault(g => g.name == targetName);
                        if (target != null)
                            SimonPhaseAnswer.Add(target.name);

                        goto Next;
                    }
                }

                currentQuadrant = clockwise ? (currentQuadrant + 1) % 4 : (currentQuadrant + 3) % 4;
            }

        Next:;
        }
        foreach (string i in SimonPhaseAnswer) Debug.Log(i);

        Debug.LogFormat("[Uncolored Simon #{0}] Simon Phase {1} - Expected answer: {2}", ModuleId,
        currentPhase,
        string.Join(" → ", SimonPhaseAnswer.ToArray()));
    }

    void InputSimonPhase(ButtonNames buttonName)
    {
        string btn = buttonName.ToString();
        if (SimonPhaseAnswerClone.Count == 0)
        {
            SimonPhaseAnswerClone = SimonPhaseAnswer.ToList();
        }
        if (btn == SimonPhaseAnswerClone.First())
        {
            SimonPhaseAnswerClone.Remove(btn);
        }
        else
        {
            Debug.LogFormat("[Uncolored Simon #{0}] Incorrect input: expected '{1}', but got '{2}'. Resetting Simon sequence.", ModuleId,
            SimonPhaseAnswerClone.First(),
            btn);
            Strike();
            SimonPhaseAnswerClone = SimonPhaseAnswer.ToList();
            return;
        }
        if (SimonPhaseAnswerClone.Count == 0)
        {
            CheckCurrentPhase();
        }
    }
    #endregion

    #region Audio & Control
    void CheckCurrentPhase()
    {
        switch (currentPhase)
        {
            case ModulePhase.Gray:
                if (isStampCheckInProgress || currentStampColor.Contains(-1)) return;
                StartCoroutine(CheckStampAnimation());
                break;
            case ModulePhase.Stamp:
                if (isGridCheckInProgress || gridRenderers.Select(r => r.sharedMaterial).Contains(NoColor)) return;
                StartCoroutine(CheckGridAnimation());
                break;
            case ModulePhase.Simon1:
                currentPhase = ModulePhase.Simon2;
                Playsound(SoundeffectNames.ShortCorrect);
                GenerateSimonPhase();
                break;
            case ModulePhase.Simon2:
                currentPhase = ModulePhase.Simon3;
                Playsound(SoundeffectNames.ShortCorrect);
                GenerateSimonPhase();
                break;
            case ModulePhase.Simon3:
                Solve();
                break;
        }
    }

    void Playsound(SoundeffectNames sound)
    {
        string clipName = sound.ToString();
        try
        {
            Audio.PlaySoundAtTransform(clipName, ModuleTransform);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Sound clip '{clipName}' failed to play. Exception: {e.Message}");
        }
    }

    void Solve()
    {
        Playsound(SoundeffectNames.LongCorrect);
        ModuleSolved = true;
        playSimon = false;
        StopAllCoroutines();
        StartCoroutine(InitiateStampPhaseAnimation());
        ResetCB();
        GetComponent<KMBombModule>().HandlePass();
    }

    void Strike()
    {
        Playsound(SoundeffectNames.ShortFail);
        GridToUnlit();
        GetComponent<KMBombModule>().HandleStrike();
    }
    #endregion

    #region Twitch Plays
#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} cb/colorblind toggles colorblind support || stamp set blue brown magenta cyan sets the colors clockwises starting from the top || stamp rotate cw/ccw rotates the stamp in that direction || pip t/tl/l/dl/dl/dr/r/tr/m presses the pip to set the colors from the stamp || reset to reset grid || submit to submit either the stamp or the grid || q1t q2d q3r q4l to press the buttons in that quadrant and position respectively.";
#pragma warning restore 414

    IEnumerator ProcessTwitchCommand(string command)
    {
        string[] split = command.ToUpperInvariant().Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);

        yield return null;

        if (isGridCheckInProgress || isStampCheckInProgress || !isActivated)
        {
            yield return "sendtochaterror This module cannot be interacted with yet!";
            yield break;
        }

        if (new[] { "CB", "COLORBLIND" }.Any(x => x.ContainsIgnoreCase(split[0])))
        {
            isColorBlindActive = !isColorBlindActive;
            ToggleColorblind(tpToggle: true);
        }

        if ("STAMP".ContainsIgnoreCase(split[0]))
        {
            if (split.Length == 1)
            {
                yield return "sendtochaterror I don't understand.";
                yield break;
            }

            if ("SET".ContainsIgnoreCase(split[1]))
            {
                if (currentPhase != ModulePhase.Gray)
                {
                    yield return "sendtochaterror You have already completed the gray phase!";
                    yield break;
                }

                if (split.Length == 2)
                {
                    yield return "sendtochaterror Please specify what colors to put in clockwise order!";
                    yield break;
                }

                if (split.Skip(2).Count() > 4)
                {
                    yield return "sendtochaterror You're inputting too many colors! Please keep it only to four!";
                    yield break;
                }

                if (split.Skip(2).Count() < 4)
                {
                    yield return "sendtochaterror Please try again.";
                    yield break;
                }

                var validColorNames = new[] { "BLUE", "BROWN", "CYAN", "GREEN", "MAGENTA", "PURPLE", "RED", "YELLOW" };

                if (!split.Skip(2).Any(validColorNames.Contains))
                {
                    yield return $"sendtochaterror {split.Skip(2).Where(x => !validColorNames.Contains(x)).Join(", ")} is/are invalid!";
                    yield break;
                }

                var setupColors = split.Skip(2).Select(x => Array.IndexOf(validColorNames, x)).ToArray();

                for (int i = 0; i < 4; i++)
                    while (currentStampColor[i] != setupColors[i])
                    {
                        Buttons[i].OnInteract();
                        yield return new WaitForSeconds(0.1f);
                    }
            }

            if ("ROTATE".ContainsIgnoreCase(split[1]))
            {
                if (currentPhase != ModulePhase.Stamp)
                {
                    yield return "sendtochaterror You are either not in this phase yet, or you're already past it!";
                    yield break;
                }

                if (split.Length == 2)
                {
                    yield return "sendtochaterror Please specify whether to go clockwise or counterclockwise!";
                    yield break;
                }

                if (split.Length > 3)
                {
                    yield return "sendtochaterror Please try again.";
                    yield break;
                }

                var validRotations = new[] { "CW", "CCW" };

                if (!validRotations.Any(x => x.ContainsIgnoreCase(split[2])))
                {
                    yield return $"sendtochaterror {split[2]} isn't valid!";
                    yield break;
                }

                var rotationIx = split[2] == "CW" ? 4 : 5;

                Buttons[rotationIx].OnInteract();
                yield return new WaitForSeconds(0.1f);
            }

            yield break;
        }

        if ("PIP".ContainsIgnoreCase(split[0]))
        {
            if (currentPhase != ModulePhase.Stamp)
            {
                yield return "sendtochaterror You are either not in this phase yet, or you're already past it!";
                yield break;
            }

            if (split.Length == 1)
            {
                yield return "sendtochaterror I don't understand.";
                yield break;
            }

            if (split.Length > 2)
            {
                yield return "sendtochaterror Please try again.";
                yield break;
            }

            var validPipNames = new[] { "T", "TL", "L", "DL", "D", "DR", "R", "TR", "M" };

            if (validPipNames.All(x => !x.ContainsIgnoreCase(split[1])))
            {
                yield return $"sendtochaterror {split[1]} is invalid!";
                yield break;
            }

            var pipIxes = new[] { 6, 7, 8, 9, 10, 11, 12, 13, 14 };

            Buttons[pipIxes[Array.IndexOf(validPipNames, split[1])]].OnInteract();
            yield return new WaitForSeconds(0.1f);

            yield break;
        }

        if ("RESET".ContainsIgnoreCase(split[0]))
        {
            if (split.Length > 1)
                yield break;

            if (currentPhase != ModulePhase.Stamp)
            {
                yield return "sendtochaterror You cannot reset anymore!";
                yield break;
            }

            Buttons[31].OnInteract();
            yield return new WaitForSeconds(0.1f);
            yield break;
        }

        if ("SUBMIT".ContainsIgnoreCase(split[0]))
        {
            if (split.Length > 1)
                yield break;

            if (currentPhase >= ModulePhase.Simon1)
            {
                yield return "sendtochaterror You cannot press the submit button anymore!";
                yield break;
            }

            Buttons[32].OnInteract();
            yield return new WaitForSeconds(0.1f);
            yield break;
        }

        if (currentPhase >= ModulePhase.Simon1)
        {
            if (!split.All(x => x[0] == 'Q' && "1234".Contains(x[1]) && "TDLR".Contains(x[2])))
            {
                yield return "sendtochaterror Please try your input again.";
                yield break;
            }

            if (split.Any(x => x.Length > 3))
            {
                yield return "sendtochaterror Please try your input again.";
                yield break;
            }

            if (split.Length > 5)
            {
                yield return "sendtochaterror You cannot input more than 5 inputs!";
                yield break;
            }

            var simonIxes = new[]
            {
                new[] { 15, 16, 17, 18 },
                new[] { 19, 20, 21, 22 },
                new[] { 23, 24, 25, 26 },
                new[] { 27, 28, 29, 30 }
            };

            foreach (var cmd in split)
            {
                Buttons[simonIxes["1234".IndexOf(cmd[1])]["TDLR".IndexOf(cmd[2])]].OnInteract();
                yield return new WaitForSeconds(0.1f);
            }
        }

    }

    IEnumerator TwitchHandleForcedSolve()
    {
        while (!isActivated)
            yield return true;
        if (currentPhase == ModulePhase.Gray)
        {
            for (int i = 0; i < 4; i++)
                while (currentStampColor[i] != Array.IndexOf(unlitColors, correctStamp[i]))
                {
                    Buttons[i].OnInteract();
                    yield return new WaitForSeconds(0.1f);
                }
            Buttons[(int)ButtonNames.Submit].OnInteract();
        }

        while (stampOrder.Count == 0)
            yield return true;

        if (currentPhase == ModulePhase.Stamp)
        {
            Buttons[(int)ButtonNames.ResetButton].OnInteract();
            yield return new WaitForSeconds(0.1f);
            for (int i = 0; i < 9; i++)
            {
                Buttons[stampOrder[i] + 5].OnInteract();
                yield return new WaitForSeconds(0.1f);
                if (stampOrder[i] % 2 == 0)
                {
                    Buttons[(int)ButtonNames.RotateCW].OnInteract();
                }
                else
                {
                    Buttons[(int)ButtonNames.RotateCCW].OnInteract();
                }
                yield return new WaitForSeconds(0.1f);
            }
            Buttons[(int)ButtonNames.Submit].OnInteract();
        }

        while (SimonPhaseAnswer.Count == 0)
            yield return true;

        for (int i = 0; i < 15; i++)
        {
            Buttons[(int)Enum.Parse(typeof(ButtonNames), SimonPhaseAnswer[i % 5])].OnInteract();
            yield return new WaitForSeconds(0.1f);
        }
    }
    #endregion
}
