using UnityEngine;
using UnityEngine.UI;

public class InteractionCounterUI : MonoBehaviour
{
    [SerializeField] private InteractionFlowManager interactionFlowManager;
    [SerializeField] private Text countText;
    [SerializeField] private Image remainingFillImage;
    [SerializeField] private string numberFormat = "{0}";

    private InteractionFlowManager subscribedFlowManager;

    private void Awake()
    {
        EnsureReferences();
        Refresh();
    }

    private void OnEnable()
    {
        EnsureReferences();

        Subscribe();

        Refresh();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    public void Configure(InteractionFlowManager newInteractionFlowManager, Text newCountText)
    {
        Configure(newInteractionFlowManager, newCountText, remainingFillImage);
    }

    public void Configure(InteractionFlowManager newInteractionFlowManager, Text newCountText, Image newRemainingFillImage)
    {
        if (interactionFlowManager != newInteractionFlowManager)
            Unsubscribe();

        interactionFlowManager = newInteractionFlowManager;
        countText = newCountText;
        remainingFillImage = newRemainingFillImage;
        Subscribe();
        Refresh();
    }

    public void Refresh()
    {
        var count = interactionFlowManager != null ? interactionFlowManager.InteractionCount : 0;
        HandleInteractionCountChanged(count);
    }

    private void HandleInteractionCountChanged(int count)
    {
        var maxCount = interactionFlowManager != null ? interactionFlowManager.MaxInteractionCount : 1;
        var remainingCount = Mathf.Max(0, maxCount - count);

        if (countText != null)
            countText.text = string.Format(numberFormat, remainingCount, maxCount);

        if (remainingFillImage != null)
            remainingFillImage.fillAmount = maxCount > 0 ? (float)remainingCount / maxCount : 0f;
    }

    private void EnsureReferences()
    {
        if (interactionFlowManager == null)
            interactionFlowManager = FindAnyObjectByType<InteractionFlowManager>();

        if (countText == null)
            countText = GetComponentInChildren<Text>(true);

        if (remainingFillImage == null)
        {
            var images = GetComponentsInChildren<Image>(true);
            foreach (var image in images)
            {
                if (image.type == Image.Type.Filled)
                {
                    remainingFillImage = image;
                    break;
                }
            }
        }
    }

    private void Subscribe()
    {
        if (!isActiveAndEnabled || interactionFlowManager == null || subscribedFlowManager == interactionFlowManager)
            return;

        Unsubscribe();
        interactionFlowManager.InteractionCountChanged += HandleInteractionCountChanged;
        subscribedFlowManager = interactionFlowManager;
    }

    private void Unsubscribe()
    {
        if (subscribedFlowManager == null)
            return;

        subscribedFlowManager.InteractionCountChanged -= HandleInteractionCountChanged;
        subscribedFlowManager = null;
    }
}
