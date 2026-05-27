using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(SelectableControlObject))]
public class ControlObjectConnector : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private string displayName = "Object";
    [SerializeField] private Sprite icon;

    [Header("Actions")]
    [SerializeField] private List<ControlAction> actions = new List<ControlAction>();

    public int SlotNumber => 0;
    public string DisplayName => displayName;
    public IReadOnlyList<ControlAction> Actions => actions;

    public void Configure(int ignoredSlotNumber, string newDisplayName, Sprite newIcon, List<ControlAction> newActions)
    {
        displayName = newDisplayName;
        icon = newIcon;
        actions = newActions ?? new List<ControlAction>();
        ApplyToSelectable();
    }

    private void Awake()
    {
        ApplyToSelectable();
    }

    private void OnValidate()
    {
        ApplyToSelectable();
    }

    public void ApplyToSelectable()
    {
        var selectable = GetComponent<SelectableControlObject>();
        if (selectable == null)
            return;

        selectable.displayName = string.IsNullOrWhiteSpace(displayName) ? gameObject.name : displayName;
        selectable.icon = icon;
        selectable.actions = actions;
    }
}
