using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UncoloredSimon : MonoBehaviour
{
    #region Public Unity Fields
    public KMBombInfo Bomb;
    public KMAudio Audio;
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
    bool ModuleSolved;
    bool twoPair = false;
    bool CheckStampIsRunning = false;
    bool playSimon = false;
    bool CheckGridIsRunning = false;
    bool Solved = false;
    ModulePhase currentPhase = ModulePhase.Gray;

    int[] currentStampColor = new int[] { -1, -1, -1, -1 };
    int[] oldStamp;
    Dictionary<ColorNames, List<int>> StampOrderLists;
    Dictionary<int, List<Material>> QColors;
    MeshRenderer[] gridRenderers;
    MeshRenderer[] stampRenderer;
    List<int> stampOrder = new List<int>();
    List<GameObject> playedSimonPhase = new List<GameObject>();
    List<string> SimonPhaseAnswer = new List<string>();
    List<string> SimonPhaseAnswerClone = new List<string>();
    List<Material> ColoredGrid = new List<Material>();
    List<Material> correctStamp = new List<Material>();
    Material[] Grayscale;
    Material[,] stampTable;
    #endregion

    #region Enums
    enum PosInQuadrant
    {
        Top, Right, Down, Left
    }
    enum ModulePhase
    {
        Gray,
        Stamp,
        Simon1,
        Simon2,
        Simon3,
    }
    enum ColorNames
    {
        Blue, Brown, Cyan, Green, Magenta, Purple, Red, Yellow
    }

    public enum ButtonNames
    {
        StampTop, StampRight, StampDown, StampLeft,
        RotateCW, RotateCCW,
        StampSpotTop, StampSpotTopLeft, StampSpotTopRight,
        StampSpotLeft, StampSpotMiddle, StampSpotRight,
        StampSpotDownLeft, StampSpotDownRight, StampSpotDown,
        Q1Top, Q1Down, Q1Left, Q1Right,
        Q2Top, Q2Down, Q2Left, Q2Right,
        Q3Top, Q3Down, Q3Left, Q3Right,
        Q4Top, Q4Down, Q4Left, Q4Right,
        ResetButton, Submit
    }

    enum SoundeffectNames
    {
        Blue, Brown, Cyan, Green, Magenta, Purple, Red, Yellow,
        RotateCW, RotateCCW,
        ShortCorrect, LongCorrect, ShortFail, LongFail, CheckBigGrid
    }
    #endregion

    #region Unity Lifecycle
    void Awake()
    {
        ModuleId = ModuleIdCounter++;
        GetComponent<KMBombModule>().OnActivate += Activate;

        foreach (KMSelectable button in Buttons)
            button.OnInteract += delegate () { InputHandler(button); return false; };
    }

    void Start()
    {
        List<MeshRenderer> renderers = new List<MeshRenderer>();
        foreach (GameObject obj in GridTiles)
            renderers.Add(obj.GetComponent<MeshRenderer>());
        gridRenderers = renderers.ToArray();

        renderers.Clear();
        foreach (GameObject obj in Stamp)
            renderers.Add(obj.GetComponent<MeshRenderer>());
        stampRenderer = renderers.ToArray();

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
    }

    void Update()
    {
        // Nothing needed here right now
    }

    void OnDestroy()
    {
        // Cleanup if needed
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
        if (CheckStampIsRunning) yield break;
        CheckStampIsRunning = true;
        for (int i = 0; i < 4; i++)
        {

            int index = Array.IndexOf(unlitColors, stampRenderer[i].sharedMaterial);
            if (correctStamp[i].name == unlitColors[currentStampColor[i]].name)
            {
                stampRenderer[i].material = litColors[index];
                Playsound((SoundeffectNames)index);
                yield return new WaitForSeconds(.5f);
                continue;
            }
            Strike();
            ChangeStampColor();
            CheckStampIsRunning = false;
            yield break;
        }
        currentPhase = ModulePhase.Stamp;
        CheckStampIsRunning = false;
        Playsound(SoundeffectNames.ShortCorrect);
        ChangeStampColor();
        StartCoroutine(InitiateStampPhaseAnimation());
        GenerateStampPositions();
        GetCorrectGridColors();
    }

    IEnumerator CheckGridAnimation()
    {
        if (CheckGridIsRunning) yield break;
        CheckGridIsRunning = true;
        for (int i = 0; i < gridRenderers.Length; i++)
        {

            int index = Array.IndexOf(unlitColors, gridRenderers[i].sharedMaterial);
            Debug.Log(unlitColors[index]);
            if (ColoredGrid[i].name == unlitColors[index].name)
            {
                gridRenderers[i].material = litColors[index];
                Playsound((SoundeffectNames)index);
                yield return new WaitForSeconds(.2f);
                continue;
            }
            Strike();
            GridToUnlit();
            CheckGridIsRunning = false;
            yield break;
        }
        currentPhase = ModulePhase.Simon1;
        CheckGridIsRunning = false;
        Playsound(SoundeffectNames.ShortCorrect);
        GridToUnlit();
        foreach (MeshRenderer mr in stampRenderer)
        {
            mr.material = NoColor;
        }
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
            yield return new WaitForSeconds(1f / 16f);
        }
    }

    IEnumerator PlaySimonPhase()
    {
        foreach (GameObject Obj in playedSimonPhase)
        {
            Material currentMat = Obj.GetComponent<MeshRenderer>().sharedMaterial;
            int index = Array.FindIndex(unlitColors, m => m.color == currentMat.color);
            Obj.GetComponent<MeshRenderer>().material = litColors[index];
            Playsound((SoundeffectNames)index);
            yield return new WaitForSeconds(.8f);
            Obj.GetComponent<MeshRenderer>().material = unlitColors[index];
        }
        if (playSimon)
        {
            yield return new WaitForSeconds(2.5f);
            StartCoroutine(PlaySimonPhase());
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
        if (Solved) return;
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

        //Catch exceptions and log them
        if (index == -1)
        {
            Debug.Log("Value: " + button + " not valid in the InputHandler");
            return;
        }

        //Log pressed button
        Debug.Log(button);

        //Logic for what button pressed
        ButtonNames btn = (ButtonNames)index;
        switch (btn)
        {
            //Stamp color change
            case ButtonNames.StampTop:
            case ButtonNames.StampRight:
            case ButtonNames.StampDown:
            case ButtonNames.StampLeft:
                if (currentPhase != ModulePhase.Gray || CheckStampIsRunning) return;
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

            //Default if exceptions happen at some point
            default:
                Debug.Log("Unhandled button index: " + index);
                break;
        }
    }
    #endregion

    #region Stamp Logic
    void ChangeStampIndex(int index)
    {
        if (currentPhase != ModulePhase.Gray) { return; }

        if (index < 0 || index >= currentStampColor.Length)
        {
            Debug.Log("An Error accrued when trying to index the 'Stamp' location, invalid value: " + index);
            return;
        }

        currentStampColor[index] = (currentStampColor[index] + 1) % unlitColors.Length;

        Debug.Log("Stamp " + index + " changed color to " + currentStampColor[index]);

        oldStamp = currentStampColor;

        ChangeStampColor();

        Playsound((SoundeffectNames)currentStampColor[index]);
    }

    void ChangeStampColor()
    {
        for (int i = 0; i < 4; i++)
        {
            if (currentStampColor[i] < 0)
            {
                stampRenderer[i].material = NoColor;
                continue;
            }
            stampRenderer[i].material = unlitColors[currentStampColor[i]];
        }
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
            Debug.Log($"StampPos {index}: Row={row + 1}, Col={column + 1}, Color={stampTable[row, column].name}");
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
                gridRenderers[0].material = stampRenderer[0].sharedMaterial;
                gridRenderers[2].material = stampRenderer[1].sharedMaterial;
                gridRenderers[4].material = stampRenderer[2].sharedMaterial;
                gridRenderers[1].material = stampRenderer[3].sharedMaterial;
                break;
            case 2:
                gridRenderers[1].material = stampRenderer[0].sharedMaterial;
                gridRenderers[4].material = stampRenderer[1].sharedMaterial;
                gridRenderers[7].material = stampRenderer[2].sharedMaterial;
                gridRenderers[3].material = stampRenderer[3].sharedMaterial;
                break;
            case 3:
                gridRenderers[3].material = stampRenderer[0].sharedMaterial;
                gridRenderers[7].material = stampRenderer[1].sharedMaterial;
                gridRenderers[10].material = stampRenderer[2].sharedMaterial;
                gridRenderers[6].material = stampRenderer[3].sharedMaterial;
                break;
            case 4:
                gridRenderers[7].material = stampRenderer[0].sharedMaterial;
                gridRenderers[11].material = stampRenderer[1].sharedMaterial;
                gridRenderers[13].material = stampRenderer[2].sharedMaterial;
                gridRenderers[10].material = stampRenderer[3].sharedMaterial;
                break;
            case 5:
                gridRenderers[11].material = stampRenderer[0].sharedMaterial;
                gridRenderers[14].material = stampRenderer[1].sharedMaterial;
                gridRenderers[15].material = stampRenderer[2].sharedMaterial;
                gridRenderers[13].material = stampRenderer[3].sharedMaterial;
                break;
            case 6:
                gridRenderers[8].material = stampRenderer[0].sharedMaterial;
                gridRenderers[12].material = stampRenderer[1].sharedMaterial;
                gridRenderers[14].material = stampRenderer[2].sharedMaterial;
                gridRenderers[11].material = stampRenderer[3].sharedMaterial;
                break;
            case 7:
                gridRenderers[5].material = stampRenderer[0].sharedMaterial;
                gridRenderers[9].material = stampRenderer[1].sharedMaterial;
                gridRenderers[12].material = stampRenderer[2].sharedMaterial;
                gridRenderers[8].material = stampRenderer[3].sharedMaterial;
                break;
            case 8:
                gridRenderers[2].material = stampRenderer[0].sharedMaterial;
                gridRenderers[5].material = stampRenderer[1].sharedMaterial;
                gridRenderers[8].material = stampRenderer[2].sharedMaterial;
                gridRenderers[4].material = stampRenderer[3].sharedMaterial;
                break;
            case 9:
                gridRenderers[4].material = stampRenderer[0].sharedMaterial;
                gridRenderers[8].material = stampRenderer[1].sharedMaterial;
                gridRenderers[11].material = stampRenderer[2].sharedMaterial;
                gridRenderers[7].material = stampRenderer[3].sharedMaterial;
                break;
        }
    }

    void ResetStampPhase()
    {
        currentStampColor = oldStamp;
        ChangeStampColor();
        foreach (MeshRenderer tile in gridRenderers)
        {
            tile.material = NoColor;
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
        for (int i = 0; i < stampOrder.Count; i++)
        {
            Debug.Log(stampOrder[i]);
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
                if (CheckStampIsRunning || currentStampColor.Contains(-1)) return;
                StartCoroutine(CheckStampAnimation());
                break;
            case ModulePhase.Stamp:
                if (CheckGridIsRunning || gridRenderers.Select(r => r.sharedMaterial).Contains(NoColor)) return;
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
            default:
                Debug.Log("Current phase is undefined!");
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
        Solved = true;
        playSimon = false;
        StopAllCoroutines();
        GetComponent<KMBombModule>().HandlePass();
    }

    void Strike()
    {
        Playsound(SoundeffectNames.ShortFail);
        GetComponent<KMBombModule>().HandleStrike();
    }
    #endregion

    #region Twitch Plays
    /* Uncomment if needed
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
    }
    */
    #endregion
}
