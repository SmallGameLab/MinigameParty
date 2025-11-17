using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MG_RendaMaster : MGSceneBase
{
    // 連打ゲーム → 回数が多いほど良い
    protected override bool LowerScoreIsBetter => false;

    [Header("Gauges")]
    [SerializeField] private Transform gaugesRoot;

    [Serializable]
    public class GaugeWidgets
    {
        public GameObject root;              // レーン全体 (Gauge_1 など)
        public RectTransform barRect;        // 実際に伸び縮みさせるバーの RectTransform
        public TextMeshProUGUI nameLabel;    // プレイヤー名
        public TextMeshProUGUI countLabel;   // カウント表示（○回）
    }
    [SerializeField] private GaugeWidgets[] gaugeSlots = new GaugeWidgets[4];

    [Header("Game Rule")]
    [SerializeField] private int goalPressCount = 60;   // ゴールになる連打回数
    [SerializeField] private float maxDuration = 15f;   // 念のための制限時間（誰も押さない事故対策）

    private class Runner
    {
        public string name;
        public KeyCode key;
        public Color color;
        public GaugeWidgets gauge;

        public int pressCount;
        public float baseWidth;   // ゲージ最大幅（初期値）
    }
    private readonly List<Runner> runners = new();

    protected override IEnumerator PlayRound(Action<List<(string name, int rawScore)>> onFinish)
    {
        runners.Clear();

        var joined = GameManager.Instance.GetJoinedPlayers();
        int n = Mathf.Min(joined.Count, gaugeSlots.Length);

        // ==== 初期化 ====
        for (int i = 0; i < gaugeSlots.Length; i++)
        {
            var g = gaugeSlots[i];
            if (g == null || g.root == null) continue;

            bool active = (i < n);
            g.root.SetActive(active);
            if (!active) continue;

            var pd = joined[i];

            // 名前
            if (g.nameLabel) g.nameLabel.text = pd.playerName;

            // バーの色（Image が付いている前提）
            if (g.barRect)
            {
                var img = g.barRect.GetComponent<Image>();
                if (img)
                {
                    var c = pd.playerColor;
                    c.a = 1f;
                    img.color = c;
                }
            }

            // ベース幅を保存（この幅を 100% としてスケールする）
            float baseWidth = g.barRect ? g.barRect.sizeDelta.x : 100f;

            runners.Add(new Runner
            {
                name = pd.playerName,
                key = pd.key,
                color = pd.playerColor,
                gauge = g,
                pressCount = 0,
                baseWidth = baseWidth
            });

            // 最初は幅 0 にしておく（ゲージ空）
            if (g.barRect)
            {
                var size = g.barRect.sizeDelta;
                size.x = 0f;
                g.barRect.sizeDelta = size;
            }

            if (g.countLabel) g.countLabel.text = "0回";
        }

        // ==== 本編：連打レース ====

        float elapsed = 0f;
        bool someoneReachedGoal = false;

        while (elapsed < maxDuration && !someoneReachedGoal)
        {
            elapsed += Time.deltaTime;

            foreach (var r in runners)
            {
                if (Input.GetKeyDown(r.key))
                {
                    r.pressCount++;

                    // テキスト更新
                    if (r.gauge.countLabel)
                        r.gauge.countLabel.text = $"{r.pressCount}回";

                    // 進捗 0〜1
                    float t = Mathf.Clamp01(r.pressCount / (float)goalPressCount);

                    // ゲージ幅を伸ばす
                    if (r.gauge.barRect)
                    {
                        var size = r.gauge.barRect.sizeDelta;
                        size.x = r.baseWidth * t;
                        r.gauge.barRect.sizeDelta = size;
                    }

                    // 誰かがゴールに到達したらレース終了フラグON
                    if (r.pressCount >= goalPressCount)
                    {
                        someoneReachedGoal = true;
                    }
                }
            }

            yield return null;
        }

        // ==== 結果集計 ====
        // 誰かが 60回に達したタイミング（or 制限時間切れ）の押した回数で勝負
        var results = runners.Select(r => (r.name, r.pressCount)).ToList();
        onFinish?.Invoke(results);
    }

    // 結果画面での表示形式（「○○回」）
    protected override string FormatRawScore(int raw)
    {
        return $"{raw}回";
    }
}
