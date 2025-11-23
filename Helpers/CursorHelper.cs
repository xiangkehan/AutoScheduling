using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;

namespace AutoScheduling3.Helpers
{
    /// <summary>
    /// 光标辅助类，用于在可交互元素上设置手型光标
    /// 注意：由于 ProtectedCursor 是受保护成员，此辅助类提供的是事件处理器
    /// 实际使用时需要在 Page/Control 内部调用 this.ProtectedCursor
    /// </summary>
    public static class CursorHelper
    {
        /// <summary>
        /// 创建手型光标
        /// </summary>
        public static InputCursor CreateHandCursor()
        {
            return InputSystemCursor.Create(InputSystemCursorShape.Hand);
        }

        /// <summary>
        /// 创建箭头光标
        /// </summary>
        public static InputCursor CreateArrowCursor()
        {
            return InputSystemCursor.Create(InputSystemCursorShape.Arrow);
        }

        /// <summary>
        /// 创建等待光标
        /// </summary>
        public static InputCursor CreateWaitCursor()
        {
            return InputSystemCursor.Create(InputSystemCursorShape.Wait);
        }

        /// <summary>
        /// 创建文本选择光标
        /// </summary>
        public static InputCursor CreateIBeamCursor()
        {
            return InputSystemCursor.Create(InputSystemCursorShape.IBeam);
        }
    }
}
