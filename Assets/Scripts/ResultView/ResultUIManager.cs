using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

[System.Serializable]
public struct AnimalIconEntry
{
    public string animalTypeName; // 例: "ライオン", "うさぎ", "ひよこ"
    public Sprite sprite;         // 対応するイラスト
}

public class ResultUIManager : MonoBehaviour
{
    [Header("Texts")]
    public TextMeshProUGUI playerName;
    public TextMeshProUGUI compliment;
    public TextMeshProUGUI animalType;

    [Header("Radar Chart")]
    public ResultRadarChart radarChart;      // 背景三角形（今まで通り）
    [SerializeField] private ResultSubRadarChart radarFill;  // 塗りつぶし部分

    [Header("Animal Icon & Background")]
    [SerializeField] private Image animalIconImage;          // 動物イラスト用 Image
    [SerializeField] private Image backgroundPanel;          // 背景色を変えるパネル

    [SerializeField]
    private List<AnimalIconEntry> animalIcons = new List<AnimalIconEntry>();

    int currentIndex = 0;
    List<PlayerData> joined;

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

        // ===== テキスト類 =====
        if (playerName) playerName.text = p.playerName;
        if (compliment) compliment.text = p.complimentMessage;
        if (animalType) animalType.text = p.determinedAnimalType;

        // ===== 背景色をプレイヤーカラーに合わせる =====
        if (backgroundPanel)
        {
            Color c = p.playerColor;

            // ★ 青プレイヤーだけ少し明るく・薄くする
            // プレイヤー設定で Color.blue を使っている前提ならこれでOK
            if (p.playerColor == Color.blue)
            {
                // R,G,B を少し明るめにして、アルファも少し薄めに
                c = new Color(0.45f, 0.65f, 1.0f, 0.35f);
            }
            else
            {
                // 他の色（赤・緑・黄など）は今まで通り少し薄くするだけ
                c.a = 0.3f;
            }

            backgroundPanel.color = c;
        }

        // ===== 動物アイコンの差し替え =====
        if (animalIconImage)
        {
            Sprite s = FindAnimalSprite(p.determinedAnimalType);
            if (s != null)
            {
                animalIconImage.sprite = s;
                animalIconImage.enabled = true;
            }
            else
            {
                animalIconImage.enabled = false;
            }
        }

        // ===== レーダーチャート(塗りつぶし)の更新 =====
        if (radarFill != null)
        {
            // 0〜20pt を 0〜1 に正規化（安全のため Clamp）
            float reflex01 = Mathf.Clamp01(p.genrePoints["reflex"] / 20f);
            float mash01 = Mathf.Clamp01(p.genrePoints["mash"] / 20f);
            float hold01 = Mathf.Clamp01(p.genrePoints["hold"] / 20f);

            radarFill.SetValues(reflex01, mash01, hold01, p.playerColor);
            radarFill.SetVerticesDirty();
        }
    }

    /// <summary>
    /// 動物タイプ文字列から対応する Sprite を探す
    /// </summary>
    Sprite FindAnimalSprite(string typeName)
    {
        if (string.IsNullOrEmpty(typeName)) return null;
        foreach (var entry in animalIcons)
        {
            if (entry.animalTypeName == typeName)
            {
                return entry.sprite;
            }
        }
        return null;
    }
}
