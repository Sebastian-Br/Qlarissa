using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qlarissa.ErrorCorrection
{
    public class InvLogFrameAsHeatmapPoint
    {
        /// <summary>
        /// Overestimation = Blue
        /// Perfect estimate = Green
        /// Underestimate = Red
        /// </summary>
        /// <param name="frame"></param>
        public InvLogFrameAsHeatmapPoint(ErrorCorrectionForInvLogFrame frame)
        {
            X_RSquared = frame.RSquared;
            Y_Slope = frame.SlopeOfOuterFunctionAtEndOfTrainingPeriod;
            double colorSlide = Sigmoid_EvaluateDeviation(frame.EstimateDeviationPercentage);
            double red = GetRednessFromColorSlide(colorSlide);
            double green = GetGreennessFromColorSlide(colorSlide);
            double blue = GetBluenessFromColorSlide(colorSlide);
            Z_Color = new((byte)red, (byte)green, (byte)blue, 120);
            MarkerSize = 5.0f;
        }

        public double X_RSquared { get; private set; }

        public double Y_Slope { get; private set; }

        public ScottPlot.Color Z_Color { get; private set; }

        public float MarkerSize { get; private set; }

        private double Sigmoid_EvaluateDeviation(double deviationPercentage)
        {
            return
                (1.0)
                /
                (1.0 + Math.Exp(-deviationPercentage * 0.075));
        }

        private double GetGreennessFromColorSlide(double colorSlide)
        {
            double differenceFromPerfectPrediction = Math.Abs(colorSlide - 0.5); // this value ranges from [0,0.5[ 0->perfect green. 0.5 -> no green.
            double standardizedDifference = differenceFromPerfectPrediction * 2.0; // from 0 to 1
            double greennessFactor = 1.0 - standardizedDifference;
            return greennessFactor * 255.0;
        }

        private double GetBluenessFromColorSlide(double colorSlide)
        {
            double overEstimation = colorSlide - 0.5;
            if(overEstimation <= 0)
            {
                return 0.0;
            }

            double bluenessFactor = overEstimation * 2.0;
            return bluenessFactor * 255.0;
        }

        private double GetRednessFromColorSlide(double colorSlide)
        {
            double underEstimation = colorSlide - 0.5;
            if (underEstimation >= 0.0)
            {
                return 0.0;
            }

            double bluenessFactor = -2.0 * underEstimation;
            return bluenessFactor * 255.0;
        }
    }
}