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
        public const double roundness = 0.8;
        public const double legacyratio = 0.125;
        public const double maxcamratio = 0.3;
        public const double mincamratio = 0.1;
        public Vector2d RiderPosition;
        public DoubleRect GetBox(double scale)
        {
            var width = (double)game.RenderSize.Width;
            var height = width * (9.0 / 16.0);//16:9 camera
            height /= game.Track.Zoom;
            width /= game.Track.Zoom;
            height *= scale;
            width *= scale;
            return new DoubleRect(RiderPosition.X - (width / 2), RiderPosition.Y - (height / 2), width, height);
        }
        public Vector2d Clamp(Vector2d camera)
        {
            var bounds = GetBox(legacyratio);
            return bounds.Clamp(camera);
        }
        public Vector2d SmoothClamp(Vector2d camera, double ppf)
        {
            var bounds = GetBox(GetSmoothCamRatio(ppf));
            var oval = bounds.EllipseClamp(camera);
            var square = bounds.Clamp(camera);
            return (Vector2d.Lerp(square, oval, roundness));
        }
        public bool SmoothIntersects(Vector2d camera, double ppf)
        {
            var bounds = GetBox(GetSmoothCamRatio(ppf));
            return bounds.Clamp(camera) == camera && bounds.EllipseClamp(camera) == camera;
        }
        public double GetSmoothCamRatio(double ppf)
        {
            if (!Constants.ScaleCamera)
                return maxcamratio;
            const int floor = 5;
            const int ceil = 50;


            var scale1 = MathHelper.Clamp((ppf - floor) / ceil, 0, 1);
            return maxcamratio - ((maxcamratio - mincamratio) * scale1);
        }
    }
}