using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public class SlotController : MonoBehaviour
{
  #region References
  [Header("Arrays & Lists")]
  [SerializeField]
  private Sprite[] Slot_Sprites;
  [SerializeField]
  internal Transform[] Slot_Transform;
  [SerializeField]
  private Image[] Stop_Images;
  [SerializeField]
  private ImageAnimation[] Stop_Anims;
  [SerializeField]
  private Sprite[] RedSlot_Sprites;
  [SerializeField]
  internal Transform[] RedSlot_Transform;
  [SerializeField]
  private FlipCardMultiplier forthslot;
  [SerializeField]
  private ImageAnimation[] RedStop_Anims;
  private Dictionary<int, Tweener> alltweens = new(3);
  private Dictionary<int, Tweener> redalltweens = new(3);
  [SerializeField] private Image[] Extra_Images1;
  [SerializeField] private Image[] Extra_Images2;
  [SerializeField] private Image[] Extra_Images3;
  [SerializeField] private Sprite transparentSprite;
  [Header("Animated Sprites")]
  [SerializeField]
  private Sprite[] Symbol1;
  [SerializeField]
  private Sprite[] Symbol2;
  [SerializeField]
  private Sprite[] Symbol3;
  [SerializeField]
  private Sprite[] Symbol4;
  [SerializeField]
  private Sprite[] Symbol5;
  [SerializeField]
  private Sprite[] Symbol6;

  [Header("Red Animated Sprites")]
  [SerializeField]
  private Sprite[] RedSymbol1;
  [SerializeField]
  private Sprite[] RedSymbol2;
  [SerializeField]
  private Sprite[] RedSymbol3;
  [SerializeField]
  private Sprite[] RedSymbol4;
  [SerializeField]
  private Sprite[] RedSymbol5;
  [SerializeField]
  private Sprite[] RedSymbol6;

  internal bool IsSpinning = false;
  internal bool IsAutoSpin = false;

  [Header("Integers")]
  [SerializeField]
  private int IconSizeFactor = 0;
  [SerializeField]
  private int SpaceFactor = 0;
  [SerializeField]
  private int tweenHeight = 0;
  [SerializeField]
  private int MidIconSizeFactor = 0;
  [SerializeField]
  private int MidSpaceFactor = 0;
  [SerializeField]
  private int MidtweenHeight = 0;
  internal int SlotNumber;
  internal double CurrentBet;
  internal int DenomCounter;
  internal int BetCounter;
  internal bool isBonusdone;
  internal bool isThirdRowLocked;
  [Header("Controllers")]
  [SerializeField]
  private UIManager uiController;
  [SerializeField]
  private SocketIOManager socketManager;
  [SerializeField]
  private AudioController audioController;
  #endregion

  [Header("turbo")]
  [SerializeField] internal bool StopSpinToggle;
  [SerializeField] internal bool IsTurboOn;
  private int[] RandomEmptyIndex = new int[3];
  private bool WasAutoSpinOn;
  private float SpinDelay = 0.2f;
  private List<FrozenIndex> frozenIndices;

  private Coroutine AutoSpinRoutine = null;
  private Coroutine tweenroutine = null;
  private List<FrozenIndex> FrozenList = new();
  [Header("sprites")]

  [SerializeField] internal List<ImageAnimation> BorderAnimations;
  [SerializeField] internal List<GameObject> Silverimages;
  private void Start()
  {
    tweenHeight = (15 * IconSizeFactor) - 280;
    MidtweenHeight = (15 * MidIconSizeFactor) - 280;
  }

  internal void UpdateUI(double balance)
  {
    if (uiController) uiController.UpdateBalance(balance);
    string balanceStr = balance.ToString("F2");

    BetCounter = 0;
    if (uiController.BetMain_Text) uiController.BetMain_Text.text = socketManager.InitialData.bets[BetCounter].ToString();


    if (!uiController.CheckBalance(double.Parse(balanceStr)))
    {
      if (uiController) uiController.EnableLowBalance();
    }
    CurrentBet = socketManager.InitialData.bets[BetCounter];
    BonusFeature bs = socketManager.FeaturesData.bonusFeature;
    uiController.SetBonusInit(bs.multiplierValues[3], bs.multiplierValues[4], bs.multiplierValues[5], CurrentBet);
  }
  internal void ChangeBet(bool IncDec)
  {
    if (audioController) audioController.PlayButtonAudio();
    if (IncDec)
    {
      BetCounter++;
      if (BetCounter >= socketManager.InitialData.bets.Count)
      {
        BetCounter = 0; // Loop back to the first bet
      }
    }
    else
    {
      BetCounter--;
      if (BetCounter < 0)
      {
        BetCounter = socketManager.InitialData.bets.Count - 1; // Loop to the last bet
      }
    }
    // if (LineBet_text) LineBet_text.text = SocketManager.InitialData.bets[BetCounter].ToString();
    if (uiController.BetMain_Text) uiController.BetMain_Text.text = socketManager.InitialData.bets[BetCounter].ToString();
    // currentTotalBet = socketManager.InitialData.bets[BetCounter] * Lines;
    // uiManager.InitialiseUIData(socketManager.UIData.paylines);
    CurrentBet = socketManager.InitialData.bets[BetCounter];
    BonusFeature bs = socketManager.FeaturesData.bonusFeature;
    uiController.SetBonusInit(bs.multiplierValues[3], bs.multiplierValues[4], bs.multiplierValues[5], CurrentBet);

  }
  internal void MaxBet()
  {
    BetCounter = socketManager.InitialData.bets.Count - 1;
    if (uiController.BetMain_Text) uiController.BetMain_Text.text = socketManager.InitialData.bets[BetCounter].ToString();
    // currentTotalBet = socketManager.InitialData.bets[BetCounter] * Lines;
  }
  #region AutoSpin
  internal void AutoSpin(int count)
  {
    if (!IsAutoSpin)
    {
      IsAutoSpin = true;

      if (AutoSpinRoutine != null)
      {
        StopCoroutine(AutoSpinRoutine);
        AutoSpinRoutine = null;
      }
      AutoSpinRoutine = StartCoroutine(AutoSpinCoroutine(count));
    }
  }
  internal void StopAutoSpin()
  {
    if (IsAutoSpin)
    {
      IsAutoSpin = false;
      StartCoroutine(StopAutoSpinCoroutine());
    }
  }

  private IEnumerator AutoSpinCoroutine(int count)
  {
    while (IsAutoSpin)
    {
      StartSpin();
      count--;
      if (uiController) uiController.updateAutoCount(count);
      yield return new WaitUntil(() => !IsSpinning);
    }
    StopAutoSpin();
  }

  private IEnumerator StopAutoSpinCoroutine()
  {
    yield return new WaitUntil(() => !IsSpinning);
    if (AutoSpinRoutine != null || tweenroutine != null)
    {
      if (AutoSpinRoutine != null) StopCoroutine(AutoSpinRoutine);
      if (tweenroutine != null) StopCoroutine(tweenroutine);
      tweenroutine = null;
      AutoSpinRoutine = null;
      // removed StopCoroutine(StopAutoSpinCoroutine()) which attempted to stop itself incorrectly
      if (uiController) uiController.ToggleButtonGrp(true);
      if (uiController) uiController.StopAutoSpin();
    }
  }
  private void OnApplicationFocus(bool focus)
  {
    audioController.CheckFocusFunction(focus, true);
  }

  #endregion

  #region SpinLogic
  internal void StartSpin()
  {
    if (tweenroutine != null)
    {
      StopCoroutine(tweenroutine);
    }
    tweenroutine = StartCoroutine(TweenRoutine());
  }



  private IEnumerator TweenRoutine()
  {
    FrozenList = new();
    uiController.ResetWinText();
    // uiController.Locked.SetActive(false);

    if (!uiController.CheckBalance(CurrentBet))
    {
      if (uiController) uiController.EnableLowBalance();
      StopAutoSpin();
      if (uiController) uiController.ToggleButtonGrp(true);
      yield break;
    }

    if (!IsTurboOn && !IsAutoSpin)
    {
      uiController.StopSpin_Button.gameObject.SetActive(true);
    }

    IsSpinning = true;
    ResetAllAnims();
    uiController.resetWinColor();
    if (!isThirdRowLocked) if (uiController) uiController.UpdateTweenBalance(CurrentBet);

    // Only spin reels that are not locked
    int movableReels = isThirdRowLocked ? 2 : 3;

    for (int i = 0; i < movableReels; i++)
    {
      InitializeTweening(Slot_Transform[i], i, true);
      yield return new WaitForSeconds(0.1f);
      Silverimages[i].SetActive(true);
    }

    if (audioController) audioController.PlayWLAudio("spin");

    socketManager.AccumulateResult(BetCounter);
    yield return new WaitUntil(() => socketManager.isResultdone);

    PopulateNormalSpin();

    if (IsTurboOn)
    {
      yield return new WaitForSeconds(0.1f);
    }
    else
    {
      for (int i = 0; i < 10; i++)
      {
        if (StopSpinToggle) break;
        yield return null;
      }
    }

    // Only stop reels that were spinning
    for (int i = 0; i < movableReels; i++)
    {
      yield return StopTweening(5, Slot_Transform[i], i, socketManager.ResultData.payload.winAmount, true);
      Silverimages[i].SetActive(false);
    }
    if (socketManager.ResultData.payload.winAmount > 0)
    {
      if (audioController) audioController.PlayWLAudio("yellow");
      uiController.FullSlotAnim.gameObject.SetActive(true);
      uiController.FullSlotAnim.StartAnimation();
      yield return new WaitForSeconds(1f);
      if (audioController) audioController.PlayWLAudio("cards");
      uiController.FullSlotAnim.StopAnimation();
      uiController.FullSlotAnim.gameObject.SetActive(false);
      forthslot.PlayFlip(socketManager.ResultData.payload.appliedMultiplier, audioController.StopWLAaudio);
      yield return new WaitForSeconds(3f);
    }
    StopSpinToggle = false;
    if (socketManager.ResultData.payload.winAmount > 0)
    {
      if (audioController) audioController.PlayWLAudio("win");
    }

    // Wait for last movable reel to finish
    int lastReel = movableReels - 1;
    if (alltweens.Count > 0 && alltweens.ContainsKey(lastReel))
      yield return alltweens[lastReel].WaitForCompletion();

    KillAllTweens();
    Debug.Log("xxxxxx" + socketManager.ResultData.payload.lockActive);
    if (socketManager.ResultData.payload.lockActive) uiController.PlayLockedAnimation(false);
    if (socketManager.ResultData.payload?.winAmount > 0)
    {
      StartNormalAnimation();
      StartborderAnimation();
      if (socketManager.ResultData.payload?.winAmount < (CurrentBet * 5))
      {
        Debug.Log($"Win Amount: {socketManager.ResultData.payload.winAmount}");
        uiController.ToggleWinPopup(true, System.Math.Round(socketManager.ResultData.payload.winAmount, 2), 0);
        yield return new WaitForSeconds(2f);
        yield return uiController.UpdateWinnings(socketManager.PlayerData.balance, socketManager.ResultData.payload.winAmount);
        uiController.ToggleWinPopup(false);
        // StartCoroutine(uiController.UpdateWinnings(socketManager.PlayerData.balance, socketManager.ResultData.payload.winAmount));
      }
      else yield return uiController.UpdateWinnings(socketManager.PlayerData.balance, socketManager.ResultData.payload.winAmount);
    }
    else
    {
      uiController.ResetWinText();
    }
    if (socketManager.ResultData.payload.isJackpot)
    {
      uiController.ToggleWinPopup(true, System.Math.Round(socketManager.ResultData.payload.winAmount, 4), 0);
      yield return new WaitForSeconds(2f);
      yield return uiController.UpdateWinnings(socketManager.PlayerData.balance, socketManager.ResultData.payload.winAmount);
      uiController.ToggleWinPopup(false);
    }
    if (socketManager.ResultData.payload.isBonusFeatureActive)
    {
      yield return new WaitForSeconds(2f);
      isBonusdone = false;
      switchtoBonusGame();
      yield return new WaitUntil(() => isBonusdone);
      yield return new WaitForSeconds(1f);
      yield return new WaitForSeconds(1f);
    }
    // if (isThirdRowLocked) uiController.PlayLockedAnimation(false);
    // Update lock state for next spin based on fresh result
    // isThirdRowLocked = socketManager.ResultData.payload.lockActive;
    isThirdRowLocked = socketManager.ResultData?.payload?.lockActive ?? false;
    if (!IsAutoSpin && !isThirdRowLocked)
    {
      if (uiController) uiController.ToggleButtonGrp(true);
      uiController.AutoSpinPanel_Button.gameObject.SetActive(true);
      uiController.StopAutoSpin_Button.gameObject.SetActive(false);
    }

    IsSpinning = false;
    if (isThirdRowLocked) StartSpin();
  }

  // void StartborderAnimation()
  // {
  //   for (int i = 0; i < 3; i++)
  //   {

  //     int symbolIndex = int.Parse(socketManager.ResultData.matrix[0][i]);
  //     if (symbolIndex != 0)
  //     {
  //       BorderAnimations[i].StartAnimation();
  //     }


  //   }

  // }
  void StartborderAnimation()
  {
    int[] values = new int[3];

    for (int i = 0; i < 3; i++)
      values[i] = int.Parse(socketManager.ResultData.matrix[0][i]);

    // Find first reel that contributes to the number
    int start = 2; // Default: animate last reel

    for (int i = 0; i < 3; i++)
    {
      // 1,2,3,4 are real values (1,2,5,10)
      // 7 is bonus
      if (values[i] >= 1 && values[i] <= 4)
      {
        start = i;
        break;
      }
    }

    // Animate from the first significant symbol to the end
    for (int i = start; i < 3; i++)
    {
      BorderAnimations[i].StartAnimation();
    }
  }
  void ResetAllAnims()
  {
    ResetNormalAnims();
    ResetRedAnims();
    foreach (var anim in BorderAnimations)
    {
      anim.StopAnimation();
    }
  }

  void ResetRedAnims()
  {
    foreach (ImageAnimation image in RedStop_Anims)
    {
      image.StopAnimation();
    }
  }

  private void ResetNormalAnims()
  {
    for (int i = 0; i < Stop_Anims.Length; i++)
    {
      Stop_Anims[i].StopAnimation();
    }
    uiController.Redline.StopAnimation();
    uiController.Redline2.StopAnimation();
  }
  #endregion



  #region RedSpinLogic




  private void PopulateNormalSpin()
  {
    for (int i = 0; i < 3; i++)
    {

      int symbolIndex = int.Parse(socketManager.ResultData.matrix[0][i]);
      PopulateAnimationSprites(Stop_Anims[i], Stop_Images[i], symbolIndex);
      populateExtraSymbols(i, symbolIndex);

    }
  }
  void populateExtraSymbols(int i, int symbolIndex)
  {
    Image[] extraImages = null;

    switch (i)
    {
      case 0:
        extraImages = Extra_Images1;
        break;
      case 1:
        extraImages = Extra_Images2;
        break;
      case 2:
        extraImages = Extra_Images3;
        break;
    }

    if (symbolIndex == 0)
    {
      // Show extras
      foreach (Image img in extraImages)
      {
        img.gameObject.SetActive(true);

        // Random sprite from index 1 to 5
        int randomIndex = Random.Range(1, 6);
        img.sprite = Slot_Sprites[randomIndex];
      }
      Stop_Images[i].sprite = Slot_Sprites[12];

    }
    else
    {
      // Hide extras
      foreach (Image img in extraImages)
      {
        img.gameObject.SetActive(false);
      }
    }
  }
  private void StartNormalAnimation()
  {
    if (socketManager.ResultData.payload.winAmount <= 0)
    {
      return;
    }
    uiController?.resetWinColor();
    for (int i = 0; i < Stop_Anims.Length; i++)
    {
      if (i > 2)
      {
        break;
      }
      if (Stop_Anims[i].textureArray.Count > 0) Stop_Anims[i].StartAnimation();
      if (int.Parse(socketManager.ResultData.matrix[0][i]) != 0)
      {
        uiController?.AddWinColor(i);
      }
    }
    uiController.Redline.StartAnimation();
    uiController.Redline2.StartAnimation();
  }

  #endregion

  #region TweenLogic
  private void PopulateAnimationSprites(ImageAnimation animScript, Image StopImage, int val)
  {
    animScript.textureArray.Clear();
    animScript.textureArray.TrimExcess();
    animScript.AnimationSpeed = 8;
    switch (val)
    {
      // case 5:
      //   for (int i = 0; i < Symbol1.Length; i++)
      //   {
      //     animScript.textureArray.Add(Symbol1[i]);
      //   }
      //   break;
      // case 1:
      //   for (int i = 0; i < Symbol2.Length; i++)
      //   {
      //     animScript.textureArray.Add(Symbol2[i]);
      //   }
      //   break;
      // case 2:
      //   for (int i = 0; i < Symbol3.Length; i++)
      //   {
      //     animScript.textureArray.Add(Symbol3[i]);
      //   }
      //   break;
      // case 3:
      //   // animScript.AnimationSpeed = 6;
      //   for (int i = 0; i < Symbol4.Length; i++)
      //   {
      //     animScript.textureArray.Add(Symbol4[i]);
      //   }
      //   break;
      // case 4:
      //   for (int i = 0; i < Symbol5.Length; i++)
      //   {
      //     animScript.textureArray.Add(Symbol5[i]);
      //   }
      //   break;
      //   // case 6:
      //   //   for (int i = 0; i < Symbol1.Length; i++)
      //   //   {
      //   //     animScript.textureArray.Add(Symbol1[i]);
      //   //   }
      //   //   break;
    }
    // Debug.Log("Setting sprite val: " + val);
    animScript.AnimationSpeed = animScript.textureArray.Count - 5;
    StopImage.sprite = Slot_Sprites[val];
  }



  private void InitializeTweening(Transform slotTransform, int index, bool isGreen, bool IsMid = false)
  {
    int myTweenHeight = 0;
    if (IsMid)
    {
      myTweenHeight = MidtweenHeight;
    }
    else
    {
      myTweenHeight = tweenHeight;
    }
    Tweener tweener = null;
    if (!isGreen)
    {
      slotTransform.localPosition = new Vector3(slotTransform.localPosition.x, -myTweenHeight, slotTransform.localPosition.z);
      tweener = slotTransform.DOLocalMoveY(0, 0.5f).SetLoops(-1, LoopType.Restart).SetEase(Ease.Linear).SetDelay(0);
      redalltweens[index] = tweener;
    }
    else
    {
      slotTransform.localPosition = new Vector3(slotTransform.localPosition.x, 0f, slotTransform.localPosition.z);
      tweener = slotTransform.DOLocalMoveY(-myTweenHeight, 0.5f).SetLoops(-1, LoopType.Restart).SetEase(Ease.Linear).SetDelay(0);
      alltweens[index] = tweener;
    }
    tweener.Play();
  }


  private IEnumerator StopTweening(int reqpos, Transform slotTransform, int index, double winning, bool isGreen)
  {
    if (!IsTurboOn)
    {
      bool IsRegister = false;
      alltweens[index]?.OnStepComplete(() => { IsRegister = true; });
      yield return new WaitUntil(() => IsRegister);
    }

    if (uiController) uiController.StopSpin_Button.gameObject.SetActive(false);

    alltweens[index].Kill();

    slotTransform.localPosition = new Vector3(slotTransform.localPosition.x, 0f, slotTransform.localPosition.z);
    Tweener t = slotTransform.DOLocalMoveY(-1108 - 200 - 20, 0.3f);
    alltweens[index] = t;

    if (!StopSpinToggle)
    {
      yield return alltweens[index].WaitForCompletion();
    }
    if (audioController) audioController.PlayWLAudio("dot");

  }
  private void KillAllTweens()
  {
    foreach (KeyValuePair<int, Tweener> pair in alltweens)
    {
      pair.Value.Kill();
    }
    alltweens.Clear();
    alltweens.TrimExcess();
  }


  #endregion

  #region Internal Methods

  internal void CallCloseSocket()
  {
    StartCoroutine(socketManager.CloseSocket());
  }

  internal void DisconnectionPopup()
  {
    if (uiController) uiController.DisconnectionPopup();
  }

  internal void PopulateSymbols(Paylines paylines)
  {
    uiController.PopulateSymbolsPayout(paylines);
  }

  #endregion

  #region BonusFeature
  int count = 1;
  int trys = 5;
  // internal void switchtoBonusGame(bool isBonus = false)
  // {
  //   if (!isBonus)
  //   {
  //     uiController.MoveToBonus();
  //     uiController.BonusCenterText.text = "SIMPLE DOLLAR";
  //     count = 0;
  //     trys = 5;
  //     uiController.BonusButtonToggle(true);
  //   }
  //   uiController.setallBonusfalse(); // reset all highlights first
  //   count++;
  //   // uiController.BonusCenterText.text = count + " OF 5 OFFER";
  //   string ps = count + " OF 5 OFFER";
  //   StretchText(ps);
  //   var bonusSettings = socketManager.FeaturesData.bonusSettings;
  //   List<Reward> offers;
  //   if (!isBonus) offers = socketManager.ResultData.payload.bonusData.rewards;
  //   else offers = socketManager.BonusData.payload.bonusData.rewards;

  //   for (int i = 0; i < uiController.bonusPrefabs.Count; i++)
  //   {
  //     bool isMatch = false;

  //     foreach (var reward in offers)
  //     {
  //       if (reward.position == i)
  //       {
  //         isMatch = true;
  //         if (!reward.isLocked)
  //         {
  //           if (reward.type == "extraoffer") uiController.bonusPrefabs[i].ShowResult(6);
  //           else uiController.bonusPrefabs[i].ShowResult(reward.multiplierIndex);
  //         }
  //         // uiController.bonusPrefabs[i].Highlight.SetActive(isMatch);
  //         break;
  //       }
  //     }

  //     if (!isMatch)
  //     {
  //       uiController.bonusPrefabs[i].ShowResult(7);

  //     }
  //     uiController.bonusPrefabs[i].Highlight.SetActive(isMatch);

  //   }
  //   if (count == 5)
  //   {
  //     StartCoroutine(LastBonus());
  //   }
  // }
  internal void switchtoBonusGame(bool isBonus = false)
  {
    if (!isBonus)
    {
      if (audioController) audioController.PlayWLAudio("bonusStart");
      uiController.MoveToBonus();
      uiController.BonusCenterText.text = "SIMPLE DOLLAR";
      count = 0;
      trys = 5;
    }

    uiController.setallBonusfalse(); // reset all highlights first
    count++;
    string ps = count + " OF 5 OFFER";
    StretchText(ps);

    var bonusSettings = socketManager.FeaturesData.bonusSettings;
    List<Reward> offers = !isBonus
        ? socketManager.ResultData.payload.bonusData.rewards
        : socketManager.BonusData.payload.bonusData.rewards;

    StartCoroutine(RevealBonusPrefabs(offers));
  }

  private IEnumerator RevealBonusPrefabs(List<Reward> offers, float delayBetween = 0.2f)
  {
    for (int i = 0; i < uiController.bonusPrefabs.Count; i++)
    {
      //  if (audioController) audioController.PlayWLAudio("card");
      bool isMatch = false;
      foreach (var reward in offers)
      {
        if (reward.position == i)
        {
          isMatch = true;
          if (!reward.isLocked)
          {
            if (reward.type == "extraoffer") uiController.bonusPrefabs[i].ShowResult(6);
            else uiController.bonusPrefabs[i].ShowResult(reward.multiplierIndex);
            if (audioController) audioController.PlayWLAudio("yellowbonus");
          }
          break;
        }
      }
      if (!isMatch)
      {
        uiController.bonusPrefabs[i].ShowResult(7);
      }
      uiController.bonusPrefabs[i].Highlight.SetActive(isMatch);

      yield return new WaitForSeconds(delayBetween);
    }

    if (count == 5)
    {
      StartCoroutine(LastBonus());
    }
    else
    {
      uiController.BonusButtonToggle(true);
    }
  }
  internal void StretchText(string value)
  {
    uiController.BonusCenterText.text = value;

    //  textStretchTween?.Kill();

    RectTransform rect = uiController.BonusCenterText.rectTransform;

    rect.localScale = Vector3.one;

    DOTween.Sequence()
        .Append(
            rect.DOScale(new Vector3(1.3f, 0.8f, 1f), 0.15f)
                .SetEase(Ease.OutExpo)
        )
        .Append(
            rect.DOScale(Vector3.one, 0.15f)
                .SetEase(Ease.OutBounce)
        );
  }
  internal void Bonusover()
  {
    StartCoroutine(BonusEnd());
  }

  IEnumerator BonusEnd()
  {
    Debug.Log("Bonus Winnings-----" + socketManager.BonusData.payload.winAmount);
    yield return uiController.UpdateWinnings(socketManager.PlayerData.balance, socketManager.BonusData.payload.winAmount, 3);
    isBonusdone = true;
    double totalwin = socketManager.ResultData.payload.winAmount + socketManager.BonusData.payload.winAmount;
    uiController.WinMain_Text.text = totalwin.ToString();
    uiController.MoveToSlot();
    uiController.BonusCenterText.text = "SIMPLE DOLLAR II";
  }
  IEnumerator LastBonus()
  {
    uiController.BonusButtonToggle(false);
    yield return new WaitForSeconds(2f);
    socketManager.AccumulateBonus("ACCEPT");
    uiController.ScaleEffect();
  }
  #endregion
}
