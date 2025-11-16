using UnityEngine;
using System.Collections.Generic;

// Attach this to any object you want to be in the "HighlightLayer"
public class Revealable : MonoBehaviour
{
    // A static list of all active highlightable objects
    public static List<Renderer> AllRevealables = new();

    private Renderer _renderer;

    void OnEnable()
    {
        _renderer = GetComponent<Renderer>();
        if (_renderer != null)
        {
            AllRevealables.Add(_renderer);
        }
    }

    void OnDisable()
    {
        if (_renderer != null)
        {
            AllRevealables.Remove(_renderer);
        }
    }
}