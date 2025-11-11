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
        // PlayPanel を開く（MGSceneBase が閉じているので）
        // PlayPanelは MGSceneBase で管理しているので、ここでは単にゲーム進行だけ書けばOK
        if (bulbOff) bulbOff.SetActive(true);
        if (bulbOn) bulbOn.SetActive(false);

        // 参加プレイヤーのキー/名前
        var joined = GameManager.Instance.GetJoinedPlayers();
        var keyMap = joined.ToDictionary(p => p.key, p => p.playerName);

        // ランダムな点灯待ち
        yield return new WaitForSeconds(UnityEngine.Random.Range(0.8f, 2.0f));

        // 点灯！
        if (bulbOff) bulbOff.SetActive(false);
        if (bulbOn) bulbOn.SetActive(true);

        float start = Time.time;
        var reacted = new HashSet<KeyCode>();
        var results = new List<(string name, int rawScore)>();

        while (reacted.Count < keyMap.Count)
        {
            foreach (var kv in keyMap)
            {
                if (!reacted.Contains(kv.Key) && Input.GetKeyDown(kv.Key))
                {
                    int ms = Mathf.RoundToInt((Time.time - start) * 1000f);
                    results.Add((kv.Value, ms));
                    reacted.Add(kv.Key);
                }
            }
            yield return null;
        }

        // MGSceneBase が ShowResult → ReportRoundResult まで面倒見ます
        onFinish?.Invoke(results);
    }

    // 例: スコアの見た目を上書き（必要なら）
    protected override string FormatRawScore(int rawMs)
    {
        return $"{rawMs} ms";
    }
}
