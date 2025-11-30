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
    public ResultRadarChart radarChart;              // 背景用（五角形など）。今は触らなくてもOK
    [SerializeField] private ResultSubRadarChart radarFill; // 中身（プレイヤーの値で塗る方）

    private int currentIndex = 0;
    private List<PlayerData> joined;

    void Start()
    {
        joined = GameManager.Instance.GetJoinedPlayers();
        if (joined == null || joined.Count == 0)
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
                // 全員分の結果を見終わったらロビーへ
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

        // テキスト類
        if (playerName) playerName.text = p.playerName;
        if (compliment) compliment.text = p.complimentMessage;
        if (animalType) animalType.text = p.determinedAnimalType;

        // 0〜20pt を 0〜1 に正規化（安全のため Clamp）
        float reflex01 = Mathf.Clamp01(p.genrePoints["reflex"] / 20f);
        float mash01 = Mathf.Clamp01(p.genrePoints["mash"] / 20f);
        float hold01 = Mathf.Clamp01(p.genrePoints["hold"] / 20f);

        Debug.Log($"[ResultUIManager] {p.playerName} R={reflex01} M={mash01} H={hold01}");

        // レーダーチャート本体（塗りつぶし）に値を反映
        if (radarFill != null)
        {
            radarFill.SetValues(reflex01, mash01, hold01, p.playerColor);
        }
    }

}
