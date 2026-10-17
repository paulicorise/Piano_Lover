using Minis;
using UnityEngine;
using UnityEngine.InputSystem;

public class MidiController : MonoBehaviour
{
    public static bool IsConnected { get; private set; }
    public event System.Action<int> NotePressed;
    public event System.Action<int> NoteReleased;
    public event System.Action InputReset;
    private SoundGen soundGen;

    private void Awake()
    {
        soundGen = FindAnyObjectByType<SoundGen>();
    }

    private void OnEnable()
    {
        InputSystem.onDeviceChange += OnDeviceChange;

        // Check devices that are already connected
        foreach (var device in InputSystem.devices)
        {
            if (device is MidiDevice midi)
            {
                Connected(midi);
                midi.onWillNoteOn += OnNoteOn;
                midi.onWillNoteOff += OnNoteOff;
            }
        }
    }

    private void OnDisable()
    {
        foreach (var device in InputSystem.devices)
        {
            if (device is MidiDevice midi)
            {
                midi.onWillNoteOn -= OnNoteOn;
                midi.onWillNoteOff -= OnNoteOff;
            }
        }

        InputSystem.onDeviceChange -= OnDeviceChange;
        IsConnected = false;
        InputReset?.Invoke();
    }

    private void OnDeviceChange(InputDevice device, InputDeviceChange change)
    {
        if (device is not MidiDevice midi)
            return;

        switch (change)
        {
            case InputDeviceChange.Added:
                Connected(midi);
                midi.onWillNoteOn += OnNoteOn;
                midi.onWillNoteOff += OnNoteOff;
                break;
            case InputDeviceChange.Reconnected:
                Connected(midi);
                break;

            case InputDeviceChange.Removed:
                midi.onWillNoteOn -= OnNoteOn;
                midi.onWillNoteOff -= OnNoteOff;
                Disconnected(midi);
                break;
            case InputDeviceChange.Disconnected:
                Disconnected(midi);
                break;
        }
    }

    void Connected(MidiDevice device)
    {
        IsConnected = true;
        Debug.Log($"Connected: {device.description.product}");
    }

    void Disconnected(MidiDevice device)
    {
        IsConnected = false;
        InputReset?.Invoke();
        Debug.Log($"Disconnected: {device.description.product}");
    }

    public void OnNoteOn(MidiNoteControl note, float velocity)
    {
        if (velocity <= 0f)
        {
            OnNoteOff(note);
            return;
        }
        NotePressed?.Invoke(note.noteNumber);
        Debug.Log($"Note: {note.noteNumber}, Velocity: {velocity}");
        if (soundGen != null) soundGen.OnKey(note.noteNumber);
    }

    public void OnNoteOff(MidiNoteControl note)
    {
        NoteReleased?.Invoke(note.noteNumber);
        Debug.Log($"Note Off: {note.noteNumber}");
        if (soundGen != null) soundGen.OnKeyOff(note.noteNumber);
    }
}
