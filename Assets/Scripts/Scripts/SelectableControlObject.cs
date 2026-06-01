using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class SelectableControlObject : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private string displayName = "Object";
    [SerializeField] private Sprite icon;

    [Header("Actions")]
    [SerializeField] private List<ControlAction> actions = new List<ControlAction>();

    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? gameObject.name : displayName;
    public Sprite Icon => icon;
    public List<ControlAction> Actions => actions;

    public void Configure(string newDisplayName, Sprite newIcon, List<ControlAction> newActions)
    {
        displayName = string.IsNullOrWhiteSpace(newDisplayName) ? gameObject.name : newDisplayName;
        icon = newIcon;
        actions = newActions ?? new List<ControlAction>();
    }
}
