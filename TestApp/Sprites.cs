using System.ComponentModel;

namespace OvenTK.TestApp;

public enum Sprites
{
    None,
    [Description("node4.png")]
    Node,
    [Description("robot_white.png")]
    Robot,
    [Description("export_notes_white.png")]
    ExportNotes,
    [Description("check.png")]
    Check,
    [Description("error.png")]
    Error,
}
