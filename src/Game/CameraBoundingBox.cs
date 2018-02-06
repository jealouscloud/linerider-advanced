//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using linerider.Game;
using linerider.Rendering;
using linerider.Utils;
using OpenTK;
namespace linerider.Game
{
    public class CameraBoundingBox : GameService
    {
        public const float roundness = 0.8f;
        public const float legacyratio = 0.125f;
        public const float maxcamratio = 0.3f;
        public Vector2d RiderPosition;
        public DoubleRect GetBox(float scale)
        {
            var width = (double)game.RenderSize.Width;
            var height = width * (9.0 / 16.0);//16:9 camera
            height /= game.Track.Zoom;
            width /= game.Track.Zoom;
            height *= scale;
            width *= scale;
            return new DoubleRect(RiderPosition.X - (width / 2), RiderPosition.Y - (height / 2), width, height);
        }
        public CameraLocation Clamp(Vector2d camera)
        {
            var bounds = GetBox(legacyratio);
            return CameraLocation.FromNewPosition(RiderPosition, bounds.Clamp(camera));
        }
        public CameraLocation SmoothClamp(Vector2d camera, float ppf)
        {
            var bounds = GetBox(GetSmoothCamRatio(ppf));
            var oval = bounds.EllipseClamp(camera);
            var square = bounds.Clamp(camera);
            return CameraLocation.FromNewPosition(RiderPosition, (Vector2d.Lerp(square, oval, roundness)));
        }
        public bool SmoothIntersects(Vector2d camera, float ppf)
        {
            var bounds = GetBox(GetSmoothCamRatio(ppf));
            return bounds.Clamp(camera) == camera && bounds.EllipseClamp(camera) == camera;
        }
        public float GetSmoothCamRatio(float ppf)
        {
            if (!Camera.ScaleCamera)
                return maxcamratio;
            const int floor = 5;
            const int ceil = 30;

            ppf = Math.Max(0, ppf - floor);
            var scale1 = (Math.Min(ceil, ppf) / ceil);
            return (float)(maxcamratio - ((maxcamratio * 0.4) * scale1));
        }
    }
}