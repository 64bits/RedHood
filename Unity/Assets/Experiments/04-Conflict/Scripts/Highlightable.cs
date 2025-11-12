using UnityEngine;
using System.Collections.Generic;

// Attach this to any object you want to be in the "HighlightLayer"
public class Highlightable : MonoBehaviour
{
    // A static list of all active highlightable objects
    public static List<Renderer> AllHighlightables = new();

    private Renderer _renderer;

    void OnEnable()
    {
        _renderer = GetComponent<Renderer>();
        if (_renderer != null)
        {
            AllHighlightables.Add(_renderer);
        }
    }

    void OnDisable()
    {
        if (_renderer != null)
        {
            AllHighlightables.Remove(_renderer);
        }
    }
}