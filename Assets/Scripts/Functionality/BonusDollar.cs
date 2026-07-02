using UnityEngine.UI;
using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;

public class BonusDollar : MonoBehaviour
{
    [SerializeField] internal GameObject Highlight;
    [SerializeField] internal SpriteNumberText textField;
    [SerializeField] internal Image BonusResult;
    [SerializeField] internal List<Sprite> ResultSprite;

    [Header("Slide Settings")]
    [SerializeField] private float slideDistance = 150f;
    [SerializeField] private float slideDuration = 0.4f;
    [SerializeField] private Ease slideEase = Ease.OutBack;

    private RectTransform resultRect;
    private Vector2 restingPosition;
    private bool initialized = false;
    private Tweener slideTween;

    private void Awake()
    {
        InitPositions();
    }

    private void InitPositions()
    {
        if (initialized) return;
        resultRect = BonusResult.rectTransform;
        restingPosition = resultRect.anchoredPosition;
        initialized = true;
    }

    internal void ShowResult(int index)
    {
        if (index < 0 || index >= ResultSprite.Count)
        {
            Debug.LogWarning($"BonusDollar: index {index} out of range for ResultSprite (count {ResultSprite.Count})");
            return;
        }

        InitPositions();

        // Swap sprite
        BonusResult.sprite = ResultSprite[index];

        // Reset to above resting position, invisible
        slideTween?.Kill();
        resultRect.anchoredPosition = restingPosition + new Vector2(0f, slideDistance);
        BonusResult.gameObject.SetActive(true);

        // Slide down into place
        slideTween = resultRect.DOAnchorPos(restingPosition, slideDuration).SetEase(slideEase);
    }

    internal void ResetResult()
    {
        slideTween?.Kill();
        InitPositions();
        resultRect.anchoredPosition = restingPosition + new Vector2(0f, slideDistance);
        BonusResult.gameObject.SetActive(false);
    }
}
