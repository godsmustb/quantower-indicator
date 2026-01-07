// ============================================================================
// NQ Setup Scanner v1.0
// VolumeAnalysis.cs - Volume spike detection using basic volume data
// Compatible with Quantower FREE license
// ============================================================================

using System;
using System.Collections.Generic;

namespace NQSetupScanner
{
    /// <summary>
    /// Volume spike detection using total bar volume
    /// Uses simple moving average and multiplier threshold
    /// </summary>
    public class VolumeAnalyzer
    {
        // Configuration
        private readonly int _maPeriod;
        private readonly double _spikeMultiplier;

        // Volume history buffer
        private readonly Queue<double> _volumeBuffer;

        // Current state
        public double CurrentVolume { get; private set; }
        public double VolumeMA { get; private set; }
        public bool IsSpike { get; private set; }
        public double SpikeRatio { get; private set; }

        // Spike tracking
        private readonly List<int> _spikeBarIndices;
        private int _lastSpikeBar;

        /// <summary>
        /// Initialize the Volume Analyzer
        /// </summary>
        /// <param name="maPeriod">Moving average period (default 20)</param>
        /// <param name="spikeMultiplier">Multiplier for spike detection (default 1.5)</param>
        public VolumeAnalyzer(int maPeriod = 20, double spikeMultiplier = 1.5)
        {
            _maPeriod = maPeriod;
            _spikeMultiplier = spikeMultiplier;

            _volumeBuffer = new Queue<double>();
            _spikeBarIndices = new List<int>();

            CurrentVolume = 0;
            VolumeMA = 0;
            IsSpike = false;
            SpikeRatio = 0;
            _lastSpikeBar = -1;
        }

        /// <summary>
        /// Reset the analyzer
        /// </summary>
        public void Reset()
        {
            _volumeBuffer.Clear();
            _spikeBarIndices.Clear();
            CurrentVolume = 0;
            VolumeMA = 0;
            IsSpike = false;
            SpikeRatio = 0;
            _lastSpikeBar = -1;
        }

        /// <summary>
        /// Process a new bar's volume
        /// </summary>
        /// <param name="volume">Bar volume</param>
        /// <param name="barIndex">Current bar index</param>
        /// <returns>True if spike detected, false otherwise</returns>
        public bool ProcessBar(double volume, int barIndex)
        {
            CurrentVolume = volume;

            // Add to buffer
            _volumeBuffer.Enqueue(volume);

            // Maintain buffer size
            while (_volumeBuffer.Count > _maPeriod)
            {
                _volumeBuffer.Dequeue();
            }

            // Calculate moving average
            if (_volumeBuffer.Count >= _maPeriod)
            {
                double sum = 0;
                foreach (double v in _volumeBuffer)
                {
                    sum += v;
                }
                VolumeMA = sum / _maPeriod;
            }
            else if (_volumeBuffer.Count > 0)
            {
                // Use available data for partial MA
                double sum = 0;
                foreach (double v in _volumeBuffer)
                {
                    sum += v;
                }
                VolumeMA = sum / _volumeBuffer.Count;
            }

            // Check for spike
            IsSpike = false;
            SpikeRatio = 0;

            if (VolumeMA > 0)
            {
                SpikeRatio = CurrentVolume / VolumeMA;

                if (SpikeRatio >= _spikeMultiplier)
                {
                    IsSpike = true;
                    _lastSpikeBar = barIndex;

                    // Track spike bars (limit to last 100)
                    _spikeBarIndices.Add(barIndex);
                    while (_spikeBarIndices.Count > 100)
                    {
                        _spikeBarIndices.RemoveAt(0);
                    }
                }
            }

            return IsSpike;
        }

        /// <summary>
        /// Check if a specific bar had a volume spike
        /// </summary>
        /// <param name="barIndex">Bar index to check</param>
        /// <returns>True if bar had a spike</returns>
        public bool WasSpikeBar(int barIndex)
        {
            return _spikeBarIndices.Contains(barIndex);
        }

        /// <summary>
        /// Get the last bar index that had a spike
        /// </summary>
        public int LastSpikeBar => _lastSpikeBar;

        /// <summary>
        /// Check if enough data for valid calculations
        /// </summary>
        public bool IsReady => _volumeBuffer.Count >= _maPeriod;

        /// <summary>
        /// Get spike information string for display
        /// </summary>
        public string GetSpikeInfo()
        {
            if (IsSpike)
            {
                return $"VOLUME SPIKE! {SpikeRatio:F1}x average";
            }
            else if (SpikeRatio > 1.0)
            {
                return $"Vol: {SpikeRatio:F1}x avg";
            }
            else
            {
                return $"Vol: {SpikeRatio:F1}x avg";
            }
        }

        /// <summary>
        /// Get a list of recent spike bar indices
        /// </summary>
        public List<int> GetRecentSpikes(int count = 10)
        {
            int startIndex = Math.Max(0, _spikeBarIndices.Count - count);
            return _spikeBarIndices.GetRange(startIndex, _spikeBarIndices.Count - startIndex);
        }
    }

    /// <summary>
    /// Volume spike event data
    /// </summary>
    public class VolumeSpikeEvent
    {
        public int BarIndex { get; set; }
        public DateTime BarTime { get; set; }
        public double Volume { get; set; }
        public double VolumeMA { get; set; }
        public double SpikeRatio { get; set; }
        public bool AtKeyLevel { get; set; }
        public string NearestLevel { get; set; }

        public VolumeSpikeEvent(int barIndex, DateTime barTime, double volume, double volumeMA)
        {
            BarIndex = barIndex;
            BarTime = barTime;
            Volume = volume;
            VolumeMA = volumeMA;
            SpikeRatio = volumeMA > 0 ? volume / volumeMA : 0;
            AtKeyLevel = false;
            NearestLevel = "";
        }

        public string GetAlertMessage()
        {
            string message = $"Volume Spike: {SpikeRatio:F1}x average";
            if (AtKeyLevel && !string.IsNullOrEmpty(NearestLevel))
            {
                message += $" at {NearestLevel}";
            }
            return message;
        }
    }
}
