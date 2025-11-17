using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MG_Balloon : MGSceneBase
{
    // 連打ゲーム → 回数が多いほど良い
    protected override bool LowerScoreIsBetter => false;

    [Header("Tracks")]
    [SerializeField] private Transform tracksRoot;

    [Serializable]
    private class TrackWidgets
    {
        public GameObject root;           // Track_x（レーン全体）
        public Image balloon;             // 風船のImage（Sceneで配置済み）
        public TextMeshProUGUI nameLabel; // プレイヤー名
    }
    [SerializeField] private TrackWidgets[] trackSlots = new TrackWidgets[4];

    [Header("Balloon Settings")]
    [Tooltip("連打の計測時間（秒）")]
    [SerializeField] private float durationSeconds = 10f;

    [Tooltip("この回数で最大サイズになる目安（UIだけの目安値）")]
    [SerializeField] private int maxMashCountForFullSize = 60;

    [Tooltip("最小サイズの倍率（ベーススケールに対して）")]
    [SerializeField] private float minScaleMul = 0.4f;

    [Tooltip("最大サイズの倍率（ベーススケールに対して）")]
    [SerializeField] private float maxScaleMul = 1.3f;

    // ランタイムデータ
    private class Runner
    {
        public string name;
        public KeyCode key;
        public Color color;
        public Image balloon;
        public Vector3 baseScale;   // シーンで配置したときのスケールを覚える
        public int mashCount;
    }

    private readonly List<Runner> runners = new();

    protected override IEnumerator PlayRound(Action<List<(string name, int rawScore)>> onFinish)
    {
        runners.Clear();

        // 参加プレイヤー取得
        var joined = GameManager.Instance.GetJoinedPlayers();
        int n = Mathf.Min(joined.Count, trackSlots.Length);

        // === トラック初期化 ===
        for (int i = 0; i < trackSlots.Length; i++)
        {
            var slot = trackSlots[i];
            if (slot == null || slot.root == null) continue;

            bool active = (i < n);
            slot.root.SetActive(active);     // 参加していないレーンは消す
            if (!active) continue;

            var pd = joined[i];

            // 名前
            if (slot.nameLabel) slot.nameLabel.text = pd.playerName;

            if (slot.balloon == null)
            {
                Debug.LogWarning($"[MG_Balloon] Track {i} に風船Imageが設定されていません");
                continue;
            }

            // ベーススケールを記録（Sceneで見栄えを調整した値）
            var rt = slot.balloon.rectTransform;
            var baseScale = rt.localScale;
            if (baseScale == Vector3.zero)
            {
                // 万一0だったらデフォルト1を入れておく
                baseScale = Vector3.one;
                rt.localScale = baseScale;
            }

            // 風船の色（プレイヤー色 / アルファ100%）
            var c = pd.playerColor;
            c.a = 1f;
            slot.balloon.color = c;

            // いきなり消したり0スケールにしたりしない（ここがポイント）
            // → Sceneで見えている状態をそのまま使う

            runners.Add(new Runner
            {
                name = pd.playerName,
                key = pd.key,
                color = pd.playerColor,
                balloon = slot.balloon,
                baseScale = baseScale,
                mashCount = 0
            });
        }

        // 参加者がいない場合の保険
        if (runners.Count == 0)
        {
            Debug.LogWarning("[MG_Balloon] runners が 0 です");
            onFinish?.Invoke(new List<(string, int)>());
            yield break;
        }

        // === 入力ループ ===
        float t = 0f;
        while (t < durationSeconds)
        {
            t += Time.deltaTime;

            // キー入力（1回押下で1カウント）
            foreach (var r in runners)
            {
                if (Input.GetKeyDown(r.key))
                {
                    r.mashCount++;
                }

                // 連打回数に応じて風船サイズを更新
                float ratio = Mathf.Clamp01(r.mashCount / (float)maxMashCountForFullSize);
                float scaleMul = Mathf.Lerp(minScaleMul, maxScaleMul, ratio);

                var rt = r.balloon.rectTransform;
                rt.localScale = r.baseScale * scaleMul;
            }

            yield return null;
        }

        // === 結果を返す（rawScore = 連打回数） ===
        var results = runners.Select(r => (r.name, r.mashCount)).ToList();
        onFinish?.Invoke(results);
    }

    // 結果画面用の表示（例: 「32回」）
    protected override string FormatRawScore(int mashCount)
    {
        return $"{mashCount}回";
    }
}
