using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class MG_GobyoChallenge : MGSceneBase
{
    // 「5秒ちょうどに近いほど良い」ので、小さい方が良い
    protected override bool LowerScoreIsBetter => true;

    [Header("Timer UI")]
    [SerializeField] private TextMeshProUGUI timerLabel;   // 経過時間を表示するラベル
    [SerializeField] private float targetSeconds = 5f;     // 目標 5 秒
    [SerializeField] private float maxWaitSeconds = 9f;    // これを過ぎたら強制終了
    [SerializeField] private float visibleUntilSeconds = 3f;   // ★ 何秒までは見えるか（例：3秒までは見せて、その後は隠す）  

    private class Runner
    {
        public string name;
        public KeyCode key;
        public bool decided;
        public int rawMs;      // 「5秒からの誤差」(ms)
    }

    private readonly List<Runner> runners = new();

    protected override IEnumerator PlayRound(Action<List<(string name, int rawScore)>> onFinish)
    {
        runners.Clear();

        // 参加プレイヤー
        var joined = GameManager.Instance.GetJoinedPlayers();
        if (joined.Count == 0)
        {
            onFinish?.Invoke(new List<(string, int)>());
            yield break;
        }

        // Runner 構築
        foreach (var p in joined)
        {
            runners.Add(new Runner
            {
                name = p.playerName,
                key = p.key,
                decided = false,
                rawMs = 999999 // でかめの初期値
            });
        }

        float startTime = Time.time;
        int finished = 0;

        // タイマーを 0.00 からスタート表示
        if (timerLabel) timerLabel.text = "0.00秒";

        // 入力 & 時間計測ループ
        while (finished < runners.Count)
        {
            float elapsed = Time.time - startTime;

            // 経過時間表示（最大でも maxWaitSeconds くらいで止める）
            float displayTime = Mathf.Min(elapsed, maxWaitSeconds);

            if (timerLabel)
            {
                if (displayTime < visibleUntilSeconds)
                {
                    // まだ見えているフェーズ：ふつうに時間を表示
                    timerLabel.text = $"{displayTime:0.00}秒";
                }
                else
                {
                    // 隠すフェーズ：時間を見せない（好みで表示を変えてOK）
                    // timerLabel.text = "";               // 完全に消す
                    timerLabel.text = "？.??秒";           // なぞ時間っぽくする例
                }
            }


            // 規定時間を超えたら強制終了（押してない人は大きな誤差扱い）
            if (elapsed >= maxWaitSeconds)
            {
                break;
            }

            // 各プレイヤーのキー入力
            foreach (var r in runners)
            {
                if (r.decided) continue;

                if (Input.GetKeyDown(r.key))
                {
                    float diffSec = Mathf.Abs(elapsed - targetSeconds);   // 5秒との差
                    int ms = Mathf.RoundToInt(diffSec * 1000f);
                    r.rawMs = Mathf.Clamp(ms, 0, 999999);
                    r.decided = true;
                    finished++;
                }
            }

            yield return null;
        }

        // 押さずに終わった人は「大きな誤差」として扱う
        foreach (var r in runners)
        {
            if (!r.decided)
            {
                // 目標の2倍くらいズレてる想定の大きな値を入れておく
                int ms = Mathf.RoundToInt(targetSeconds * 2f * 1000f);
                r.rawMs = ms;
                r.decided = true;
            }
        }

        // GameManager へ返す rawScore リストを作成
        var results = runners.Select(r => (r.name, r.rawMs)).ToList();
        onFinish?.Invoke(results);
    }

    // 結果画面でのスコア表示：「±x.xx秒」
    protected override string FormatRawScore(int rawMs)
    {
        float sec = rawMs / 1000f;
        return $"±{sec:0.00}秒";
    }
}
