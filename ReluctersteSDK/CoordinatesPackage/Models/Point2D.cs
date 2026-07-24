using ReluctersteSDK.CoordinatesPackage.Vector;

namespace ReluctersteSDK.CoordinatesPackage.Models
{
    public abstract class Point2D
    {
        public double pointX { get; set; }
        public double pointY { get; set; }
        public abstract void Update();
        protected abstract void move();
        protected abstract void accelerate();
    }

    public class PolarSpeedPoint2D : Point2D
    {
        public PolarVector Speed = new PolarVector(0, 0);
        public PolarVector Acceleration = new PolarVector(0, 0);

        public override void Update()
        {
            accelerate();
            move();
        }
        protected override void move()
        {
            pointX += (Speed.R) * Math.Cos(Speed.Theta);
            pointY += (Speed.R) * Math.Sin(Speed.Theta);
        }
        protected override void accelerate()
        {
            Speed += Acceleration;
        }
    }
}
