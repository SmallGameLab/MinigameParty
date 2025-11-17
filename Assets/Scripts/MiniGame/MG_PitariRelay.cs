using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MG_PitariRelay : MGSceneBase
{
    // 小さい方が良い（ターゲットからのズレ時間）
    protected override bool LowerScoreIsBetter => true;

    [Header("Tracks")]
    [SerializeField] private Transform tracksRoot;

    [Serializable]
    private class TrackWidgets
    {
        public GameObject root;                 // Track_x（レーン全体の親）
        public RectTransform bar;               // 動くバー（Scene 上で配置済み）
        public RectTransform target;            // 目標位置（固定 / Scene 上で配置済み）
        public TextMeshProUGUI nameLabel;       // プレイヤー名表示（任意）
    }
    [SerializeField] private TrackWidgets[] trackSlots = new TrackWidgets[4];

    [Header("Movement")]
    [SerializeField] private float barSpeed = 700f;          // px/sec（UIスケールに応じて調整）
    public enum MoveMode { PingPong, Loop }
    [SerializeField] private MoveMode movement = MoveMode.PingPong;

    // ※ margin は「シーン上で置いた Bar/Target の位置」からどれだけ内側に縮めるか
    [SerializeField] private float marginTop = 0f;
    [SerializeField] private float marginBottom = 0f;

    // ランタイム
    private class Runner
    {
        public string name;
        public KeyCode key;
        public Color color;
        public RectTransform bar;
        public RectTransform target;
        public bool decided;
        public int rawMs;
        public float dir = +1f;
        public float minY, maxY;
    }
    private readonly List<Runner> runners = new();

    protected override IEnumerator PlayRound(Action<List<(string name, int rawScore)>> onFinish)
    {
        runners.Clear();

        // 参加プレイヤーを取得
        var joined = GameManager.Instance.GetJoinedPlayers();
        int n = Mathf.Min(joined.Count, trackSlots.Length);

        // ==== トラック初期化 & 割り当て ====
        for (int i = 0; i < trackSlots.Length; i++)
        {
            var t = trackSlots[i];
            if (t == null || t.root == null) continue;

            bool active = (i < n);
            t.root.SetActive(active);      // 参加していない人のレーンは非表示
            if (!active) continue;

            var pd = joined[i];

            // プレイヤー名
            if (t.nameLabel) t.nameLabel.text = pd.playerName;

            // ★バーの色をプレイヤー色にする
            if (t.bar)
            {
                var img = t.bar.GetComponent<Image>();
                if (img)
                {
                    var c = pd.playerColor;
                    c.a = 1f;              // しっかり見えるように 100% に
                    img.color = c;
                }
            }

            if (!t.bar || !t.target)
            {
                Debug.LogWarning($"[MG_PitariRelay] Track {i} の bar または target が未設定です。");
                continue;
            }

            // ★★ ここが一番重要 ★★
            // シーン上で配置した Bar/Target の Y 位置をそのまま可動範囲に使う
            float barY = t.bar.anchoredPosition.y;
            float targetY = t.target.anchoredPosition.y;

            // 下端と上端を決める（どちらが上でもOKにする）
            float minY = Mathf.Min(barY, targetY) + marginBottom;
            float maxY = Mathf.Max(barY, targetY) - marginTop;

            // 初期位置：下端にそろえる
            // （すでに下に置いてあるなら結果的に同じ位置なので見た目は変わりません）
            t.bar.anchoredPosition = new Vector2(t.bar.anchoredPosition.x, minY);

            runners.Add(new Runner
            {
                name = pd.playerName,
                key = pd.key,
                color = pd.playerColor,
                bar = t.bar,
                target = t.target,
                decided = false,
                rawMs = 999999,
                dir = +1f,
                minY = minY,
                maxY = maxY
            });
        }

        // ==== 入力 & 移動ループ ====
        int finished = 0;
        while (finished < runners.Count)
        {
            float dt = Time.deltaTime;

            foreach (var r in runners)
            {
                if (r.decided) continue;

                // バーを移動
                float y = r.bar.anchoredPosition.y + r.dir * barSpeed * dt;

                if (movement == MoveMode.PingPong)
                {
                    if (y > r.maxY) { y = r.maxY; r.dir = -1f; }
                    if (y < r.minY) { y = r.minY; r.dir = +1f; }
                }
                else // Loop
                {
                    float range = r.maxY - r.minY;
                    if (range <= 0.01f) range = 1f; // 万一範囲が0だったときの保険
                    if (y > r.maxY) y = r.minY + (y - r.maxY) % range;
                    if (y < r.minY) y = r.maxY - (r.minY - y) % range;
                }

                r.bar.anchoredPosition = new Vector2(r.bar.anchoredPosition.x, y);

                // キー入力
                if (Input.GetKeyDown(r.key))
                {
                    // ターゲットとの距離 → 時間差(ms) 相当のスコアに変換
                    float dy = Mathf.Abs(y - r.target.anchoredPosition.y);
                    int ms = Mathf.RoundToInt(dy / Mathf.Max(1f, barSpeed) * 1000f);

                    r.rawMs = Mathf.Clamp(ms, 0, 999999);
                    r.decided = true;
                    finished++;
                }
            }

            yield return null;
        }

        // ==== 結果を GameManager へ返す ====
        var results = runners.Select(r => (r.name, r.rawMs)).ToList();
        onFinish?.Invoke(results);
    }

    // 結果画面での表示形式
    protected override string FormatRawScore(int distanceMm)
    {
        // 例: "ずれ12mm"
        return $"ずれ{distanceMm}mm";
    }
}
