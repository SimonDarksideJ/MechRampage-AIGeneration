using System.Collections.Generic;
using MechRampage.Core.World;
using MechRampage.Core.Rendering;

namespace MechRampage.Core
{
    /// <summary>
    /// Aggregate root for prototype save data. Wraps scene snapshot plus upgrade + UI state.
    /// </summary>
    public class SaveData
    {
        public SceneSnapshot Scene { get; set; }
        public List<UpgradeLevel> Upgrades { get; set; } = new();
        public bool OverlayEnabled { get; set; }
        public DebugOverlayMode OverlayMode { get; set; }
    public bool FillAttackRanges { get; set; }
    public float CameraTargetX { get; set; }
    public float CameraTargetZ { get; set; }
    public float CameraZoom { get; set; }
    public int MiniMapSizeIndex { get; set; }
    public int MiniMapZoomLevel { get; set; }
    public float CameraTweenLerpFactor { get; set; }
    public int SaveVersion { get; set; } = 3; // version 3 adds minimap zoom + camera tween factor
    }

    public class UpgradeLevel
    {
        public string Id { get; set; }
        public int Level { get; set; }
    }
}
