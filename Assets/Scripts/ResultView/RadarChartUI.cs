using UnityEngine;

/// <summary>
/// 3軸レーダーチャート（正三角形）をUI上で描画するコンポーネント。
/// このスクリプトを付けた RectTransform を「中心」とみなし、
/// その子にあるポイントと線を動かして三角形グラフを作る。
/// </summary>
public class RadarChartUI : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float radius = 200f;  // 最大値のときの半径（px）

    [Header("Points (child RectTransforms with Image)")]
    [SerializeField] private RectTransform pointReflex;   // スピード
    [SerializeField] private RectTransform pointMash;     // たいりょく
    [SerializeField] private RectTransform pointHold;     // しゅうちゅう力

    [Header("Edges (thin Images between points)")]
    [SerializeField] private RectTransform edgeReflexMash;
    [SerializeField] private RectTransform edgeMashHold;
    [SerializeField] private RectTransform edgeHoldReflex;

    /// <summary>
    /// 各ジャンルの値(0〜1)をセットする。
    /// 0 = 中心, 1 = 正三角形の頂点。
    /// </summary>
    public void SetValues(float reflex01, float mash01, float hold01)
    {
        // 0〜1 にクランプ
        reflex01 = Mathf.Clamp01(reflex01);
        mash01 = Mathf.Clamp01(mash01);
        hold01 = Mathf.Clamp01(hold01);

        // このオブジェクト自身を中心とみなす
        var root = (RectTransform)transform;

        // 120度間隔の方向ベクトル
        // 上（90°）= スピード, 右下（-30°）= たいりょく, 左下（210°）= しゅうちゅう力
        Vector2 dirR = DirFromAngle(90f);
        Vector2 dirM = DirFromAngle(-30f);
        Vector2 dirH = DirFromAngle(210f);

        // 各ポイントのローカル座標を計算（中心からのオフセット）
        Vector2 pR = dirR * (radius * reflex01);
        Vector2 pM = dirM * (radius * mash01);
        Vector2 pH = dirH * (radius * hold01);

        // 点を配置
        SetPoint(pointReflex, pR);
        SetPoint(pointMash, pM);
        SetPoint(pointHold, pH);

        // 線を配置（from〜to の中点に細長いImageを置き、回転させる）
        SetEdge(edgeReflexMash, pR, pM);
        SetEdge(edgeMashHold, pM, pH);
        SetEdge(edgeHoldReflex, pH, pR);
    }

    // 角度(度)→単位ベクトル
    Vector2 DirFromAngle(float deg)
    {
        float rad = deg * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
    }

    void SetPoint(RectTransform rt, Vector2 localPos)
    {
        if (!rt) return;
        rt.anchoredPosition = localPos;   // 親(RadarChartRoot)からのオフセット
    }

    void SetEdge(RectTransform edge, Vector2 from, Vector2 to)
    {
        if (!edge) return;

        Vector2 mid = (from + to) * 0.5f;
        Vector2 diff = to - from;
        float len = diff.magnitude;

        edge.anchoredPosition = mid;

        // x方向の長さだけ更新（yはInspectorで太さを決めておく）
        var size = edge.sizeDelta;
        size.x = len;
        edge.sizeDelta = size;

        float angle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;
        edge.localRotation = Quaternion.Euler(0f, 0f, angle);
    }
}
