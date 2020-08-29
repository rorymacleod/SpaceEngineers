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
            public IList<IMyTextSurface> Surfaces = new List<IMyTextSurface>();

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

            public void Clear()
            {
                foreach (var s in Surfaces)
                {
                    s.WriteText(string.Empty);
                }
            }

            public void WriteTitle(string text)
            {
                Clear();
                Write(text);
                Write("=====");
            }

            public void Write(string text)
            {
                foreach (var s in Surfaces)
                {
                    s.WriteText(text, true);
                    s.WriteText(Environment.NewLine, true);
                }
            }
        }
    }
}
