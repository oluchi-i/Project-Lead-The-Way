using UnityEngine;

public sealed class MapPathControlPoint : MonoBehaviour
{
    [SerializeField] private string controlPointId = "control-point";

    public string ControlPointId => controlPointId;

    public void Configure(string id)
    {
        controlPointId = id;
    }
}
