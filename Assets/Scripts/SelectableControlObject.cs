using System.Collections.Generic;
using UnityEngine;

public class SelectableControlObject : MonoBehaviour
{
    [HideInInspector]
    public int slotNumber = 1;
    public string displayName = "Object";
    public Sprite icon;
    public List<ControlAction> actions = new List<ControlAction>();
}
