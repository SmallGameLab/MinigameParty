using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MG_PikaStop : MGSceneBase
{
    // 反応速度ゲーム → 小さい方が良い
    protected override bool LowerScoreIsBetter => true;

    [Header("PikaStop refs")]
    [SerializeField] private GameObject bulbOff;
    [SerializeField] private GameObject bulbOn;

    protected override IEnumerator PlayRound(Action<List<(string name, int rawScore)>> onFinish)
    {
        if (bulbOff) bulbOff.SetActive(true);
        if (bulbOn) bulbOn.SetActive(false);

        var joined = GameManager.Instance.GetJoinedPlayers();
        var keyMap = joined.ToDictionary(p => p.key, p => p.playerName);
        var falseStart = new HashSet<KeyCode>();
        var reacted = new HashSet<KeyCode>();
        var results = new List<(string name, int rawScore)>();

        // ===== ランダム待機（フライング判定区間） =====
        float wait = UnityEngine.Random.Range(0.8f, 2.0f);
        float t0 = Time.time;
        while (Time.time - t0 < wait)
        {
            foreach (var kv in keyMap)
            {
                if (!falseStart.Contains(kv.Key) && Input.GetKeyDown(kv.Key))
                {
                    // フライング登録
                    falseStart.Add(kv.Key);
                }
            }
            yield return null;
        }

        // ===== ピカッ！点灯 =====
        if (bulbOff) bulbOff.SetActive(false);
        if (bulbOn) bulbOn.SetActive(true);

        float start = Time.time;

        // ===== 反応待ち =====
        while (reacted.Count < keyMap.Count)
        {
            foreach (var kv in keyMap)
            {
                // すでに反応 or フライング済みはスキップ
                if (reacted.Contains(kv.Key) || falseStart.Contains(kv.Key))
                    continue;

                if (Input.GetKeyDown(kv.Key))
                {
                    int ms = Mathf.RoundToInt((Time.time - start) * 1000f);
                    results.Add((kv.Value, ms));
                    reacted.Add(kv.Key);
                }
            }

            // フライングした人は強制的にスコア0扱いで記録
            foreach (var kv in falseStart)
            {
                if (!reacted.Contains(kv))
                {
                    results.Add((keyMap[kv], int.MaxValue)); // フライング: int.MaxValue
                    reacted.Add(kv);
                }
            }

            yield return null;
        }

        onFinish?.Invoke(results);
    }


    // 例: スコアの見た目を上書き（必要なら）
    protected override string FormatRawScore(int rawMs)
    {
        if (rawMs == int.MaxValue)
            return "フライング！";

        float sec = rawMs / 1000f;
        return $"{sec:0.00} 秒";
    }
}
