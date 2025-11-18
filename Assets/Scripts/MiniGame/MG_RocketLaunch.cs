using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ミニゲーム「ロケット発射！」
/// 長押しでゲージをためるチキンレース。
/// ・1.0 に近いほど高スコア（LowerScoreIsBetter = true）
/// ・1.0 を超えたら「ばくはつ！」で最下位相当
/// ・全員が爆発するか、タイムリミットに達したらミニゲーム終了
/// ※キーは何度でも押したり離したりしてOK
/// </summary>
public class MG_RocketLaunch : MGSceneBase
{
    // 「誤差が小さいほど良い」ので true
    protected override bool LowerScoreIsBetter => true;

    [Header("Rocket Rows")]
    [SerializeField] private Transform rowsRoot;

    [Serializable]
    public class RocketWidgets
    {
        public GameObject root;            // RocketRow_x 全体
        public Image rocketImage;          // ロケット本体
        public RectTransform fuelRect;     // ゲージ用 RectTransform（アンカーは下固定）
        public TextMeshProUGUI nameLabel;  // プレイヤー名
        public TextMeshProUGUI statusLabel;// 「OK」「ばくはつ！」など
        public GameObject explosion;       // 爆発オブジェクト（最初は非表示）
    }
    [SerializeField] private RocketWidgets[] rocketSlots = new RocketWidgets[4];

    [Header("Game Rule")]
    [SerializeField] private float chargeSpeed = 0.8f; // 1秒でどれくらい溜まるか
    [SerializeField] private float maxDuration = 10f;  // タイムリミット（秒）

    private class Runner
    {
        public string name;
        public KeyCode key;
        public Color color;
        public RocketWidgets rocket;

        public bool decided;      // 「結果はもう確定したか」（爆発のみで使用）
        public bool overflow;     // 上限オーバーで爆発したか
        public float level01;     // 0〜1.1くらいまで（1.0が理想）
        public float baseHeight;  // ゲージの最大高さ
    }

    private readonly List<Runner> runners = new();

    protected override IEnumerator PlayRound(Action<List<(string name, int rawScore)>> onFinish)
    {
        runners.Clear();

        var joined = GameManager.Instance.GetJoinedPlayers();
        int n = Mathf.Min(joined.Count, rocketSlots.Length);

        // === 初期化 ===
        for (int i = 0; i < rocketSlots.Length; i++)
        {
            var rw = rocketSlots[i];
            if (rw == null || rw.root == null) continue;

            bool active = i < n;
            rw.root.SetActive(active);
            if (!active) continue;

            var pd = joined[i];

            // 名前
            if (rw.nameLabel) rw.nameLabel.text = pd.playerName;
            if (rw.statusLabel) rw.statusLabel.text = "";

            // ロケット色
            if (rw.rocketImage)
            {
                var c = pd.playerColor;
                c.a = 1f;
                rw.rocketImage.color = c;
            }

            // ゲージ色 & 初期高さ 0
            float baseH = 100f;
            if (rw.fuelRect)
            {
                baseH = rw.fuelRect.sizeDelta.y;

                var size = rw.fuelRect.sizeDelta;
                size.y = 0f;
                rw.fuelRect.sizeDelta = size;

                var img = rw.fuelRect.GetComponent<Image>();
                if (img)
                {
                    var col = pd.playerColor;
                    col.a = 0.9f;
                    img.color = col;
                }
            }

            // 爆発は最初は非表示
            if (rw.explosion)
                rw.explosion.SetActive(false);

            runners.Add(new Runner
            {
                name = pd.playerName,
                key = pd.key,
                color = pd.playerColor,
                rocket = rw,
                decided = false,
                overflow = false,
                level01 = 0f,
                baseHeight = baseH
            });
        }

        if (runners.Count == 0)
        {
            onFinish?.Invoke(new List<(string name, int rawScore)>());
            yield break;
        }

        // === メインループ ===
        float elapsed = 0f;
        int finished = 0;

        while (elapsed < maxDuration && finished < runners.Count)
        {
            elapsed += Time.deltaTime;

            foreach (var r in runners)
            {
                if (r.decided) continue;

                // 押している間だけチャージ（何度でも再開できる）
                if (Input.GetKey(r.key))
                {
                    r.level01 += chargeSpeed * Time.deltaTime;
                }

                // 上限を超えたら爆発 → 即確定
                if (!r.overflow && r.level01 >= 1.0f)
                {
                    r.level01 = Mathf.Min(r.level01, 1.1f);
                    r.overflow = true;
                    r.decided = true;
                    finished++;

                    if (r.rocket.statusLabel)
                        r.rocket.statusLabel.text = "ばくはつ！";

                    // ロケットとゲージを隠して爆発表示
                    if (r.rocket.explosion)
                        r.rocket.explosion.SetActive(true);
                }

                // ※ここでは KeyUp では何もしない
                // 何度も押し直せるようにするため、確定は「爆発」か「時間切れ」のみ

                // ゲージの見た目を更新
                float clamped = Mathf.Clamp01(r.level01);
                if (r.rocket.fuelRect)
                {
                    var size = r.rocket.fuelRect.sizeDelta;
                    size.y = r.baseHeight * clamped;
                    r.rocket.fuelRect.sizeDelta = size;
                }
            }

            // 全員爆発したら早めに終了
            if (finished >= runners.Count)
                break;

            yield return null;
        }

        // タイムリミットに到達したら、残りの人はその時点のゲージで勝負
        if (elapsed >= maxDuration)
        {
            foreach (var r in runners)
            {
                if (r.decided) continue;
                r.decided = true;
                if (r.rocket.statusLabel)
                    r.rocket.statusLabel.text = "OK！"; // 時間までに爆発しなかった組
            }
        }

        // === スコア計算 ===
        var results = new List<(string name, int rawScore)>();
        foreach (var r in runners)
        {
            int raw;
            if (r.overflow)
            {
                // 爆発は最下位になるように大きい値
                raw = 999999;
            }
            else
            {
                // 1.0 に近いほど良い → 差を ms 相当に変換
                float diff = Mathf.Abs(1.0f - Mathf.Clamp01(r.level01)); // 0〜1
                raw = Mathf.RoundToInt(diff * 1000f);
            }
            results.Add((r.name, raw));
        }

        onFinish?.Invoke(results);
    }

    protected override string FormatRawScore(int raw)
    {
        if (raw >= 999999)
            return "ばくはつ！";

        float secDiff = raw / 1000f;   // 例: 30 → 0.03

        // ★ 1 - 差 を計算
        float perscore = 1f - secDiff;
        float score = perscore * 100f; // 例: 0.97 → 97.0

        // マイナスになることもあるので 0.00 でクランプ (任意)
        score = Mathf.Max(score, 0f);

        return $"{score}%";
    }

}
