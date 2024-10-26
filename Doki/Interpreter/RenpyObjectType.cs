using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Doki.Interpreter
{
    public enum RenpyObjectType
    {
        Dialog,
        Show,
        Hide,
        Scene,
        With,
        Play,
        Stop,
        Queue,
        Function,
        OneLinePython,
        InlinePython,
        LabelEntryPoint,
        NestedLabel,
        Goto,
        Return,
        GotoLine,
        IfElse,
        GotoLineUnless,
        GotoLineTimeout,
        ForkGotoLine,
        Immediate,
        ImmediateTransform,
        EasedTransform,
        ChoiceSet,
        Ease,
        Pause,
        LoadImage,
        Menu,
        Size,
        Time,
        Parallel,
        SetRandRange,
        Text,
        Expression,
        WaitForScreen,
        MenuInput,
        Window,
        NOP,
        Unlock,
        WindowAuto,
        ClrFlag
    }
}
