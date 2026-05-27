using System.Collections;
using UnityEngine;

public enum ObstacleType { IceCreamScoop, WaffleCone, Popsicle, FrozenBlock, ChocolateBar }

public class ObstacleBlock : MonoBehaviour
{
    [Header("Settings")]
    public ObstacleType obstacleType = ObstacleType.IceCreamScoop;
    public bool         isMoveable   = true;

    [Header("Slide speed")]
    public float slideSpeed = 9f;

    public Vector2Int GridPos { get; private set; }

    static readonly Color[] TypeColors =
    {
        new Color(1.00f, 0.62f, 0.70f),   // IceCreamScoop  – strawberry pink
        new Color(0.98f, 0.82f, 0.47f),   // WaffleCone     – golden waffle
        new Color(0.49f, 0.87f, 0.69f),   // Popsicle       – mint green
        new Color(0.68f, 0.84f, 0.95f),   // FrozenBlock    – icy blue
        new Color(0.48f, 0.31f, 0.18f),   // ChocolateBar   – dark chocolate
    };

    Renderer[] _renderers;
    Color      _baseColor;
    Vector3    _baseScale;
    bool       _selected;
    Coroutine  _slide;

    void Awake()
    {
        _renderers = GetComponentsInChildren<Renderer>(true);
        _baseScale = transform.localScale;
    }

    public void Initialize(Vector2Int pos, ObstacleType type)
    {
        obstacleType = type;
        GridPos      = pos;
        _baseColor   = TypeColors[(int)type % TypeColors.Length];
        Paint(_baseColor);
        transform.position = GridManager.Instance.GridToWorld(pos);
        GridManager.Instance.RegisterObstacle(pos, this);
    }

    public void MoveTo(Vector2Int newPos)
    {
        GridPos = newPos;
        if (_slide != null) StopCoroutine(_slide);
        _slide = StartCoroutine(SlideTo(GridManager.Instance.GridToWorld(newPos)));
    }

    public void SetSelected(bool on)
    {
        if (_selected == on) return;
        _selected = on;
        Color target = on ? Color.Lerp(_baseColor, Color.white, 0.4f) : _baseColor;
        Paint(target);
        transform.localScale = on ? _baseScale * 1.1f : _baseScale;
    }

    IEnumerator SlideTo(Vector3 target)
    {
        Vector3 start   = transform.position;
        float   dist    = Vector3.Distance(start, target);
        float   dur     = dist / Mathf.Max(slideSpeed, 0.1f);
        float   elapsed = 0f;

        while (elapsed < dur)
        {
            elapsed           += Time.deltaTime;
            transform.position = Vector3.Lerp(start, target, Mathf.SmoothStep(0f, 1f, elapsed / dur));
            yield return null;
        }
        transform.position = target;
    }

    void Paint(Color c)
    {
        foreach (Renderer r in _renderers)
            if (r.material != null)
                r.material.color = c;
    }
}
