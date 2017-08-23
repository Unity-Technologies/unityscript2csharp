using System;

namespace UnityScript2CSharp
{
    [Flags]
    enum AnchorKind
    {
        None,
        Above,
        Below,
        Left,
        Right,

        All = Above | Below | Left | Right
    }
}