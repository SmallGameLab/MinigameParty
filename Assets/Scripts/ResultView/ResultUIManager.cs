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
    public ResultRadarChart radarChart;  // レーダーチャートへの参照

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

        // レーダーチャートを更新
        int r = p.genrePoints["reflex"];
        int m = p.genrePoints["mash"];
        int h = p.genrePoints["hold"];

        if (radarChart)
        {
            radarChart.SetValues(r, m, h);
        }
    }
}
