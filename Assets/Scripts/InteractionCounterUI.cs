using UnityEngine;
using UnityEngine.UI;

public class InteractionCounterUI : MonoBehaviour
{
    [SerializeField] private InteractionFlowManager interactionFlowManager;
    [SerializeField] private Text countText;
    [SerializeField] private string numberFormat = "{0}/{1}";

    private void Awake()
    {
        EnsureReferences();
        Refresh();
    }

    private void OnEnable()
    {
        EnsureReferences();

        if (interactionFlowManager != null)
            interactionFlowManager.InteractionCountChanged += HandleInteractionCountChanged;

        Refresh();
    }

    private void OnDisable()
    {
        if (interactionFlowManager != null)
            interactionFlowManager.InteractionCountChanged -= HandleInteractionCountChanged;
    }

    public void Configure(InteractionFlowManager newInteractionFlowManager, Text newCountText)
    {
        interactionFlowManager = newInteractionFlowManager;
        countText = newCountText;
        Refresh();
    }

    public void Refresh()
    {
        var count = interactionFlowManager != null ? interactionFlowManager.InteractionCount : 0;
        HandleInteractionCountChanged(count);
    }

    private void HandleInteractionCountChanged(int count)
    {
        if (countText != null)
        {
            var maxCount = interactionFlowManager != null ? interactionFlowManager.MaxInteractionCount : 1;
            countText.text = string.Format(numberFormat, count, maxCount);
        }
    }

    private void EnsureReferences()
    {
        if (interactionFlowManager == null)
            interactionFlowManager = FindAnyObjectByType<InteractionFlowManager>();

        if (countText == null)
            countText = GetComponentInChildren<Text>(true);
    }
}
