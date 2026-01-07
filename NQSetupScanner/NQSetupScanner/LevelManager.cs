// ============================================================================
// NQ Setup Scanner v1.0
// LevelManager.cs - Manages all price levels (PD, Sessions, OR, Swings)
// ============================================================================

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace NQSetupScanner
{
    /// <summary>
    /// Manages all horizontal price levels for the indicator
    /// Includes Previous Day, Session Kill Zones, Opening Range, and Swing Liquidity
    /// </summary>
    public class LevelManager
    {
        // Configuration
        private readonly int _swingLookback;
        private readonly int _mitigationTicks;
        private readonly double _tickSize;

        // Previous Day Levels
        public PriceLevel PDH { get; private set; }
        public PriceLevel PDL { get; private set; }
        public PriceLevel PDC { get; private set; }

        // Session tracking
        private readonly Dictionary<SessionType, double> _sessionHighs;
        private readonly Dictionary<SessionType, double> _sessionLows;
        private readonly Dictionary<SessionType, PriceLevel> _sessionHighLevels;
        private readonly Dictionary<SessionType, PriceLevel> _sessionLowLevels;

        // Opening Range
        public PriceLevel ORHigh { get; private set; }
        public PriceLevel ORLow { get; private set; }
        private bool _orCalculated;
        private double _orHighPrice;
        private double _orLowPrice;

        // Swing Levels
        public List<SwingPoint> SwingHighs { get; private set; }
        public List<SwingPoint> SwingLows { get; private set; }
        public List<PriceLevel> BSLLevels { get; private set; }
        public List<PriceLevel> SSLLevels { get; private set; }

        // Historical data for swing detection
        private readonly List<double> _highHistory;
        private readonly List<double> _lowHistory;
        private readonly List<DateTime> _timeHistory;

        // State tracking
        private DateTime _lastBarTime;
        private SessionType _currentSession;
        private DateTime _lastSessionReset;
        private bool _pdLevelsSet;
        private double _sessionHigh;
        private double _sessionLow;
        private double _sessionClose;

        /// <summary>
        /// Initialize the Level Manager
        /// </summary>
        /// <param name="swingLookback">Number of bars to look back for swing detection (default 1920)</param>
        /// <param name="tickSize">Tick size for the instrument (e.g., 0.25 for NQ)</param>
        /// <param name="mitigationTicks">Ticks through level to mark as mitigated (default 10)</param>
        public LevelManager(int swingLookback = 1920, double tickSize = 0.25, int mitigationTicks = 10)
        {
            _swingLookback = swingLookback;
            _tickSize = tickSize;
            _mitigationTicks = mitigationTicks;

            _sessionHighs = new Dictionary<SessionType, double>();
            _sessionLows = new Dictionary<SessionType, double>();
            _sessionHighLevels = new Dictionary<SessionType, PriceLevel>();
            _sessionLowLevels = new Dictionary<SessionType, PriceLevel>();

            SwingHighs = new List<SwingPoint>();
            SwingLows = new List<SwingPoint>();
            BSLLevels = new List<PriceLevel>();
            SSLLevels = new List<PriceLevel>();

            _highHistory = new List<double>();
            _lowHistory = new List<double>();
            _timeHistory = new List<DateTime>();

            _currentSession = SessionType.None;
            _pdLevelsSet = false;
            _orCalculated = false;

            InitializeSessionTracking();
        }

        /// <summary>
        /// Initialize session tracking dictionaries
        /// </summary>
        private void InitializeSessionTracking()
        {
            foreach (SessionType session in Enum.GetValues(typeof(SessionType)))
            {
                if (session != SessionType.None)
                {
                    _sessionHighs[session] = double.MinValue;
                    _sessionLows[session] = double.MaxValue;
                }
            }
        }

        /// <summary>
        /// Reset all levels for a new session
        /// </summary>
        public void Reset()
        {
            PDH = null;
            PDL = null;
            PDC = null;
            ORHigh = null;
            ORLow = null;
            _orCalculated = false;
            _pdLevelsSet = false;
            _sessionHigh = double.MinValue;
            _sessionLow = double.MaxValue;

            InitializeSessionTracking();
            _sessionHighLevels.Clear();
            _sessionLowLevels.Clear();

            // Keep swing history but recalculate levels
            RecalculateSwingLevels();
        }

        /// <summary>
        /// Process a new bar and update all levels
        /// </summary>
        /// <param name="barTime">Bar timestamp (UTC)</param>
        /// <param name="high">Bar high</param>
        /// <param name="low">Bar low</param>
        /// <param name="close">Bar close</param>
        /// <param name="barIndex">Current bar index</param>
        public void ProcessBar(DateTime barTime, double high, double low, double close, int barIndex)
        {
            // Check for new trading day (session reset)
            if (_lastBarTime != DateTime.MinValue && TimeZoneUtilities.IsNewTradingDay(barTime, _lastBarTime))
            {
                OnNewTradingDay();
            }

            // Update historical data for swing detection
            UpdateHistory(high, low, barTime);

            // Update session high/low tracking
            UpdateSessionTracking(high, low, close);

            // Detect current session
            SessionType newSession = TimeZoneUtilities.GetCurrentSession(barTime);
            if (newSession != _currentSession && newSession != SessionType.None)
            {
                OnSessionChange(newSession);
            }

            // Update current session high/low
            UpdateCurrentSessionLevels(newSession, high, low);

            // Update Opening Range
            UpdateOpeningRange(barTime, high, low);

            // Detect swings
            DetectSwings(barIndex);

            // Check for level mitigation (raids)
            CheckLevelMitigation(high, low, close);

            _lastBarTime = barTime;
            _currentSession = newSession;
        }

        /// <summary>
        /// Handle new trading day - set previous day levels
        /// </summary>
        private void OnNewTradingDay()
        {
            // Store previous day levels
            if (_sessionHigh > double.MinValue && _sessionLow < double.MaxValue)
            {
                PDH = new PriceLevel(_sessionHigh, "PDH", DrawingUtilities.GoldColor, DashStyle.Solid, 2)
                {
                    Type = LevelType.PDH
                };

                PDL = new PriceLevel(_sessionLow, "PDL", DrawingUtilities.GoldColor, DashStyle.Solid, 2)
                {
                    Type = LevelType.PDL
                };

                PDC = new PriceLevel(_sessionClose, "PDC", DrawingUtilities.WhiteColor, DashStyle.Dash, 1)
                {
                    Type = LevelType.PDC
                };

                _pdLevelsSet = true;
            }

            // Reset session tracking
            _sessionHigh = double.MinValue;
            _sessionLow = double.MaxValue;
            _orCalculated = false;
            ORHigh = null;
            ORLow = null;
            _orHighPrice = double.MinValue;
            _orLowPrice = double.MaxValue;

            // Reset session levels for the new day
            foreach (SessionType session in Enum.GetValues(typeof(SessionType)))
            {
                if (session != SessionType.None)
                {
                    _sessionHighs[session] = double.MinValue;
                    _sessionLows[session] = double.MaxValue;
                }
            }
            _sessionHighLevels.Clear();
            _sessionLowLevels.Clear();

            _lastSessionReset = DateTime.UtcNow;
        }

        /// <summary>
        /// Update session high/low/close tracking
        /// </summary>
        private void UpdateSessionTracking(double high, double low, double close)
        {
            if (high > _sessionHigh)
                _sessionHigh = high;
            if (low < _sessionLow)
                _sessionLow = low;
            _sessionClose = close;
        }

        /// <summary>
        /// Handle session change
        /// </summary>
        private void OnSessionChange(SessionType newSession)
        {
            // Reset high/low for new session type
            _sessionHighs[newSession] = double.MinValue;
            _sessionLows[newSession] = double.MaxValue;
        }

        /// <summary>
        /// Update current session high/low levels
        /// </summary>
        private void UpdateCurrentSessionLevels(SessionType session, double high, double low)
        {
            if (session == SessionType.None) return;

            bool updated = false;

            if (high > _sessionHighs[session])
            {
                _sessionHighs[session] = high;
                updated = true;
            }

            if (low < _sessionLows[session])
            {
                _sessionLows[session] = low;
                updated = true;
            }

            if (updated)
            {
                string prefix = DrawingUtilities.GetSessionPrefix(session);
                Color sessionColor = DrawingUtilities.GetSessionColor(session);

                // Update or create session high level
                if (_sessionHighs[session] > double.MinValue)
                {
                    _sessionHighLevels[session] = new PriceLevel(
                        _sessionHighs[session],
                        $"{prefix} H",
                        sessionColor,
                        DashStyle.Dot,
                        1)
                    {
                        Type = LevelType.SessionHigh
                    };
                }

                // Update or create session low level
                if (_sessionLows[session] < double.MaxValue)
                {
                    _sessionLowLevels[session] = new PriceLevel(
                        _sessionLows[session],
                        $"{prefix} L",
                        sessionColor,
                        DashStyle.Dot,
                        1)
                    {
                        Type = LevelType.SessionLow
                    };
                }
            }
        }

        /// <summary>
        /// Update Opening Range levels
        /// </summary>
        private void UpdateOpeningRange(DateTime barTime, double high, double low)
        {
            if (_orCalculated) return;

            if (TimeZoneUtilities.IsOpeningRangePeriod(barTime))
            {
                // During OR period, track high and low
                if (high > _orHighPrice)
                    _orHighPrice = high;
                if (low < _orLowPrice)
                    _orLowPrice = low;
            }
            else if (_orHighPrice > double.MinValue && _orLowPrice < double.MaxValue)
            {
                // OR period has ended, create levels
                DateTime etTime = TimeZoneUtilities.ToEasternTime(barTime);
                TimeSpan currentTime = etTime.TimeOfDay;

                // Only set OR if we're past 10:00 AM ET
                if (currentTime >= new TimeSpan(10, 0, 0))
                {
                    ORHigh = new PriceLevel(_orHighPrice, "OR-H", DrawingUtilities.OrangeColor, DashStyle.Solid, 2)
                    {
                        Type = LevelType.ORHigh
                    };

                    ORLow = new PriceLevel(_orLowPrice, "OR-L", DrawingUtilities.OrangeColor, DashStyle.Solid, 2)
                    {
                        Type = LevelType.ORLow
                    };

                    _orCalculated = true;
                }
            }
        }

        /// <summary>
        /// Update price history for swing detection
        /// </summary>
        private void UpdateHistory(double high, double low, DateTime time)
        {
            _highHistory.Add(high);
            _lowHistory.Add(low);
            _timeHistory.Add(time);

            // Trim history to lookback period
            while (_highHistory.Count > _swingLookback)
            {
                _highHistory.RemoveAt(0);
                _lowHistory.RemoveAt(0);
                _timeHistory.RemoveAt(0);
            }
        }

        /// <summary>
        /// Detect swing highs and lows using 3-bar pivot pattern
        /// </summary>
        private void DetectSwings(int currentBarIndex)
        {
            // Need at least 3 bars for pivot detection
            if (_highHistory.Count < 3) return;

            int pivotIndex = _highHistory.Count - 2; // Middle bar of the 3-bar pattern

            double leftHigh = _highHistory[pivotIndex - 1];
            double pivotHigh = _highHistory[pivotIndex];
            double rightHigh = _highHistory[pivotIndex + 1];

            double leftLow = _lowHistory[pivotIndex - 1];
            double pivotLow = _lowHistory[pivotIndex];
            double rightLow = _lowHistory[pivotIndex + 1];

            DateTime pivotTime = _timeHistory[pivotIndex];
            int pivotBarIndex = currentBarIndex - 1;

            // Check for swing high: High > High[1] AND High > High[-1]
            if (pivotHigh > leftHigh && pivotHigh > rightHigh)
            {
                // Check if this swing already exists (within 1 tick)
                bool exists = false;
                foreach (var swing in SwingHighs)
                {
                    if (Math.Abs(swing.Price - pivotHigh) < _tickSize * 2)
                    {
                        exists = true;
                        swing.TouchCount++;
                        break;
                    }
                }

                if (!exists)
                {
                    SwingHighs.Add(new SwingPoint(pivotHigh, pivotBarIndex, pivotTime, true));
                }
            }

            // Check for swing low: Low < Low[1] AND Low < Low[-1]
            if (pivotLow < leftLow && pivotLow < rightLow)
            {
                // Check if this swing already exists (within 1 tick)
                bool exists = false;
                foreach (var swing in SwingLows)
                {
                    if (Math.Abs(swing.Price - pivotLow) < _tickSize * 2)
                    {
                        exists = true;
                        swing.TouchCount++;
                        break;
                    }
                }

                if (!exists)
                {
                    SwingLows.Add(new SwingPoint(pivotLow, pivotBarIndex, pivotTime, false));
                }
            }

            // Recalculate swing levels
            RecalculateSwingLevels();
        }

        /// <summary>
        /// Recalculate BSL/SSL levels from detected swings
        /// </summary>
        private void RecalculateSwingLevels()
        {
            BSLLevels.Clear();
            SSLLevels.Clear();

            // Create BSL levels from significant swing highs
            foreach (var swing in SwingHighs)
            {
                if (!swing.IsMitigated && (swing.TouchCount >= 2 || IsSignificantSwing(swing)))
                {
                    BSLLevels.Add(new PriceLevel(
                        swing.Price,
                        $"BSL {swing.Price:F2}",
                        DrawingUtilities.BlueColor,
                        DashStyle.Dash,
                        1)
                    {
                        Type = LevelType.SwingHigh
                    });
                }
            }

            // Create SSL levels from significant swing lows
            foreach (var swing in SwingLows)
            {
                if (!swing.IsMitigated && (swing.TouchCount >= 2 || IsSignificantSwing(swing)))
                {
                    SSLLevels.Add(new PriceLevel(
                        swing.Price,
                        $"SSL {swing.Price:F2}",
                        DrawingUtilities.RedColor,
                        DashStyle.Dash,
                        1)
                    {
                        Type = LevelType.SwingLow
                    });
                }
            }
        }

        /// <summary>
        /// Check if a swing is significant (major pivot)
        /// </summary>
        private bool IsSignificantSwing(SwingPoint swing)
        {
            // A swing is significant if it's within the top/bottom 20% of the lookback range
            if (_highHistory.Count < 50) return true;

            double highestHigh = double.MinValue;
            double lowestLow = double.MaxValue;

            foreach (double h in _highHistory)
            {
                if (h > highestHigh) highestHigh = h;
            }
            foreach (double l in _lowHistory)
            {
                if (l < lowestLow) lowestLow = l;
            }

            double range = highestHigh - lowestLow;
            if (range <= 0) return true;

            if (swing.IsHigh)
            {
                // Significant if in top 20% of range
                return swing.Price >= highestHigh - (range * 0.2);
            }
            else
            {
                // Significant if in bottom 20% of range
                return swing.Price <= lowestLow + (range * 0.2);
            }
        }

        /// <summary>
        /// Check for level mitigation (price trading through level)
        /// </summary>
        private void CheckLevelMitigation(double high, double low, double close)
        {
            double mitigationDistance = _mitigationTicks * _tickSize;

            // Check swing highs for mitigation
            foreach (var swing in SwingHighs)
            {
                if (!swing.IsMitigated)
                {
                    // Price traded through by mitigation distance
                    if (high > swing.Price + mitigationDistance)
                    {
                        swing.IsMitigated = true;
                    }
                }
            }

            // Check swing lows for mitigation
            foreach (var swing in SwingLows)
            {
                if (!swing.IsMitigated)
                {
                    // Price traded through by mitigation distance
                    if (low < swing.Price - mitigationDistance)
                    {
                        swing.IsMitigated = true;
                    }
                }
            }

            // Recalculate levels to remove mitigated ones
            RecalculateSwingLevels();
        }

        /// <summary>
        /// Check for liquidity raid (sweep and return)
        /// </summary>
        /// <param name="high">Bar high</param>
        /// <param name="low">Bar low</param>
        /// <param name="close">Bar close</param>
        /// <returns>Raid information if detected, null otherwise</returns>
        public LiquidityRaidInfo CheckForRaid(double high, double low, double close)
        {
            double raidThreshold = 2 * _tickSize;

            // Check BSL levels for sweep
            foreach (var level in BSLLevels)
            {
                // Price pierced level by at least 2 ticks
                if (high > level.Price + raidThreshold)
                {
                    // But closed back below (wick rejection)
                    if (close < level.Price)
                    {
                        return new LiquidityRaidInfo
                        {
                            LevelType = "BSL",
                            Price = level.Price,
                            IsBullish = false,
                            Message = $"BSL SWEPT @ {level.Price:F2} - Watch for reversal"
                        };
                    }
                }
            }

            // Check SSL levels for sweep
            foreach (var level in SSLLevels)
            {
                // Price pierced level by at least 2 ticks
                if (low < level.Price - raidThreshold)
                {
                    // But closed back above (wick rejection)
                    if (close > level.Price)
                    {
                        return new LiquidityRaidInfo
                        {
                            LevelType = "SSL",
                            Price = level.Price,
                            IsBullish = true,
                            Message = $"SSL SWEPT @ {level.Price:F2} - Watch for reversal"
                        };
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Get all active price levels for drawing
        /// </summary>
        public List<PriceLevel> GetAllLevels()
        {
            var levels = new List<PriceLevel>();

            // Previous day levels
            if (PDH != null && !PDH.IsMitigated) levels.Add(PDH);
            if (PDL != null && !PDL.IsMitigated) levels.Add(PDL);
            if (PDC != null && !PDC.IsMitigated) levels.Add(PDC);

            // Session levels
            foreach (var kvp in _sessionHighLevels)
            {
                if (kvp.Value != null && !kvp.Value.IsMitigated)
                    levels.Add(kvp.Value);
            }
            foreach (var kvp in _sessionLowLevels)
            {
                if (kvp.Value != null && !kvp.Value.IsMitigated)
                    levels.Add(kvp.Value);
            }

            // Opening Range
            if (ORHigh != null && !ORHigh.IsMitigated) levels.Add(ORHigh);
            if (ORLow != null && !ORLow.IsMitigated) levels.Add(ORLow);

            // Swing levels
            levels.AddRange(BSLLevels);
            levels.AddRange(SSLLevels);

            return levels;
        }

        /// <summary>
        /// Get just the Previous Day levels
        /// </summary>
        public List<PriceLevel> GetPDLevels()
        {
            var levels = new List<PriceLevel>();
            if (PDH != null) levels.Add(PDH);
            if (PDL != null) levels.Add(PDL);
            if (PDC != null) levels.Add(PDC);
            return levels;
        }

        /// <summary>
        /// Get just the Session levels
        /// </summary>
        public List<PriceLevel> GetSessionLevels()
        {
            var levels = new List<PriceLevel>();
            foreach (var kvp in _sessionHighLevels)
            {
                if (kvp.Value != null)
                    levels.Add(kvp.Value);
            }
            foreach (var kvp in _sessionLowLevels)
            {
                if (kvp.Value != null)
                    levels.Add(kvp.Value);
            }
            return levels;
        }

        /// <summary>
        /// Get just the Opening Range levels
        /// </summary>
        public List<PriceLevel> GetORLevels()
        {
            var levels = new List<PriceLevel>();
            if (ORHigh != null) levels.Add(ORHigh);
            if (ORLow != null) levels.Add(ORLow);
            return levels;
        }

        /// <summary>
        /// Get just the Swing Liquidity levels
        /// </summary>
        public List<PriceLevel> GetSwingLevels()
        {
            var levels = new List<PriceLevel>();
            levels.AddRange(BSLLevels);
            levels.AddRange(SSLLevels);
            return levels;
        }

        /// <summary>
        /// Check if Previous Day levels are set
        /// </summary>
        public bool HasPDLevels => _pdLevelsSet;

        /// <summary>
        /// Check if Opening Range is calculated
        /// </summary>
        public bool HasOpeningRange => _orCalculated;

        /// <summary>
        /// Get current session type
        /// </summary>
        public SessionType CurrentSession => _currentSession;
    }

    /// <summary>
    /// Information about a liquidity raid event
    /// </summary>
    public class LiquidityRaidInfo
    {
        public string LevelType { get; set; }
        public double Price { get; set; }
        public bool IsBullish { get; set; }
        public string Message { get; set; }
    }
}
