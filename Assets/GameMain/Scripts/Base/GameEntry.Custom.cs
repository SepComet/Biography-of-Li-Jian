//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using CustomComponent;
using UnityEngine;

/// <summary>
/// 游戏入口。
/// </summary>
public partial class GameEntry : MonoBehaviour
{
    public static BuiltinDataComponent BuiltinData { get; private set; }
    public static CombineComponent Combine { get; private set; }
    public static DialogComponent Dialog { get; private set; }

    private static void InitCustomComponents()
    {
        BuiltinData = UnityGameFramework.Runtime.GameEntry.GetComponent<BuiltinDataComponent>();
        Combine = UnityGameFramework.Runtime.GameEntry.GetComponent<CombineComponent>();
        Dialog = UnityGameFramework.Runtime.GameEntry.GetComponent<DialogComponent>();
    }
}
