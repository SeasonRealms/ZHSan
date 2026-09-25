
global using System.Text;
global using System.Text.Json.Serialization;
global using System.Diagnostics;
global using System.Runtime.ExceptionServices;
global using System.Runtime.CompilerServices;
global using System.Runtime.InteropServices;
global using System.Collections.Concurrent;
global using Vector3 = System.Numerics.Vector3;

global using Animation = GameObjects.Animations.Animation;
// B0：MAUI 隱式全局 using（Microsoft.Maui.Graphics / Microsoft.Maui.Controls）帶入同名類型，
// 以下別名把 Point/Frame/CheckBox/Region 固定到專案與 SeasonXNA 的定義，避免 CS0104 歧義。
global using Point = Microsoft.Xna.Framework.Point;
global using Platform = Platforms.Platform;
global using Keyboard = Microsoft.Xna.Framework.Input.Keyboard;
global using Color = Microsoft.Xna.Framework.Color;
global using Frame = GamePanels.Scrollbar.Frame;
global using CheckBox = GamePanels.CheckBox;
global using Region = GameObjects.ArchitectureDetail.Region;
global using Condition = GameObjects.Conditions.Condition;
global using Image = PersonPortraitPlugin.Image;
global using Architecture = GameObjects.Architecture;
global using Size = System.Drawing.Size;
global using Font = GameGlobal.Font;


