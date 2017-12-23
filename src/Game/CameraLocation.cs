using System;
using OpenTK;

namespace linerider.Game
{

    public class CameraLocation : GameService
    {
        private Vector2d _origin;
        private Vector2d _offset;
        public CameraLocation(Vector2d origin, Vector2d offset)
        {
            _origin = origin;
            _offset = offset;
        }
        public static CameraLocation FromNewPosition(Vector2d origin, Vector2d camera)
        {
            return new CameraLocation(origin,(camera - origin) * game.Track.Zoom);
        }
        public Vector2d GetPosition()
        {
            return _origin + (_offset / game.Track.Zoom );
        }
    }
}
