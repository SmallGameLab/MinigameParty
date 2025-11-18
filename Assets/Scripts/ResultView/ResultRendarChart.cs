using UnityEngine;

/// <summary>
/// 3ジャンル用レーダーチャート（三角形）
/// Canvas 上の RectTransform を基準に、LineRenderer で三角形を描く
/// </summary>
public class ResultRadarChart : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private RectTransform chartArea;  // RadarChartRoot の RectTransform
    [SerializeField] private LineRenderer polygon;     // 子オブジェクトの LineRenderer

    [Header("Config")]
    [SerializeField] private float radius = 180f;      // 最大値(20pt)のときの半径

    // 角度は「画面上」での向き（度数法）
    // reflex: 上, mash: 右下, hold: 左下 みたいな配置にしています
    private const float AngleReflex = 90f;
    private const float AngleMash = -30f;   // 330度
    private const float AngleHold = -150f;  // 210度

    /// <summary>
    /// 各ジャンルのポイント(0〜20)からレーダーチャートを更新
    /// </summary>
    public void SetValues(int reflex, int mash, int hold)
    {
        if (!chartArea || !polygon) return;

        // 0〜1 に正規化（最大20pt）
        float nr = Mathf.Clamp01(reflex / 20f);
        float nm = Mathf.Clamp01(mash / 20f);
        float nh = Mathf.Clamp01(hold / 20f);

        // 3頂点をローカル座標で計算
        Vector3 pReflex = Dir(AngleReflex) * (radius * nr);
        Vector3 pMash = Dir(AngleMash) * (radius * nm);
        Vector3 pHold = Dir(AngleHold) * (radius * nh);

        // Canvas → ワールド座標へ変換して LineRenderer に渡す
        Vector3 w0 = LocalToWorld(pReflex);
        Vector3 w1 = LocalToWorld(pMash);
        Vector3 w2 = LocalToWorld(pHold);

        polygon.positionCount = 3;
        polygon.SetPosition(0, w0);
        polygon.SetPosition(1, w1);
        polygon.SetPosition(2, w2);
    }

    // 角度(度)→単位ベクトル
    private static Vector3 Dir(float angleDeg)
    {
        float rad = angleDeg * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f);
    }

    // RadarChartRoot のローカル座標 → ワールド座標
    private Vector3 LocalToWorld(Vector3 local)
    {
        return chartArea.TransformPoint(local);
    }

    // エディタでプレビュー確認しやすいように簡単なテスト
    private void OnValidate()
    {
        if (Application.isPlaying) return;
        // テスト用値：全ジャンル最大
        if (chartArea && polygon)
        {
            SetValues(20, 20, 20);
        }
    }
}
