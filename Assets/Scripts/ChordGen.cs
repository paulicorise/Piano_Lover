using UnityEngine;

public class ChordGen : MonoBehaviour
{
    public enum ChordFamily { Triad, Seventh, Ninth, Sixth }

    [SerializeField] private bool generateOnStart = true;
    [Tooltip("Octave of the root note. Middle C is C4 (MIDI 60).")]
    [SerializeField, Range(0, 7)] private int rootOctave = 4;

    public ChordFamily CurrentFamily { get; private set; }
    public string ChordName { get; private set; }
    public int RootMidiNote { get; private set; }
    public System.Collections.Generic.IReadOnlyList<int> MidiNotes { get; private set; }
        = System.Array.AsReadOnly(new int[0]);

    private static readonly string[] NoteNames =
        { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };

    private sealed class ChordDefinition
    {
        public readonly string Suffix;
        public readonly int[] Intervals;

        public ChordDefinition(string suffix, params int[] intervals)
        {
            Suffix = suffix;
            Intervals = intervals;
        }
    }

    // Semitone offsets from the root, in root position.
    // Ninth chords include the seventh; sixth chords use an added sixth.
    private static readonly ChordDefinition[][] Chords =
    {
        new[]
        {
            new ChordDefinition("", 0, 4, 7),
            new ChordDefinition("m", 0, 3, 7),
            new ChordDefinition("dim", 0, 3, 6),
            new ChordDefinition("aug", 0, 4, 8)
        },
        new[]
        {
            new ChordDefinition("maj7", 0, 4, 7, 11),
            new ChordDefinition("7", 0, 4, 7, 10),
            new ChordDefinition("m7", 0, 3, 7, 10),
            new ChordDefinition("m7b5", 0, 3, 6, 10),
            new ChordDefinition("dim7", 0, 3, 6, 9),
            new ChordDefinition("m(maj7)", 0, 3, 7, 11)
        },
        new[]
        {
            new ChordDefinition("maj9", 0, 4, 7, 11, 14),
            new ChordDefinition("9", 0, 4, 7, 10, 14),
            new ChordDefinition("m9", 0, 3, 7, 10, 14)
        },
        new[]
        {
            new ChordDefinition("6", 0, 4, 7, 9),
            new ChordDefinition("m6", 0, 3, 7, 9)
        }
    };

    // Match pitch classes so inversions and doubled notes are recognized too.
    public static string DescribePlayedChord(System.Collections.Generic.IEnumerable<int> notes)
    {
        var pitchClasses = new System.Collections.Generic.HashSet<int>();
        foreach (int note in notes) pitchClasses.Add(note % 12);
        if (pitchClasses.Count == 0) return "--";

        var matches = new System.Collections.Generic.List<string>();
        foreach (ChordDefinition[] family in Chords)
        {
            foreach (ChordDefinition chord in family)
            {
                if (chord.Intervals.Length != pitchClasses.Count) continue;
                for (int root = 0; root < NoteNames.Length; root++)
                {
                    bool matchesAll = true;
                    foreach (int interval in chord.Intervals)
                    {
                        if (!pitchClasses.Contains((root + interval) % 12))
                        {
                            matchesAll = false;
                            break;
                        }
                    }
                    if (matchesAll) matches.Add(NoteNames[root] + chord.Suffix);
                }
            }
        }
        // Some note sets have multiple valid names (for example Am7 and C6).
        if (matches.Count > 0) return string.Join(" or ", matches);

        var names = new System.Collections.Generic.List<string>();
        for (int pitch = 0; pitch < NoteNames.Length; pitch++)
            if (pitchClasses.Contains(pitch)) names.Add(NoteNames[pitch]);
        return "Notes: " + string.Join(" ", names);
    }

    private void Start()
    {
        if (generateOnStart && MidiNotes.Count == 0)
            GenerateRandomChord();
    }

    // Can also be connected to a UI Button's OnClick event.
    [ContextMenu("Generate Random Chord")]
    public void GenerateRandomChord()
    {
        GenerateChord((ChordFamily)Random.Range(0, Chords.Length));
    }

    public void GenerateChord(ChordFamily family)
    {
        int familyIndex = (int)family;
        if (familyIndex < 0 || familyIndex >= Chords.Length)
            throw new System.ArgumentOutOfRangeException(nameof(family));

        ChordDefinition[] choices = Chords[familyIndex];
        ChordDefinition chord = choices[Random.Range(0, choices.Length)];
        int root = Random.Range(0, NoteNames.Length);

        CurrentFamily = family;
        // The octave limit keeps even the highest ninth within MIDI 0..127.
        RootMidiNote = (Mathf.Clamp(rootOctave, 0, 7) + 1) * 12 + root;
        ChordName = NoteNames[root] + chord.Suffix;

        int[] notes = new int[chord.Intervals.Length];
        for (int i = 0; i < notes.Length; i++)
            notes[i] = RootMidiNote + chord.Intervals[i];

        MidiNotes = System.Array.AsReadOnly(notes);
        Debug.Log($"{ChordName} ({CurrentFamily}): MIDI [{string.Join(", ", notes)}]", this);
    }
}

