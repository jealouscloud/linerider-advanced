using System;
using System.Collections.Generic;
using linerider.Game;

namespace linerider.Drawing
{
    public class DrawOptions
    {
        public bool Paused = false;
        public bool LineColors = true;
        public bool GravityWells = false;
        public bool NightMode=false;
        public KnobState KnobState = 0;
        public float Blend = 1;
        public bool Overlay = false;
        public Rider Rider;
        public bool DrawFlag;
        public Rider FlagRider;
        public List<int> RiderDiagnosis = null;
        public bool ShowContactLines = false;
        public bool ShowMomentumVectors = false;
        public int Iteration = 6;
        public float Zoom;
        public int OverlayFrame = -1;
        public bool IsRunning
        {
            get
            {
                return !Paused;
            }
        }
    }
}