public class Enums
{
    public enum PosInQuadrant
    {
        Top, Right, Down, Left
    }
    public enum ModulePhase
    {
        Gray,
        Stamp,
        Simon1,
        Simon2,
        Simon3,
    }
    public enum ColorNames
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

    public enum SoundeffectNames
    {
        Blue, Brown, Cyan, Green, Magenta, Purple, Red, Yellow,
        RotateCW, RotateCCW,
        ShortCorrect, LongCorrect, ShortFail, LongFail, CheckBigGrid
    }
}
