// ============================================================================
// NQ Setup Scanner v1.0
// MarketRegime.cs - Choppiness Index and ADX calculations for regime detection
// ============================================================================

using System;
using System.Collections.Generic;

namespace NQSetupScanner
{
    /// <summary>
    /// Market Regime Detector using Choppiness Index and ADX
    /// Determines if market is Trending, Ranging, or in Transition
    /// </summary>
    public class MarketRegimeDetector
    {
        // Configuration
        private readonly int _chopPeriod;
        private readonly int _adxPeriod;
        private readonly double _trendingChopThreshold;
        private readonly double _rangingChopThreshold;
        private readonly double _trendingAdxThreshold;
        private readonly double _rangingAdxThreshold;

        // Calculation buffers
        private readonly Queue<double> _atrBuffer;
        private readonly Queue<double> _highBuffer;
        private readonly Queue<double> _lowBuffer;
        private readonly Queue<double> _closeBuffer;

        // ADX components
        private double _smoothedPlusDM;
        private double _smoothedMinusDM;
        private double _smoothedTR;
        private double _smoothedDX;

        // Current values
        public double ChoppinessIndex { get; private set; }
        public double ADX { get; private set; }
        public double PlusDI { get; private set; }
        public double MinusDI { get; private set; }
        public MarketRegimeType CurrentRegime { get; private set; }
        public DirectionalBias Bias { get; private set; }

        // Initialization flag
        private int _barsCalculated;
        private double _prevClose;
        private double _prevHigh;
        private double _prevLow;

        /// <summary>
        /// Initialize Market Regime Detector
        /// </summary>
        /// <param name="chopPeriod">Choppiness Index period (default 14)</param>
        /// <param name="adxPeriod">ADX period (default 14)</param>
        /// <param name="trendingChopThreshold">Choppiness below this = trending (default 38.2)</param>
        /// <param name="rangingChopThreshold">Choppiness above this = ranging (default 61.8)</param>
        public MarketRegimeDetector(
            int chopPeriod = 14,
            int adxPeriod = 14,
            double trendingChopThreshold = 38.2,
            double rangingChopThreshold = 61.8)
        {
            _chopPeriod = chopPeriod;
            _adxPeriod = adxPeriod;
            _trendingChopThreshold = trendingChopThreshold;
            _rangingChopThreshold = rangingChopThreshold;
            _trendingAdxThreshold = 25.0;
            _rangingAdxThreshold = 20.0;

            _atrBuffer = new Queue<double>();
            _highBuffer = new Queue<double>();
            _lowBuffer = new Queue<double>();
            _closeBuffer = new Queue<double>();

            _barsCalculated = 0;
            ChoppinessIndex = 50.0;
            ADX = 25.0;
            PlusDI = 0;
            MinusDI = 0;
            CurrentRegime = MarketRegimeType.Transition;
            Bias = DirectionalBias.Neutral;
        }

        /// <summary>
        /// Reset all calculations
        /// </summary>
        public void Reset()
        {
            _atrBuffer.Clear();
            _highBuffer.Clear();
            _lowBuffer.Clear();
            _closeBuffer.Clear();
            _barsCalculated = 0;
            _smoothedPlusDM = 0;
            _smoothedMinusDM = 0;
            _smoothedTR = 0;
            _smoothedDX = 0;
            ChoppinessIndex = 50.0;
            ADX = 25.0;
            PlusDI = 0;
            MinusDI = 0;
            CurrentRegime = MarketRegimeType.Transition;
            Bias = DirectionalBias.Neutral;
        }

        /// <summary>
        /// Calculate regime indicators for a new bar
        /// </summary>
        /// <param name="high">Bar high price</param>
        /// <param name="low">Bar low price</param>
        /// <param name="close">Bar close price</param>
        public void Calculate(double high, double low, double close)
        {
            _barsCalculated++;

            if (_barsCalculated == 1)
            {
                // First bar - just store values
                _prevHigh = high;
                _prevLow = low;
                _prevClose = close;

                _highBuffer.Enqueue(high);
                _lowBuffer.Enqueue(low);
                _closeBuffer.Enqueue(close);

                return;
            }

            // Calculate True Range
            double tr = MathUtilities.TrueRange(high, low, _prevClose);
            _atrBuffer.Enqueue(tr);

            // Maintain buffer sizes
            _highBuffer.Enqueue(high);
            _lowBuffer.Enqueue(low);
            _closeBuffer.Enqueue(close);

            while (_atrBuffer.Count > _chopPeriod) _atrBuffer.Dequeue();
            while (_highBuffer.Count > _chopPeriod) _highBuffer.Dequeue();
            while (_lowBuffer.Count > _chopPeriod) _lowBuffer.Dequeue();
            while (_closeBuffer.Count > _chopPeriod) _closeBuffer.Dequeue();

            // Calculate ADX components
            CalculateADX(high, low, close);

            // Calculate Choppiness Index once we have enough data
            if (_barsCalculated >= _chopPeriod)
            {
                CalculateChoppiness();
            }

            // Determine regime
            DetermineRegime();

            // Update previous values
            _prevHigh = high;
            _prevLow = low;
            _prevClose = close;
        }

        /// <summary>
        /// Calculate Choppiness Index
        /// Formula: 100 * LOG10(SUM(ATR,n) / (HH - LL)) / LOG10(n)
        /// </summary>
        private void CalculateChoppiness()
        {
            double[] atrArray = _atrBuffer.ToArray();
            double[] highArray = _highBuffer.ToArray();
            double[] lowArray = _lowBuffer.ToArray();

            // Sum of ATR over period
            double atrSum = 0;
            foreach (double atr in atrArray)
            {
                atrSum += atr;
            }

            // Highest high and lowest low over period
            double highestHigh = double.MinValue;
            double lowestLow = double.MaxValue;

            foreach (double h in highArray)
            {
                if (h > highestHigh) highestHigh = h;
            }

            foreach (double l in lowArray)
            {
                if (l < lowestLow) lowestLow = l;
            }

            double range = highestHigh - lowestLow;

            // Avoid division by zero
            if (range > 0 && atrSum > 0)
            {
                // Choppiness Index formula
                ChoppinessIndex = 100.0 * Math.Log10(atrSum / range) / Math.Log10(_chopPeriod);

                // Clamp to valid range
                ChoppinessIndex = Math.Max(0, Math.Min(100, ChoppinessIndex));
            }
        }

        /// <summary>
        /// Calculate ADX, +DI, and -DI
        /// Using Wilder's smoothing method
        /// </summary>
        private void CalculateADX(double high, double low, double close)
        {
            // Calculate directional movement
            double plusDM = 0;
            double minusDM = 0;

            double upMove = high - _prevHigh;
            double downMove = _prevLow - low;

            if (upMove > downMove && upMove > 0)
            {
                plusDM = upMove;
            }
            else if (downMove > upMove && downMove > 0)
            {
                minusDM = downMove;
            }

            // Calculate True Range
            double tr = MathUtilities.TrueRange(high, low, _prevClose);

            // Smooth using Wilder's method
            if (_barsCalculated <= _adxPeriod + 1)
            {
                // Initial smoothing - simple sum
                _smoothedPlusDM += plusDM;
                _smoothedMinusDM += minusDM;
                _smoothedTR += tr;

                if (_barsCalculated == _adxPeriod + 1)
                {
                    // First smoothed values
                    _smoothedPlusDM = _smoothedPlusDM;
                    _smoothedMinusDM = _smoothedMinusDM;
                    _smoothedTR = _smoothedTR;
                }
            }
            else
            {
                // Wilder's smoothing
                _smoothedPlusDM = _smoothedPlusDM - (_smoothedPlusDM / _adxPeriod) + plusDM;
                _smoothedMinusDM = _smoothedMinusDM - (_smoothedMinusDM / _adxPeriod) + minusDM;
                _smoothedTR = _smoothedTR - (_smoothedTR / _adxPeriod) + tr;
            }

            // Calculate +DI and -DI
            if (_smoothedTR > 0)
            {
                PlusDI = 100.0 * _smoothedPlusDM / _smoothedTR;
                MinusDI = 100.0 * _smoothedMinusDM / _smoothedTR;
            }

            // Calculate DX
            double diSum = PlusDI + MinusDI;
            double dx = 0;
            if (diSum > 0)
            {
                dx = 100.0 * Math.Abs(PlusDI - MinusDI) / diSum;
            }

            // Smooth ADX
            if (_barsCalculated <= _adxPeriod * 2)
            {
                _smoothedDX += dx;
                if (_barsCalculated == _adxPeriod * 2)
                {
                    ADX = _smoothedDX / _adxPeriod;
                }
            }
            else
            {
                ADX = (_smoothedDX * (_adxPeriod - 1) + dx) / _adxPeriod;
                _smoothedDX = ADX;
            }

            // Clamp ADX
            ADX = Math.Max(0, Math.Min(100, ADX));
        }

        /// <summary>
        /// Determine market regime based on Choppiness and ADX
        /// </summary>
        private void DetermineRegime()
        {
            // Trending: Choppiness < 38.2 AND ADX > 25
            if (ChoppinessIndex < _trendingChopThreshold && ADX > _trendingAdxThreshold)
            {
                CurrentRegime = MarketRegimeType.Trending;
            }
            // Ranging: Choppiness > 61.8 AND ADX < 20
            else if (ChoppinessIndex > _rangingChopThreshold && ADX < _rangingAdxThreshold)
            {
                CurrentRegime = MarketRegimeType.Ranging;
            }
            // Transition: Everything else
            else
            {
                CurrentRegime = MarketRegimeType.Transition;
            }

            // Determine directional bias
            if (PlusDI > MinusDI + 2) // Add small buffer to avoid noise
            {
                Bias = DirectionalBias.Long;
            }
            else if (MinusDI > PlusDI + 2)
            {
                Bias = DirectionalBias.Short;
            }
            else
            {
                Bias = DirectionalBias.Neutral;
            }
        }

        /// <summary>
        /// Get display lines for the regime panel
        /// </summary>
        public string[] GetPanelLines()
        {
            string regimeColor = CurrentRegime == MarketRegimeType.Trending ? "[GREEN]" :
                                CurrentRegime == MarketRegimeType.Ranging ? "[RED]" : "[YELLOW]";

            string regimeEmoji = CurrentRegime == MarketRegimeType.Trending ? "+" :
                                CurrentRegime == MarketRegimeType.Ranging ? "-" : "~";

            string biasText = Bias == DirectionalBias.Long ? "LONG (+DI > -DI)" :
                             Bias == DirectionalBias.Short ? "SHORT (-DI > +DI)" : "NEUTRAL";

            return new string[]
            {
                $"{regimeColor}REGIME: {regimeEmoji} {DrawingUtilities.GetRegimeText(CurrentRegime)}",
                $"Chop: {ChoppinessIndex:F1}  ADX: {ADX:F1}",
                $"Bias: {biasText}"
            };
        }

        /// <summary>
        /// Check if there's enough data for valid calculations
        /// </summary>
        public bool IsReady => _barsCalculated >= Math.Max(_chopPeriod, _adxPeriod * 2);
    }
}
