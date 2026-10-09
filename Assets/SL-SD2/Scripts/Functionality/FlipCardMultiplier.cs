using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class FlipCardMultiplier : MonoBehaviour
{
    [Serializable]
    public struct FlipCardData
    {
        public int value;
        public Sprite topSprite;
        public Sprite bottomSprite;
    }

    [Header("References")]
    [SerializeField] private Sprite normalSprite;
    [SerializeField] private Sprite HighlightedSprite;
    [SerializeField] private Image ForthSlotImage;
    [SerializeField] private RectTransform topHalfRect;
    [SerializeField] private Image topHalfImage;
    [SerializeField] private RectTransform bottomHalfRect;
    [SerializeField] private Image bottomHalfImage;

    [Header("Data")]
    [SerializeField] private FlipCardData[] cardData; // 1x, 2x, 5x, 10x

    [Header("Timing")]
    [SerializeField] private float halfFlipDuration = 0.12f;
    [SerializeField] private int shuffleFlips = 3;

    private Sequence masterSequence;
    private int lastShownIndex = -1;

    internal void PlayFlip(int targetValue, Action onComplete = null)
    {
        ForthSlotImage.sprite = HighlightedSprite;
        int targetIndex = Array.FindIndex(cardData, d => d.value == targetValue);
        if (targetIndex < 0)
        {
            Debug.LogWarning($"FlipCardMultiplier: no data for value {targetValue}");
            return;
        }

        masterSequence?.Kill();
        masterSequence = DOTween.Sequence();

        for (int i = 0; i < shuffleFlips; i++)
        {
            int randIndex = GetRandomIndexExcluding(lastShownIndex);
            lastShownIndex = randIndex;
            masterSequence.Append(BuildFlipStep(randIndex));
        }

        lastShownIndex = targetIndex;
        masterSequence.Append(BuildFlipStep(targetIndex));
        masterSequence.OnComplete(() => { onComplete?.Invoke(); });
        masterSequence.Play();
    }

    private Sequence BuildFlipStep(int index)
    {
        FlipCardData data = cardData[index];
        Sequence seq = DOTween.Sequence();

        // Top half flips first
        seq.Append(topHalfRect.DOScaleY(0f, halfFlipDuration).SetEase(Ease.InQuad));
        seq.AppendCallback(() => topHalfImage.sprite = data.topSprite);
        seq.Append(topHalfRect.DOScaleY(1f, halfFlipDuration).SetEase(Ease.OutQuad));

        // Then bottom half
        seq.Append(bottomHalfRect.DOScaleY(0f, halfFlipDuration).SetEase(Ease.InQuad));
        seq.AppendCallback(() => bottomHalfImage.sprite = data.bottomSprite);
        seq.Append(bottomHalfRect.DOScaleY(1f, halfFlipDuration).SetEase(Ease.OutQuad));

        return seq;
    }

    private int GetRandomIndexExcluding(int exclude)
    {
        if (cardData.Length <= 1) return 0;
        int index;
        do { index = UnityEngine.Random.Range(0, cardData.Length); }
        while (index == exclude);
        return index;
    }
    internal void SetNormal()
    {
        ForthSlotImage.sprite = normalSprite;
    }
}