//
//  SceneryRenderer.cs
//
//  Author:
//       Noah Ablaseau <nablaseau@hotmail.com>
//
//  Copyright (c) 2017 
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Collections.Concurrent;
using System.Linq;
namespace linerider.Drawing
{
    internal class SceneryRenderer : GameService
    {
        private class indexfragment
        {
            public List<int> indices;
            public int start;
        }
        #region Fields

        public bool RequiresUpdate = true;
        private SceneryVBO _editorvbo;
        private SceneryVBO _playbackbvo;

        private ConcurrentDictionary<int, indexfragment> _editorindices = new ConcurrentDictionary<int, indexfragment>();
        private ConcurrentDictionary<int, indexfragment> _playbackindices = new ConcurrentDictionary<int, indexfragment>();

        private object SyncRoot = new object();

        #endregion Fields

        #region Constructors

        public SceneryRenderer()
        {
            _editorvbo = new SceneryVBO();
            _playbackbvo = new SceneryVBO();
        }

        #endregion Constructors

        #region Methods
        public void Render(bool colors)
        {
            using (new GLEnableCap(EnableCap.Texture2D))
            {
                GL.BlendFunc(BlendingFactorSrc.One, BlendingFactorDest.OneMinusSrcAlpha);
                GameDrawingMatrix.Enter();
                try
                {
                    if (colors)
                    {
                        _editorvbo.Draw();
                    }
                    else
                    {
                        _playbackbvo.Draw();
                    }
                    RequiresUpdate = false;
                }
                finally
                {
                    GameDrawingMatrix.Exit();
                }
            }
        }
        /// <summary>
        /// Sets line to initialize the entire track.
        /// </summary>
        public void InitializeTrack(List<Line> lines)
        {
            Invalidate();
            lock (SyncRoot)
            {
                _editorvbo.Clear();
                _playbackbvo.Clear();

                _playbackindices.Clear();
                _editorindices.Clear();
                List<int> eindices = new List<int>(lines.Count * (6 * 3));//size of line * linecount
                List<int> pindices = new List<int>(lines.Count * (6 * 3));//size of line * linecount
                int ipos = 0;
                int ppos = 0;
                foreach (var l in lines)
                {
                    if (!(l is SceneryLine))
                        continue;
                    var data = _editorvbo.DrawBasicTrackLine((Vector2)l.Position, (Vector2)l.Position2, Color.FromArgb(255, 0x00, 0xCC, 0x00), ((SceneryLine)l).Width);
                    var start = ipos;
                    ipos += data.Count;
                    eindices.AddRange(data);

                    _editorindices[l.ID] = new indexfragment() { start = start, indices = data };

                    data = _playbackbvo.DrawBasicTrackLine((Vector2)l.Position, (Vector2)l.Position2, Settings.NightMode ? Color.White : Color.Black, ((SceneryLine)l).Width);
                    start = ppos;
                    ppos += data.Count;
                    pindices.AddRange(data);

                    _playbackindices[l.ID] = new indexfragment() { start = start, indices = data };
                }
                _editorvbo.SetIndices(eindices);
                _playbackbvo.SetIndices(pindices);
                _editorvbo.Update();
                _playbackbvo.Update();
            }
        }

        public void AddLine(Line l)
        {
            lock (SyncRoot)
            {
                Invalidate();
                indexfragment lookup;
                var data = _editorvbo.DrawBasicTrackLine((Vector2)l.Position, (Vector2)l.Position2, Color.FromArgb(0x00, 0xCC, 0x00), ((SceneryLine)l).Width);

                if (_editorindices.TryGetValue(l.ID, out lookup))
                {
                    lookup.indices = data;
                    _editorvbo.SetIndices(lookup.start, data);
                }
                else
                {
                    var start = _editorvbo.AddIndices(data);
                    _editorindices[l.ID] = new indexfragment() { start = start, indices = data };
                }

                data = _playbackbvo.DrawBasicTrackLine((Vector2)l.Position, (Vector2)l.Position2, Settings.NightMode ? Color.White : Color.Black, ((SceneryLine)l).Width);

                if (_playbackindices.TryGetValue(l.ID, out lookup))
                {
                    lookup.indices = data;
                    _playbackbvo.SetIndices(lookup.start, data);
                }
                else
                {
                    var start = _playbackbvo.AddIndices(data);
                    _playbackindices[l.ID] = new indexfragment() { start = start, indices = data };
                }
            }
        }

        public void RemoveLine(Line line)
        {
            lock (SyncRoot)
            {
                Invalidate();
                var onelookup = _editorindices[line.ID];
                _editorvbo.FreeVertices(onelookup.indices);
                if (_editorvbo.TryRemoveIndices(onelookup.start, onelookup.indices.Count))//we're the most recent line
                {
                    while (!_editorindices.TryRemove(line.ID, out onelookup)) { }//concurrent dictionary woes
                }
                else
                {
                    onelookup.indices = null;
                }

                onelookup = _playbackindices[line.ID];
                _playbackbvo.FreeVertices(onelookup.indices);
                if (_playbackbvo.TryRemoveIndices(onelookup.start, onelookup.indices.Count))//we're the most recent line
                {
                    while (!_playbackindices.TryRemove(line.ID, out onelookup)) { }//concurrent dictionary woes
                }
                else
                {
                    onelookup.indices = null;
                }
            }
        }

        public void LineChanged(Line line)
        {
            lock (SyncRoot)
            {
                Invalidate();

                _editorvbo.LineChangedFreeVertices(_editorindices[line.ID].indices);
                _editorvbo.DrawBasicTrackLine((Vector2)line.Position, (Vector2)line.Position2, Color.FromArgb(0x00, 0xCC, 0x00), ((SceneryLine)line).Width);

                _playbackbvo.LineChangedFreeVertices(_playbackindices[line.ID].indices);
                _playbackbvo.DrawBasicTrackLine((Vector2)line.Position, (Vector2)line.Position2, Settings.NightMode ? Color.White : Color.Black, ((SceneryLine)line).Width);
            }
        }

        private void Invalidate()
        {
            RequiresUpdate = true;
        }
        #endregion Methods

    }
}