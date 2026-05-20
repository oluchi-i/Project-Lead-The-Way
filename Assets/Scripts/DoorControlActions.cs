using DoorScript;
using UnityEngine;

[RequireComponent(typeof(SelectableControlObject))]
public class DoorControlActions : MonoBehaviour
{
    [SerializeField] private Door door;
    [SerializeField] private int slotNumber = 1;
    [SerializeField] private string displayName = "Door";
    [SerializeField] private Sprite icon;
    [SerializeField] private Sprite actionIcon;

    private void Awake()
    {
        if (door == null)
            door = GetComponentInChildren<Door>();

        var selectable = GetComponent<SelectableControlObject>();
        selectable.slotNumber = slotNumber;
        selectable.displayName = displayName;
        selectable.icon = icon;

        if (selectable.actions.Count == 0)
        {
            var action = new ControlAction { label = "Open / Close", icon = actionIcon };
            action.onSelected.AddListener(ToggleDoor);
            selectable.actions.Add(action);
        }
    }

    public void ToggleDoor()
    {
        if (door != null)
            door.OpenDoor();
    }
}
