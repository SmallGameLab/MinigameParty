using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MG_TobikkoMeijin : MGSceneBase
{
    // 目標高度とのズレが小さいほど良い
    protected override bool LowerScoreIsBetter => true;

    [Header("Lanes")]
    [SerializeField] private Transform lanesRoot;

    [Serializable]
    private class LaneWidgets
    {
        public GameObject root;            // Lane_x
        public RectTransform planeRect;    // Plane（飛行機）
        public RectTransform targetAlt;    // TargetAlt（狙いの高さ）
        public RectTransform goal;         // Goal（ゴールX位置）
        public TextMeshProUGUI nameLabel;  // Name（プレイヤー名）
    }
    [SerializeField] private LaneWidgets[] laneSlots = new LaneWidgets[4];

    [Header("Physics-like Params")]
    [SerializeField] private float gravity = 900f;          // 重力加速度（下向き）
    [SerializeField] private float thrustUp = 1400f;        // キー押下中の上向き加速度
    [SerializeField] private float thrustForward = 800f;    // キー押下中の前向き加速度
    [SerializeField] private float maxForwardSpeed = 900f;  // 横速度の上限
    [SerializeField] private float maxRiseSpeed = 900f;     // 上昇速度の上限
    [SerializeField] private float maxFallSpeed = 900f;     // 落下速度の上限

    [SerializeField] private float maxHeightOffset = 350f;  // groundY から上方向どこまでOKか
    [SerializeField] private float marginBottom = 30f;      // groundY からどれだけ下で墜落扱いか
    [SerializeField] private float maxTotalTime = 10f;      // 安全タイムアウト

    private class Runner
    {
        public string name;
        public KeyCode key;
        public Color color;

        public RectTransform plane;
        public RectTransform targetAlt;
        public float groundY;
        public float minY;
        public float maxY;

        public float goalX;

        public bool hasTakenOff;
        public bool finished;
        public bool crashed;

        public float velX;
        public float velY;

        public int rawScore;  // 目標高度からのズレ（px）or 失敗は大きな値
    }

    private readonly List<Runner> runners = new();

    protected override IEnumerator PlayRound(Action<List<(string name, int rawScore)>> onFinish)
    {
        runners.Clear();

        var gm = GameManager.Instance;
        var joined = gm.GetJoinedPlayers();
        int n = Mathf.Min(joined.Count, laneSlots.Length);

        // ===== レーン初期化 =====
        for (int i = 0; i < laneSlots.Length; i++)
        {
            var lane = laneSlots[i];
            if (lane == null || lane.root == null) continue;

            bool active = (i < n);
            lane.root.SetActive(active);
            if (!active) continue;

            var pd = joined[i];

            if (lane.nameLabel) lane.nameLabel.text = pd.playerName;

            if (!lane.planeRect || !lane.targetAlt || !lane.goal)
            {
                Debug.LogWarning($"[MG_TobikkoMeijin] Lane {i}: plane / targetAlt / goal 未設定");
                lane.root.SetActive(false);
                continue;
            }

            // プレイヤーカラーを飛行機に
            var img = lane.planeRect.GetComponent<Image>();
            if (img)
            {
                var c = pd.playerColor;
                c.a = 1f;
                img.color = c;
            }

            // Plane のシーン上の位置を基準にする
            Vector2 p0 = lane.planeRect.anchoredPosition;
            float groundY = p0.y;
            float minY = groundY - marginBottom;
            float maxY = groundY + maxHeightOffset;

            float startX = p0.x;
            float goalX = lane.goal.anchoredPosition.x;
            if (goalX <= startX + 1f)
            {
                goalX = startX + 300f; // 保険
                Debug.LogWarning($"[MG_TobikkoMeijin] Lane {i}: Goal が近すぎるため仮のXを使用しました");
            }

            lane.planeRect.anchoredPosition = p0; // そのまま

            runners.Add(new Runner
            {
                name = pd.playerName,
                key = pd.key,
                color = pd.playerColor,
                plane = lane.planeRect,
                targetAlt = lane.targetAlt,
                groundY = groundY,
                minY = minY,
                maxY = maxY,
                goalX = goalX,
                hasTakenOff = false,
                finished = false,
                crashed = false,
                velX = 0f,
                velY = 0f,
                rawScore = 999999
            });
        }

        if (runners.Count == 0)
        {
            onFinish?.Invoke(new List<(string, int)>());
            yield break;
        }

        // ===== メインループ =====
        float globalTime = 0f;
        int finishedCount = 0;

        while (finishedCount < runners.Count && globalTime < maxTotalTime)
        {
            float dt = Time.deltaTime;
            globalTime += dt;

            foreach (var r in runners)
            {
                if (r.finished) continue;

                var pos = r.plane.anchoredPosition;

                // 離陸前：キー待ち（全く動かない）
                if (!r.hasTakenOff)
                {
                    if (Input.GetKeyDown(r.key))
                    {
                        r.hasTakenOff = true;
                        // 初回は少しだけ前進させると「離陸した感」が出る
                        r.velX = 200f;
                        r.velY = 0f;
                    }

                    r.plane.anchoredPosition = pos;
                    continue;
                }

                // ==== 離陸後：速度＆加速度 ====
                float ax = 0f;
                float ay = -gravity;   // 常に重力は下向き

                if (Input.GetKey(r.key))
                {
                    // 押している間は上向き＆前向きの力を加える
                    ax += thrustForward;
                    ay += thrustUp;
                }

                // 速度更新
                r.velX += ax * dt;
                r.velY += ay * dt;

                // クランプ（暴走防止）
                r.velX = Mathf.Clamp(r.velX, 0f, maxForwardSpeed);
                r.velY = Mathf.Clamp(r.velY, -maxFallSpeed, maxRiseSpeed);

                // 位置更新
                pos.x += r.velX * dt;
                pos.y += r.velY * dt;

                // 墜落／頭打ち判定
                if (pos.y < r.minY)
                {
                    pos.y = r.minY;
                    r.finished = true;
                    r.crashed = true;
                    r.rawScore = 999999;
                    finishedCount++;
                }
                else if (pos.y > r.maxY)
                {
                    pos.y = r.maxY;
                    r.finished = true;
                    r.crashed = true;
                    r.rawScore = 999999;
                    finishedCount++;
                }

                r.plane.anchoredPosition = pos;

                // ゴール到達（成功時）
                if (!r.crashed && pos.x >= r.goalX)
                {
                    r.finished = true;
                    finishedCount++;

                    float dy = Mathf.Abs(pos.y - r.targetAlt.anchoredPosition.y);
                    r.rawScore = Mathf.Clamp(Mathf.RoundToInt(dy), 0, 999999);
                }
            }

            yield return null;
        }

        // タイムアウトした人はその場の高さでスコア確定
        foreach (var r in runners)
        {
            if (!r.finished)
            {
                var pos = r.plane.anchoredPosition;
                float dy = Mathf.Abs(pos.y - r.targetAlt.anchoredPosition.y);
                r.rawScore = Mathf.Clamp(Mathf.RoundToInt(dy), 0, 999999);
                r.finished = true;
            }
        }

        var results = runners.Select(r => (r.name, r.rawScore)).ToList();
        onFinish?.Invoke(results);
    }

    protected override string FormatRawScore(int distance)
    {
        // 単位はあとで調整OK
        return $"ずれ{distance}m";
    }
}
