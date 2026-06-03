using System;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class ControlAction
{
    public string label = "Use";
    public Sprite icon;
    public UnityEvent onSelected = new UnityEvent();

    public void Invoke()
    {
        onSelected?.Invoke();
    }
}
