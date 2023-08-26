using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRage;
using VRageMath;
using System.Data;

namespace IngameScript
{
    partial class Program
    {
        public class LcdManager
        {
            private readonly MyGridProgram Grid;
            public List<IMyTextSurface> Surfaces = new List<IMyTextSurface>();
            public List<string> Lines = new List<string>();
            public string Title = string.Empty;

            public LcdManager(MyGridProgram grid, IMyTextSurfaceProvider provider, int surface)
            {
                Grid = grid;
                var ts = provider.GetSurface(surface);
                Add(ts);
            }

            public void Add(IMyTextSurface surface)
            {
                surface.ContentType = ContentType.TEXT_AND_IMAGE;
                surface.WriteText(string.Empty);
                Surfaces.Add(surface);
            }

            public void AddTextSurfaces(string name)
            {
                var blocks = Grid.FindBlocks<IMyTerminalBlock>(name);
                foreach (var block in blocks)
                {
                    if (block is IMyTextSurface)
                    {
                        Add((IMyTextSurface)block);
                    }
                }
            }

            public void Clear()
            {
                Lines.Clear();
                Title = string.Empty;
                foreach (var s in Surfaces)
                {
                    s.WriteText(string.Empty);
                }
            }

            public void WriteTitle(string text)
            {
                Clear();
                Title = text;
                Surfaces.ForEach(s => UpdateSurface(s));
            }

            public string[] ReadLines()
            {
                var buffer = new StringBuilder();
                Surfaces[0].ReadText(buffer);
                var lines = buffer.ToString().Split(new[] { Environment.NewLine }, StringSplitOptions.None);
                return lines;
            }

            public void UpdateLine(string text)
            {
                Lines[Lines.Count - 1] = GetTimestamp() + text;
                Surfaces.ForEach(s => UpdateSurface(s));
            }

            public void Write(string text)
            {
                Lines.Add(GetTimestamp() + text);

                foreach (var s in Surfaces)
                {
                    s.WriteText(Lines[Lines.Count - 1], true);
                    s.WriteText(Environment.NewLine, true);
                }
            }

            private string GetTimestamp()
            {
                return DateTime.Now.ToString("mm:ss ");
            }

            private void UpdateSurface(IMyTextSurface surface)
            {
                var sb = new StringBuilder();
                if (!string.IsNullOrEmpty(Title))
                {
                    sb.AppendLine(Title);
                    
                    var titleSize = surface.MeasureStringInPixels(sb, surface.Font, surface.FontSize);
                    var charSize = surface.MeasureStringInPixels(new StringBuilder("="), surface.Font, surface.FontSize);
                    string chars = new string('=', (int)Math.Ceiling(titleSize.X / charSize.X));
                    sb.AppendLine(chars);
                }

                Lines.ForEach(l => sb.AppendLine(l));
                surface.WriteText(sb);
            }
        }
    }
}
