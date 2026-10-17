using System.Collections.Generic;
using UnityEngine;

// Attach alongside MidiController. The practice display is created automatically.
public class ChordPractice : MonoBehaviour
{
    [SerializeField] private ChordGen generator;
    [SerializeField] private MidiController midi;
    [SerializeField] private PlayerMovement player;
    [SerializeField] private bool showPlayingChord = true;
    [Tooltip("Time after the last key press before judging a held chord.")]
    [SerializeField, Min(0.05f)] private float inputDelay = 0.35f;
    [SerializeField, Min(0f)] private float nextChordDelay = 1.2f;

    private readonly HashSet<int> heldNotes = new HashSet<int>();
    private readonly HashSet<int> target = new HashSet<int>();
    private readonly HashSet<int> played = new HashSet<int>();
    private bool pendingCheck;
    private bool solved;
    private bool waitingForRelease;
    private float checkAt;
    private float nextAt;
    private int correctCount;
    private string playingChord = "--";
    private string feedback = "Play all notes of the chord together.";
    private GUIStyle titleStyle;
    private GUIStyle textStyle;
    private GUIStyle counterStyle;

    public int CorrectCount => correctCount;

    private void Awake()
    {
        if (generator == null) generator = GetComponent<ChordGen>();
        if (generator == null) generator = gameObject.AddComponent<ChordGen>();
        if (midi == null) midi = GetComponent<MidiController>();
        if (midi == null) midi = FindAnyObjectByType<MidiController>();
        if (player == null) player = FindAnyObjectByType<PlayerMovement>();
    }

    private void OnEnable()
    {
        if (midi == null) return;
        midi.NotePressed += Press;
        midi.NoteReleased += Release;
        midi.InputReset += ResetInput;
    }

    private void Start()
    {
        // ChordGen may run Start before or after this component.
        if (generator.MidiNotes.Count == 0) generator.GenerateRandomChord();
    }

    private void OnDisable()
    {
        if (midi != null)
        {
            midi.NotePressed -= Press;
            midi.NoteReleased -= Release;
            midi.InputReset -= ResetInput;
        }
        ResetInput();
    }

    private void ResetInput()
    {
        heldNotes.Clear();
        playingChord = "--";
        pendingCheck = false;
        waitingForRelease = false;
    }

    private void Press(int note)
    {
        heldNotes.Add(note);
        playingChord = ChordGen.DescribePlayedChord(heldNotes);
        if (solved || waitingForRelease) return;
        pendingCheck = true;
        checkAt = Time.unscaledTime + inputDelay;
        feedback = "Listening...";
    }

    private void Release(int note)
    {
        // Check a quick chord before its first note is released.
        if (pendingCheck && !solved) CheckChord();
        heldNotes.Remove(note);
        playingChord = ChordGen.DescribePlayedChord(heldNotes);
        if (waitingForRelease && heldNotes.Count == 0)
        {
            waitingForRelease = false;
            feedback = "Play all notes of the chord together.";
        }
        // Releasing notes alone must not turn a wrong chord into a correct one.
        pendingCheck = false;
    }

    private void Update()
    {
        if (solved)
        {
            if (heldNotes.Count == 0 && Time.unscaledTime >= nextAt)
            {
                SkipChord();
            }
        }
        else if (pendingCheck && Time.unscaledTime >= checkAt)
        {
            CheckChord();
        }
    }

    public void SkipChord()
    {
        if (generator == null) return;
        string previous = generator.ChordName;
        for (int attempt = 0; attempt < 20; attempt++)
        {
            generator.GenerateRandomChord();
            if (generator.ChordName != previous) break;
        }
        solved = false;
        pendingCheck = false;
        waitingForRelease = heldNotes.Count > 0;
        feedback = waitingForRelease
            ? "Release the keys, then play the new chord."
            : "Play all notes of the chord together.";
    }

    private void CheckChord()
    {
        pendingCheck = false;
        if (solved || waitingForRelease) return;
        if (heldNotes.Count == 0 || generator.MidiNotes.Count == 0) return;
        target.Clear();
        played.Clear();
        foreach (int note in generator.MidiNotes) target.Add(note % 12);
        foreach (int note in heldNotes) played.Add(note % 12);

        if (target.SetEquals(played))
        {
            solved = true;
            correctCount++;
            if (player != null) player.Jump();
            nextAt = Time.unscaledTime + nextChordDelay;
            feedback = "Correct! Release the keys for the next chord.";
        }
        else
        {
            feedback = "Try again — release the keys, then play the whole chord.";
        }
    }

    private void OnGUI()
    {
        float toggleWidth = Mathf.Min(200f, Screen.width - 24f);
        if (GUI.Button(new Rect((Screen.width - toggleWidth) / 2f, 12f, toggleWidth, 32f),
            showPlayingChord ? "Hide playing chord" : "Show playing chord"))
        {
            showPlayingChord = !showPlayingChord;
        }
        if (generator == null) return;
        if (titleStyle == null)
        {
            titleStyle = new GUIStyle(GUI.skin.label)
                { fontSize = 44, alignment = TextAnchor.MiddleCenter };
            textStyle = new GUIStyle(GUI.skin.label)
                { fontSize = 18, alignment = TextAnchor.MiddleCenter, wordWrap = true };
            counterStyle = new GUIStyle(textStyle)
                { fontSize = 28, fontStyle = FontStyle.Bold };
        }

        float width = Mathf.Min(600f, Screen.width - 24f);
        GUILayout.BeginArea(new Rect((Screen.width - width) / 2f, 52f, width, 440f), GUI.skin.box);
        GUILayout.Label("CHORD PRACTICE", textStyle);
        GUILayout.Label("Target chord", textStyle);
        GUILayout.Label(generator.ChordName ?? "...", titleStyle, GUILayout.Height(65f));
        GUILayout.Label(generator.CurrentFamily.ToString(), textStyle);
        if (showPlayingChord)
            GUILayout.Label($"Playing: {playingChord}", counterStyle);
        Color previousColor = GUI.color;
        GUI.color = solved ? Color.green : Color.white;
        GUILayout.Label(feedback, textStyle, GUILayout.Height(60f));
        GUI.color = previousColor;
        GUILayout.Label($"Correct chords: {correctCount}", counterStyle);
        GUILayout.Label($"Keys held: {heldNotes.Count}", textStyle);
        GUILayout.Label(midi != null && MidiController.IsConnected
            ? "Any octave or inversion. Include every chord tone."
            : "Connect a MIDI keyboard to play.", textStyle);
        GUILayout.Space(10f);
        if (GUILayout.Button("Skip chord", GUILayout.Height(36f)))
            SkipChord();
        GUILayout.EndArea();
    }
}
