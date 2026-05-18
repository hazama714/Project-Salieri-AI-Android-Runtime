// 機能: VRM/FBX共通の表情制御インターフェース定義
// バージョン: v1.0.0
// 更新日: 2025-06-08
// 改修内容:
// - SetExpression(string emotion): 感情に対応する表情を適用
// - ResetExpression(): すべてのBlendShapeをリセット（任意）

public interface IExpressionController
{
    /// <summary>
    /// 指定された感情に対応する表情を適用する
    /// 例: "happy" → "笑い" BlendShape を100に
    /// </summary>
    void SetExpression(string emotion);

    /// <summary>
    /// 表情をリセットする（全てのBlendShapeのweightを0に）
    /// </summary>
    void ResetExpression();
}
