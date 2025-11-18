using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ResultUIManager : MonoBehaviour
{
    [Header("Texts")]
    public TextMeshProUGUI playerName;
    public TextMeshProUGUI compliment;
    public TextMeshProUGUI animalType;

    [Header("Radar Chart")]
    public RadarChartUI radarChart;   // ★ 追加

    int currentIndex = 0;
    List<PlayerData> joined;

    void Start()
    {
        joined = GameManager.Instance.GetJoinedPlayers();
        if (joined.Count == 0)
        {
            GameManager.Instance.LoadLobbyScene();
            return;
        }
        ShowCurrent();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0) ||
            Input.GetKeyDown(KeyCode.Return) ||
            Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            currentIndex++;
            if (currentIndex >= joined.Count)
            {
                GameManager.Instance.LoadLobbyScene();
            }
            else
            {
                ShowCurrent();
            }
        }
    }

    void ShowCurrent()
    {
        var p = joined[currentIndex];

        if (playerName) playerName.text = p.playerName;
        if (compliment) compliment.text = p.complimentMessage;
        if (animalType) animalType.text = p.determinedAnimalType;

        // 各ジャンル 0〜20pt を 0〜1 に正規化
        float reflex01 = Mathf.Clamp01(p.genrePoints["reflex"] / 20f);
        float mash01 = Mathf.Clamp01(p.genrePoints["mash"] / 20f);
        float hold01 = Mathf.Clamp01(p.genrePoints["hold"] / 20f);

        if (radarChart)
        {
            radarChart.SetValues(reflex01, mash01, hold01);
        }
    }
}
