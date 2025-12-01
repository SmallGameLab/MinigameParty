using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MG_CupWater : MGSceneBase
{
    // 「差が小さい方が良い」ゲームなので true
    protected override bool LowerScoreIsBetter => true;

    [Header("Cups")]
    [SerializeField] private Transform cupsRoot;

    [Serializable]
    private class CupWidgets
    {
        public GameObject root;              // Cup_x 全体

        // ★追加：コップの枠（CupFrame の Image）
        public Image cupFrame;

        public RectTransform waterRect;      // 水の矩形（WaterFill）
        public TextMeshProUGUI nameLabel;    // プレイヤー名
        public TextMeshProUGUI statusLabel;  // 「セーフ」「こぼれた！」など
    }

    [SerializeField] private CupWidgets[] cupSlots = new CupWidgets[4];

    [Header("Game Rule")]
    [SerializeField] private float fillSpeed = 0.35f; // どれくらいの速さで溜まるか
    [SerializeField] private float maxDuration = 8f;  // 制限時間（秒）

    private class Runner
    {
        public string name;
        public KeyCode key;
        public Color color;
        public CupWidgets cup;

        public float level01;    // 0.0〜1.0（オーバー時は1.0ちょい超えも保持）
        public bool overflow;
        public float baseHeight; // 水矩形の「満タン」高さ
    }

    private readonly List<Runner> runners = new();

    protected override IEnumerator PlayRound(Action<List<(string name, int rawScore)>> onFinish)
    {
        runners.Clear();

        var joined = GameManager.Instance.GetJoinedPlayers();
        int n = Mathf.Min(joined.Count, cupSlots.Length);

        // === 初期化 ===
        for (int i = 0; i < cupSlots.Length; i++)
        {
            var c = cupSlots[i];
            if (c == null || c.root == null) continue;

            bool active = (i < n);
            c.root.SetActive(active);
            if (!active) continue;

            var pd = joined[i];

            // 名前
            if (c.nameLabel) c.nameLabel.text = pd.playerName;
            if (c.statusLabel) c.statusLabel.text = "";

            float baseHeight = 100f;

            // 水の見た目
            if (c.waterRect)
            {
                baseHeight = c.waterRect.sizeDelta.y;

                // 最初は水0
                var size = c.waterRect.sizeDelta;
                size.y = 0f;
                c.waterRect.sizeDelta = size;

                // 水の色（プレイヤー色ベース）
                var img = c.waterRect.GetComponent<Image>();
                if (img)
                {
                    var col = pd.playerColor;
                    col.a = 0.85f;
                    img.color = col;
                }
            }

            // コップ枠はとりあえず元の色のまま
            // （Inspector で設定した色をそのまま使う）

            runners.Add(new Runner
            {
                name = pd.playerName,
                key = pd.key,
                color = pd.playerColor,
                cup = c,
                level01 = 0f,
                overflow = false,
                baseHeight = baseHeight
            });
        }

        // === 本編：長押しで水を注ぐ ===
        float elapsed = 0f;
        while (elapsed < maxDuration)
        {
            elapsed += Time.deltaTime;

            foreach (var r in runners)
            {
                if (r.overflow) continue; // こぼれた人はそれ以上増えない

                // キーを押している間だけ水を増やす（何度でも押し直し可）
                if (Input.GetKey(r.key))
                {
                    r.level01 += fillSpeed * Time.deltaTime;

                    // フチを超えたら「こぼれた！」判定
                    if (r.level01 >= 1.0f)
                    {
                        r.level01 = Mathf.Min(r.level01, 1.1f);
                        if (!r.overflow)
                        {
                            r.overflow = true;

                            // テキスト
                            if (r.cup.statusLabel)
                                r.cup.statusLabel.text = "こぼれた！";

                            // ★ここで赤くする
                            Color missRed = new Color(1f, 0.25f, 0.25f, 1f);

                            // コップ枠
                            if (r.cup.cupFrame)
                                r.cup.cupFrame.color = missRed;

                            // 水
                            if (r.cup.waterRect)
                            {
                                var wImg = r.cup.waterRect.GetComponent<Image>();
                                if (wImg) wImg.color = missRed;
                            }
                        }
                    }
                }

                // 見た目更新（水の高さ）
                if (r.cup.waterRect)
                {
                    float t = Mathf.Clamp01(r.level01); // 見た目は 0〜1 にクランプ
                    var size = r.cup.waterRect.sizeDelta;
                    size.y = r.baseHeight * t;
                    r.cup.waterRect.sizeDelta = size;
                }
            }

            yield return null;
        }

        // === 結果集計 ===
        var results = new List<(string name, int rawScore)>();

        foreach (var r in runners)
        {
            int raw;
            if (r.overflow)
            {
                // こぼれた人は絶対最下位扱いにしたいので超デカい値
                raw = 999999;
            }
            else
            {
                float diff = Mathf.Abs(1.0f - r.level01);  // 0 に近いほど良い
                raw = Mathf.RoundToInt(diff * 1000f);      // ml イメージ
                if (r.cup.statusLabel) r.cup.statusLabel.text = "セーフ！";
            }
            results.Add((r.name, raw));
        }

        onFinish?.Invoke(results);
    }

    // 結果画面での表示形式
    protected override string FormatRawScore(int raw)
    {
        if (raw >= 999000)
            return "こぼれた！";

        // ★ 1 - 差 を計算
        float perscore = 1 - raw / 1000f;
        float score = perscore * 1000f; // 例: 0.97 → 970.0

        // マイナスになることもあるので 0.00 でクランプ (任意)
        score = Mathf.Max(score, 0f);

        return $"{score:0}ml";
    }
}
