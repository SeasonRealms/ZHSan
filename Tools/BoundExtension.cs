using System;
// B1+：Bounds 已改為 GameManager 本地定義（原 FontStashSharp.Bounds）。
using GameManager;

namespace Tools;

public static class BoundExtension
{
    public static float Width(this Bounds bound)
    {
        return MathF.Abs(bound.X2 - bound.X);
    }

    public static float Height(this Bounds bound)
    {
        return MathF.Abs(bound.Y2 - bound.Y);
    }
}