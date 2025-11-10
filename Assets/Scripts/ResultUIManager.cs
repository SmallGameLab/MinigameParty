// Assets/Scripts/ResultUIManager.cs
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ResultUIManager : MonoBehaviour
{
    [Header("Refs")]
    public TextMeshProUGUI playerName;
    public TextMeshProUGUI compliment;
    public TextMeshProUGUI animalType;
    public Image barReflex, barMash, barHold;

    int currentIndex = 0;
    List<PlayerData> joined;

    void Start()
    {
        joined = GameManager.Instance.GetJoinedPlayers();
        if (joined.Count == 0) { GameManager.Instance.LoadLobbyScene(); return; }
        ShowCurrent();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            currentIndex++;
            if (currentIndex >= joined.Count) { GameManager.Instance.LoadLobbyScene(); }
            else ShowCurrent();
        }
    }

    void ShowCurrent()
    {
        var p = joined[currentIndex];
        playerName.text = p.playerName;
        compliment.text = p.complimentMessage;
        animalType.text = p.determinedAnimalType;

        // 合計ポイント(0〜20) → 横幅(最大=GraphArea幅の 900px 相当) に換算
        float maxW = 900f;
        float wR = Mathf.Clamp01(p.genrePoints["reflex"] / 20f) * maxW;
        float wM = Mathf.Clamp01(p.genrePoints["mash"] / 20f) * maxW;
        float wH = Mathf.Clamp01(p.genrePoints["hold"] / 20f) * maxW;

        SetBarWidth(barReflex.rectTransform, wR);
        SetBarWidth(barMash.rectTransform, wM);
        SetBarWidth(barHold.rectTransform, wH);
    }

    void SetBarWidth(RectTransform rt, float w)
    {
        var size = rt.sizeDelta;
        size.x = w;
        rt.sizeDelta = size;
    }
}
