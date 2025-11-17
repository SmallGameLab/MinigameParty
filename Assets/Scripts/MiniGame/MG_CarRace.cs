using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MG_CarRace : MGSceneBase
{
    // 連打が多いほど「距離」が長い → 大きい方が良い
    protected override bool LowerScoreIsBetter => false;

    [Header("Tracks")]
    [SerializeField] private Transform tracksRoot;

    [Serializable]
    private class TrackWidgets
    {
        public GameObject root;                 // Track_x（レーン全体の親）
        public RectTransform car;               // 車（Scene 上で配置済み）
        public RectTransform goal;              // ゴール位置（Scene 上で配置済み）
        public TextMeshProUGUI nameLabel;       // プレイヤー名表示
        public TextMeshProUGUI meterLabel;      // 距離表示「○m」（任意）
    }

    [SerializeField] private TrackWidgets[] trackSlots = new TrackWidgets[4];

    [Header("Race Settings")]
    [SerializeField] private float raceDuration = 10f;     // レース時間（秒）
    [SerializeField] private int pressesForFullDistance = 80; // これくらい連打でゴール到達、という目安

    [Header("Score View")]
    [SerializeField] private float metersPerPress = 1.5f;  // 1回の連打を何メートル相当にするか

    // ランタイム用
    private class Runner
    {
        public string name;
        public KeyCode key;
        public Color color;
        public RectTransform car;
        public RectTransform goal;
        public TextMeshProUGUI meterLabel;

        public float startX;
        public float goalX;
        public int pressCount;
    }

    private readonly List<Runner> runners = new();

    protected override IEnumerator PlayRound(Action<List<(string name, int rawScore)>> onFinish)
    {
        runners.Clear();

        // 参加プレイヤー
        var joined = GameManager.Instance.GetJoinedPlayers();
        int n = Mathf.Min(joined.Count, trackSlots.Length);

        // ==== トラック初期化 & 割り当て ====
        for (int i = 0; i < trackSlots.Length; i++)
        {
            var t = trackSlots[i];
            if (t == null || t.root == null) continue;

            bool active = (i < n);
            t.root.SetActive(active);
            if (!active) continue;

            var pd = joined[i];

            // プレイヤー名
            if (t.nameLabel) t.nameLabel.text = pd.playerName;

            // 車とゴールが無い場合は警告
            if (!t.car || !t.goal)
            {
                Debug.LogWarning($"[MG_CarRace] Track {i} の car または goal が未設定です。");
                continue;
            }

            // 車の色をプレイヤーカラーに
            var img = t.car.GetComponent<Image>();
            if (img)
            {
                var c = pd.playerColor;
                c.a = 1f;
                img.color = c;
            }

            // スタート / ゴール位置（X 座標）を記録
            float startX = t.car.anchoredPosition.x;
            float goalX = t.goal.anchoredPosition.x;

            // メーター初期値
            if (t.meterLabel) t.meterLabel.text = "0m";

            runners.Add(new Runner
            {
                name = pd.playerName,
                key = pd.key,
                color = pd.playerColor,
                car = t.car,
                goal = t.goal,
                meterLabel = t.meterLabel,
                startX = startX,
                goalX = goalX,
                pressCount = 0
            });
        }

        // 参加者がいなければ空結果で終了
        if (runners.Count == 0)
        {
            onFinish?.Invoke(new List<(string name, int rawScore)>());
            yield break;
        }

        // ==== レース本編 ====
        float elapsed = 0f;
        bool raceFinished = false;   // ★ゴールしたら true にするフラグ

        while (!raceFinished && elapsed < raceDuration)
        {
            elapsed += Time.deltaTime;

            foreach (var r in runners)
            {
                // 連打チェック
                if (Input.GetKeyDown(r.key))
                {
                    r.pressCount++;

                    // どれくらいゴールに近いか（0〜1）
                    float tNorm = pressesForFullDistance > 0
                        ? Mathf.Clamp01(r.pressCount / (float)pressesForFullDistance)
                        : 0f;

                    // 車の位置を更新
                    float x = Mathf.Lerp(r.startX, r.goalX, tNorm);
                    var pos = r.car.anchoredPosition;
                    pos.x = x;
                    r.car.anchoredPosition = pos;

                    // 距離表示更新
                    if (r.meterLabel)
                    {
                        r.meterLabel.text = FormatDistance(r.pressCount);
                    }

                    // ★ここがポイント：ゴールに到達したかチェック
                    //   tNorm が 1 に達したら「ゴールした」とみなして即レース終了
                    if (tNorm >= 1f)
                    {
                        raceFinished = true;
                        // ここで break してもいいが、同フレーム内で他プレイヤーも
                        // ゴールする可能性を残したいなら break しない手もある
                        // 今回は「誰かがゴールした瞬間に終了」でOKなので break で抜ける
                        break;
                    }
                }
            }

            yield return null;
        }

        // （お好みで：ゴールフラグ後に、全員分の車の位置を最終値に整えるなども可能）


        // ==== 結果集計 ====
        var results = runners
            .Select(r => (r.name, rawScore: r.pressCount))
            .ToList();

        onFinish?.Invoke(results);
    }

    // 「連打回数 → 距離テキスト」
    private string FormatDistance(int pressCount)
    {
        float meters = pressCount * metersPerPress;
        return $"{meters:0}m";
    }

    // 結果画面での表示形式（GameManager から来る rawScore = pressCount）
    protected override string FormatRawScore(int rawPresses)
    {
        float meters = rawPresses * metersPerPress;
        return $"{meters:0}m";
    }
}
