using RealTimePPDisplayer.Displayer;

namespace OrtdpWrapper.Rtppi
{
    public class RtppInfo : DisplayerBase
    {
        private readonly PPTuple _previousPpTuple = new PPTuple();
        private PPTuple _speedPpTuple;

        public PPTuple SmoothPp => _previousPpTuple;
        public override void FixedDisplay(double time)
        {
            RealTimePPDisplayer.SmoothMath.SmoothDampPPTuple(_previousPpTuple, Pp, ref _speedPpTuple, 0.033);
        }
    }
}