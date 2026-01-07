// ============================================================================
// NQ Setup Scanner v1.0
// NQSetupScanner.cs - Main indicator entry point
// Multi-factor confluence indicator for NQ futures trading
// Compatible with Quantower FREE license
// ============================================================================

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using TradingPlatform.BusinessLayer;

namespace NQSetupScanner
{
    /// <summary>
    /// NQ Setup Scanner - A comprehensive trading indicator for NQ futures
    /// Features:
    /// - Market Regime Detection (Choppiness Index + ADX)
    /// - Previous Day Levels (PDH/PDL/PDC)
    /// - Session Kill Zone Levels
    /// - Opening Range
    /// - Swing Liquidity Levels (BSL/SSL)
    /// - Volume Spike Detection
    /// - Confluence Scoring
    /// </summary>
    public class NQSetupScanner : Indicator
    {
        #region Input Parameters

        // Market Regime Parameters
        [InputParameter("Choppiness Period", 10, 1, 50, 1, 0)]
        public int ChopPeriod = 14;

        [InputParameter("ADX Period", 11, 1, 50, 1, 0)]
        public int ADXPeriod = 14;

        [InputParameter("Trending Threshold (Chop below)", 12, 20, 50, 0.1, 1)]
        public double TrendingChopThreshold = 38.2;

        [InputParameter("Ranging Threshold (Chop above)", 13, 50, 80, 0.1, 1)]
        public double RangingChopThreshold = 61.8;

        // Level Parameters
        [InputParameter("Swing Lookback Bars", 20, 100, 5000, 1, 0)]
        public int SwingLookback = 1920;

        [InputParameter("Level Proximity (ticks)", 21, 1, 50, 1, 0)]
        public int LevelProximityTicks = 10;

        [InputParameter("Mitigation Ticks", 22, 5, 50, 1, 0)]
        public int MitigationTicks = 10;

        // Volume Parameters
        [InputParameter("Volume MA Period", 30, 5, 50, 1, 0)]
        public int VolumeMAPeriod = 20;

        [InputParameter("Volume Spike Multiplier", 31, 1.0, 3.0, 0.1, 1)]
        public double VolumeSpikeMultiplier = 1.5;

        // Display Options
        [InputParameter("Show PDH/PDL", 40)]
        public bool ShowPDLevels = true;

        [InputParameter("Show Session Levels", 41)]
        public bool ShowSessionLevels = true;

        [InputParameter("Show Opening Range", 42)]
        public bool ShowOpeningRange = true;

        [InputParameter("Show Swing Levels", 43)]
        public bool ShowSwingLevels = true;

        [InputParameter("Show OR Fill", 44)]
        public bool ShowORFill = true;

        [InputParameter("Show Regime Panel", 45)]
        public bool ShowRegimePanel = true;

        [InputParameter("Show Scoring Panel", 46)]
        public bool ShowScoringPanel = true;

        // Alert Options
        [InputParameter("Enable Sound Alerts", 50)]
        public bool EnableSoundAlerts = true;

        [InputParameter("Alert on High Score", 51)]
        public bool AlertOnHighScore = true;

        [InputParameter("Alert on Volume Spike at Level", 52)]
        public bool AlertOnVolumeSpikeAtLevel = true;

        [InputParameter("Alert on Liquidity Raid", 53)]
        public bool AlertOnLiquidityRaid = true;

        #endregion

        #region Private Fields

        // Component instances
        private MarketRegimeDetector _regimeDetector;
        private LevelManager _levelManager;
        private VolumeAnalyzer _volumeAnalyzer;
        private ScoringEngine _scoringEngine;

        // Drawing resources
        private Font _titleFont;
        private Font _contentFont;
        private Font _labelFont;

        // State tracking
        private int _lastCalculatedBar = -1;
        private DateTime _lastAlertTime = DateTime.MinValue;
        private bool _isInitialized = false;
        private double _tickSize = 0.25; // Default for NQ

        // Alert cooldown (prevent spam)
        private readonly TimeSpan _alertCooldown = TimeSpan.FromSeconds(30);

        #endregion

        #region Indicator Lifecycle

        /// <summary>
        /// Indicator constructor
        /// </summary>
        public NQSetupScanner()
            : base()
        {
            Name = "NQ Setup Scanner";
            Description = "Multi-factor confluence indicator for NQ futures trading";
            SeparateWindow = false;
        }

        /// <summary>
        /// Called when indicator is added to chart
        /// </summary>
        protected override void OnInit()
        {
            base.OnInit();

            // Get tick size from symbol if available
            if (Symbol != null && Symbol.TickSize > 0)
            {
                _tickSize = Symbol.TickSize;
            }

            // Initialize components
            _regimeDetector = new MarketRegimeDetector(
                ChopPeriod,
                ADXPeriod,
                TrendingChopThreshold,
                RangingChopThreshold);

            _levelManager = new LevelManager(
                SwingLookback,
                _tickSize,
                MitigationTicks);

            _volumeAnalyzer = new VolumeAnalyzer(
                VolumeMAPeriod,
                VolumeSpikeMultiplier);

            _scoringEngine = new ScoringEngine(
                LevelProximityTicks,
                _tickSize);

            // Connect components
            _scoringEngine.LevelManager = _levelManager;
            _scoringEngine.VolumeAnalyzer = _volumeAnalyzer;

            // Initialize fonts
            _titleFont = new Font("Arial", 10, FontStyle.Bold);
            _contentFont = new Font("Arial", 9, FontStyle.Regular);
            _labelFont = new Font("Arial", 8, FontStyle.Regular);

            _isInitialized = true;
        }

        /// <summary>
        /// Called when indicator is removed from chart
        /// </summary>
        protected override void OnClear()
        {
            base.OnClear();

            // Dispose fonts
            _titleFont?.Dispose();
            _contentFont?.Dispose();
            _labelFont?.Dispose();

            _isInitialized = false;
        }

        #endregion

        #region Calculation

        /// <summary>
        /// Main calculation method called for each bar
        /// </summary>
        protected override void OnUpdate(UpdateArgs args)
        {
            if (!_isInitialized) return;

            int barIndex = Count - 1;

            // Avoid recalculating the same bar unless it's the current bar updating
            if (barIndex == _lastCalculatedBar && args.Reason != UpdateReason.NewBar)
            {
                // Only update current bar data
                UpdateCurrentBar(barIndex);
                return;
            }

            // Get bar data
            double high = High(barIndex);
            double low = Low(barIndex);
            double close = Close(barIndex);
            double open = Open(barIndex);
            double volume = Volume(barIndex);
            DateTime barTime = Time(barIndex);

            // Update Market Regime
            _regimeDetector.Calculate(high, low, close);

            // Update Level Manager
            _levelManager.ProcessBar(barTime, high, low, close, barIndex);

            // Update Volume Analyzer
            bool volumeSpike = _volumeAnalyzer.ProcessBar(volume, barIndex);

            // Calculate Score
            var score = _scoringEngine.CalculateScore(close);

            // Check for alerts
            CheckAlerts(barIndex, close, volumeSpike, score);

            _lastCalculatedBar = barIndex;
        }

        /// <summary>
        /// Update only current bar data (for real-time updates)
        /// </summary>
        private void UpdateCurrentBar(int barIndex)
        {
            double close = Close(barIndex);
            double volume = Volume(barIndex);

            // Update volume (may trigger spike on current bar)
            _volumeAnalyzer.ProcessBar(volume, barIndex);

            // Recalculate score with current price
            _scoringEngine.CalculateScore(close);
        }

        #endregion

        #region Alert Handling

        /// <summary>
        /// Check for various alert conditions
        /// </summary>
        private void CheckAlerts(int barIndex, double price, bool volumeSpike, ScoringResult score)
        {
            if (!EnableSoundAlerts) return;

            // Cooldown check
            if (DateTime.Now - _lastAlertTime < _alertCooldown) return;

            // High score alert
            if (AlertOnHighScore && score.TotalScore >= 4)
            {
                TriggerAlert(_scoringEngine.GetAlertMessage());
                return;
            }

            // Volume spike at key level
            if (AlertOnVolumeSpikeAtLevel && volumeSpike)
            {
                var (label, levelPrice, distance) = _scoringEngine.FindNearestLevel(price);
                double proximityDistance = LevelProximityTicks * _tickSize;

                if (distance <= proximityDistance && !string.IsNullOrEmpty(label))
                {
                    TriggerAlert($"Volume Spike at {label} ({levelPrice:F2})");
                    return;
                }
            }

            // Liquidity raid alert
            if (AlertOnLiquidityRaid)
            {
                double high = High(barIndex);
                double low = Low(barIndex);
                double close = Close(barIndex);

                var raidInfo = _levelManager.CheckForRaid(high, low, close);
                if (raidInfo != null)
                {
                    TriggerAlert(raidInfo.Message);
                }
            }
        }

        /// <summary>
        /// Trigger an alert
        /// </summary>
        private void TriggerAlert(string message)
        {
            _lastAlertTime = DateTime.Now;

            // Play alert sound
            try
            {
                Alert.Play();
            }
            catch
            {
                // Ignore sound errors
            }

            // Log alert (Quantower will show in alert log)
            Log($"[NQ Scanner] {message}", StrategyLoggingLevel.Trading);
        }

        #endregion

        #region Chart Rendering

        /// <summary>
        /// Custom chart painting for levels and panels
        /// </summary>
        public override void OnPaintChart(PaintChartEventArgs args)
        {
            base.OnPaintChart(args);

            if (!_isInitialized) return;

            Graphics graphics = args.Graphics;
            Rectangle chartRect = args.Rectangle;

            // Set up antialiasing for smoother rendering
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // Draw price levels
            DrawPriceLevels(graphics, chartRect, args);

            // Draw Opening Range fill
            if (ShowORFill && ShowOpeningRange && _levelManager.HasOpeningRange)
            {
                DrawOpeningRangeFill(graphics, chartRect, args);
            }

            // Draw volume spike highlights
            DrawVolumeSpikeHighlights(graphics, chartRect, args);

            // Draw panels
            if (ShowRegimePanel)
            {
                DrawRegimePanel(graphics, chartRect);
            }

            if (ShowScoringPanel)
            {
                DrawScoringPanel(graphics, chartRect);
            }
        }

        /// <summary>
        /// Draw all price levels
        /// </summary>
        private void DrawPriceLevels(Graphics graphics, Rectangle chartRect, PaintChartEventArgs args)
        {
            // Draw PD levels
            if (ShowPDLevels)
            {
                foreach (var level in _levelManager.GetPDLevels())
                {
                    DrawLevel(graphics, level, chartRect, args);
                }
            }

            // Draw Session levels
            if (ShowSessionLevels)
            {
                foreach (var level in _levelManager.GetSessionLevels())
                {
                    DrawLevel(graphics, level, chartRect, args);
                }
            }

            // Draw Opening Range levels
            if (ShowOpeningRange)
            {
                foreach (var level in _levelManager.GetORLevels())
                {
                    DrawLevel(graphics, level, chartRect, args);
                }
            }

            // Draw Swing levels (BSL/SSL)
            if (ShowSwingLevels)
            {
                foreach (var level in _levelManager.GetSwingLevels())
                {
                    DrawLevel(graphics, level, chartRect, args);
                }
            }
        }

        /// <summary>
        /// Draw a single price level
        /// </summary>
        private void DrawLevel(Graphics graphics, PriceLevel level, Rectangle chartRect, PaintChartEventArgs args)
        {
            if (level == null || level.IsMitigated) return;

            // Convert price to Y coordinate
            int y = (int)GetChartY(level.Price);

            // Check if level is visible on chart
            if (y < chartRect.Top || y > chartRect.Bottom) return;

            // Draw the line
            using (Pen pen = new Pen(level.LineColor, level.LineWidth))
            {
                pen.DashStyle = level.LineStyle;
                graphics.DrawLine(pen, chartRect.Left, y, chartRect.Right, y);
            }

            // Draw the label
            if (!string.IsNullOrEmpty(level.Label))
            {
                SizeF labelSize = graphics.MeasureString(level.Label, _labelFont);
                float labelX = chartRect.Right - labelSize.Width - 5;
                float labelY = y - labelSize.Height / 2;

                // Background for readability
                using (Brush bgBrush = new SolidBrush(Color.FromArgb(180, 20, 20, 30)))
                {
                    graphics.FillRectangle(bgBrush, labelX - 3, labelY - 1,
                        labelSize.Width + 6, labelSize.Height + 2);
                }

                using (Brush textBrush = new SolidBrush(level.LineColor))
                {
                    graphics.DrawString(level.Label, _labelFont, textBrush, labelX, labelY);
                }
            }
        }

        /// <summary>
        /// Draw Opening Range fill
        /// </summary>
        private void DrawOpeningRangeFill(Graphics graphics, Rectangle chartRect, PaintChartEventArgs args)
        {
            if (_levelManager.ORHigh == null || _levelManager.ORLow == null) return;

            int yHigh = (int)GetChartY(_levelManager.ORHigh.Price);
            int yLow = (int)GetChartY(_levelManager.ORLow.Price);

            // Ensure coordinates are within chart bounds
            yHigh = Math.Max(chartRect.Top, Math.Min(chartRect.Bottom, yHigh));
            yLow = Math.Max(chartRect.Top, Math.Min(chartRect.Bottom, yLow));

            int top = Math.Min(yHigh, yLow);
            int height = Math.Abs(yLow - yHigh);

            using (Brush fillBrush = new SolidBrush(Color.FromArgb(30, DrawingUtilities.OrangeColor)))
            {
                graphics.FillRectangle(fillBrush, chartRect.Left, top, chartRect.Width, height);
            }
        }

        /// <summary>
        /// Draw volume spike bar highlights
        /// </summary>
        private void DrawVolumeSpikeHighlights(Graphics graphics, Rectangle chartRect, PaintChartEventArgs args)
        {
            // Get visible bar range
            int firstVisibleBar = (int)GetFirstVisibleBarIndex();
            int lastVisibleBar = Math.Min(Count - 1, firstVisibleBar + GetVisibleBarsCount());

            for (int i = firstVisibleBar; i <= lastVisibleBar; i++)
            {
                if (_volumeAnalyzer.WasSpikeBar(i))
                {
                    // Get bar X coordinates
                    int x = (int)GetChartX(i);
                    int barWidth = Math.Max(3, (int)(chartRect.Width / GetVisibleBarsCount()));

                    // Draw highlight
                    using (Brush brush = new SolidBrush(Color.FromArgb(40, DrawingUtilities.YellowColor)))
                    {
                        graphics.FillRectangle(brush, x - barWidth / 2, chartRect.Top,
                            barWidth, chartRect.Height);
                    }
                }
            }
        }

        /// <summary>
        /// Draw Market Regime panel
        /// </summary>
        private void DrawRegimePanel(Graphics graphics, Rectangle chartRect)
        {
            // Panel position (top-right)
            int panelWidth = 200;
            int panelHeight = 70;
            int panelX = chartRect.Right - panelWidth - 10;
            int panelY = chartRect.Top + 10;

            Rectangle panelRect = new Rectangle(panelX, panelY, panelWidth, panelHeight);

            // Get regime lines
            string[] lines = _regimeDetector.GetPanelLines();

            // Draw panel
            DrawingUtilities.DrawPanel(graphics, panelRect, "MARKET REGIME", lines,
                _titleFont, _contentFont);
        }

        /// <summary>
        /// Draw Scoring panel
        /// </summary>
        private void DrawScoringPanel(Graphics graphics, Rectangle chartRect)
        {
            // Panel position (below regime panel)
            int panelWidth = 220;
            int panelHeight = 140;
            int panelX = chartRect.Right - panelWidth - 10;
            int panelY = chartRect.Top + 90; // Below regime panel

            Rectangle panelRect = new Rectangle(panelX, panelY, panelWidth, panelHeight);

            // Get scoring lines
            string[] lines = _scoringEngine.GetPanelLines();

            // Draw panel
            DrawingUtilities.DrawPanel(graphics, panelRect, "SETUP SCANNER", lines,
                _titleFont, _contentFont);
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Get the Y coordinate for a price level
        /// </summary>
        private double GetChartY(double price)
        {
            try
            {
                return ChartInfo.GetY(price);
            }
            catch
            {
                return double.NaN;
            }
        }

        /// <summary>
        /// Get the X coordinate for a bar index
        /// </summary>
        private double GetChartX(int barIndex)
        {
            try
            {
                return ChartInfo.GetX(barIndex);
            }
            catch
            {
                return double.NaN;
            }
        }

        /// <summary>
        /// Get the first visible bar index
        /// </summary>
        private double GetFirstVisibleBarIndex()
        {
            try
            {
                return ChartInfo.FirstVisibleBarIndex;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Get the number of visible bars
        /// </summary>
        private int GetVisibleBarsCount()
        {
            try
            {
                return ChartInfo.VisibleBarsCount;
            }
            catch
            {
                return 100;
            }
        }

        /// <summary>
        /// Access High price for a bar
        /// </summary>
        private double High(int index)
        {
            return GetPrice(PriceType.High, index);
        }

        /// <summary>
        /// Access Low price for a bar
        /// </summary>
        private double Low(int index)
        {
            return GetPrice(PriceType.Low, index);
        }

        /// <summary>
        /// Access Close price for a bar
        /// </summary>
        private double Close(int index)
        {
            return GetPrice(PriceType.Close, index);
        }

        /// <summary>
        /// Access Open price for a bar
        /// </summary>
        private double Open(int index)
        {
            return GetPrice(PriceType.Open, index);
        }

        /// <summary>
        /// Access Volume for a bar
        /// </summary>
        private double Volume(int index)
        {
            return GetPrice(PriceType.Volume, index);
        }

        /// <summary>
        /// Access Time for a bar
        /// </summary>
        private DateTime Time(int index)
        {
            try
            {
                return GetTimeUtc(index);
            }
            catch
            {
                return DateTime.UtcNow;
            }
        }

        #endregion
    }
}
