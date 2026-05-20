namespace ThoNohT.NohBoard.Extra
{
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using static System.Math;

    public static class Geom
    {
        public static TPoint GetCenter(this Rectangle rect)
        {
            return rect.Location + new Size(rect.Width / 2, rect.Height / 2);
        }

        public static Rectangle CircleToRectangle(TPoint center, int radius)
        {
            return new Rectangle(center.X - radius, center.Y - radius, radius * 2, radius * 2);
        }

        public static TPoint CircularTranslate(this TPoint start, int distance, float angle)
        {
            return new TPoint(start.X + distance * Cos(angle), start.Y + distance * Sin(angle));
        }

        public static float RadToDeg(float rad)
        {
            return (float)(rad / PI * 180);
        }

        public static float GetAngle(this SizeF speed)
        {
            if (speed.Width == 0)
                return (float)(speed.Height > 0 ? 0 : PI);

            return (speed.Width < 0 ? 1 : 0) * (float)PI + (float)Atan(speed.Height / speed.Width);
        }

        public static float Length(this SizeF speed)
        {
            return (float)Sqrt(Pow(speed.Width, 2) + (float)Pow(speed.Height, 2));
        }

        public static SizeF RotateDegrees(this SizeF speed, int degrees)
        {
            
            var radians = degrees * PI / 180;
            var inputList = new[] { new PointF(speed.Width, speed.Height) };
            var rotationMatrix = new Matrix(
                (float)Cos(radians), (float)-Sin(radians),
                (float)Sin(radians), (float)Cos(radians),
                0, 0);

            rotationMatrix.TransformVectors(inputList);
            return new SizeF(inputList[0].X, inputList[0].Y);
        }

        public static SizeF GetUnitVector(this SizeF speed)
        {
            
            return speed.Multiply(1 / speed.Length());
        }

        public static SizeF ProjectOn(this SizeF toProject, SizeF projectOn)
        {
            
            var unitVector = projectOn.GetUnitVector();
            var projectionMatrix = new Matrix(
                unitVector.Width * unitVector.Width, unitVector.Width * unitVector.Height,
                unitVector.Width * unitVector.Height, unitVector.Height * unitVector.Height,
                0, 0);

            var inputList = new[] { new PointF(toProject.Width, toProject.Height) };
            projectionMatrix.TransformVectors(inputList);
            return new SizeF(inputList[0].X, inputList[0].Y);
        }

        public static SizeF Multiply(this SizeF speed, float scalar)
        {
            return new SizeF(speed.Width * scalar, speed.Height * scalar);
        }

        public static float Length(this Size speed)
        {
            return (float)Sqrt(Pow(speed.Width, 2) + (float)Pow(speed.Height, 2));
        }

        public static Rectangle ToOrigin(this Rectangle rect)
        {
            return new Rectangle(0, 0, rect.Width, rect.Height);
        }
    }
}
