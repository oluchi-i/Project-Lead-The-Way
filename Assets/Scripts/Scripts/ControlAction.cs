using System;
using UnityEngine;
using UnityEngine.Events;

// ─────────────────────────────────────────────────────────────────────────────
//  ControlAction.cs
//  A serializable action slot used by SelectableControlObject.
//  SelectionPanelsUI shows one button per action and calls Invoke() when clicked.
// ─────────────────────────────────────────────────────────────────────────────

[Serializable]
public class ControlAction
{
    public string label = "Move";
    public Sprite icon;
    public UnityEvent onAction = new UnityEvent();

    public void Invoke()
    {
        if (onAction != null)
            onAction.Invoke();
    }
}
