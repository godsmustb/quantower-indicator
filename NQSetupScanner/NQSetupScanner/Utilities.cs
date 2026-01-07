// ============================================================================
// NQ Setup Scanner v1.0
// Utilities.cs - Time zone helpers and drawing utilities
// ============================================================================

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using TradingPlatform.BusinessLayer;

namespace NQSetupScanner
{
    /// <summary>
    /// Session type enumeration for kill zone tracking
    /// </summary>
    public enum SessionType
    {
        Asia,
        London,
        NYAM,
        NYPM,
        None
    }

    /// <summary>
    /// Market regime enumeration
    /// </summary>
    public enum MarketRegimeType
    {
        Trending,
        Ranging,
        Transition
    }

    /// <summary>
    /// Directional bias enumeration
    /// </summary>
    public enum DirectionalBias
    {
        Long,
        Short,
        Neutral
    }

    /// <summary>
    /// Represents a price level with metadata
    /// </summary>
    public class PriceLevel
    {
        public double Price { get; set; }
        public string Label { get; set; }
        public Color LineColor { get; set; }
        public DashStyle LineStyle { get; set; }
        public int LineWidth { get; set; }
        public DateTime CreatedTime { get; set; }
        public bool IsMitigated { get; set; }
        public LevelType Type { get; set; }

        public PriceLevel(double price, string label, Color color, DashStyle style = DashStyle.Solid, int width = 2)
        {
            Price = price;
            Label = label;
            LineColor = color;
            LineStyle = style;
            LineWidth = width;
            CreatedTime = DateTime.UtcNow;
            IsMitigated = false;
        }
    }

    /// <summary>
    /// Level type for scoring categorization
    /// </summary>
    public enum LevelType
    {
        PDH,
        PDL,
        PDC,
        SessionHigh,
        SessionLow,
        ORHigh,
        ORLow,
        SwingHigh,
        SwingLow
    }

    /// <summary>
    /// Session time range definition
    /// </summary>
    public class SessionTimeRange
    {
        public SessionType Type { get; }
        public TimeSpan StartTime { get; }
        public TimeSpan EndTime { get; }
        public bool SpansMidnight { get; }
        public Color SessionColor { get; }

        public SessionTimeRange(SessionType type, TimeSpan start, TimeSpan end, Color color)
        {
            Type = type;
            StartTime = start;
            EndTime = end;
            SpansMidnight = end < start;
            SessionColor = color;
        }
    }

    /// <summary>
    /// Swing point data structure
    /// </summary>
    public class SwingPoint
    {
        public double Price { get; set; }
        public int BarIndex { get; set; }
        public DateTime Time { get; set; }
        public bool IsHigh { get; set; }
        public bool IsMitigated { get; set; }
        public int TouchCount { get; set; }

        public SwingPoint(double price, int barIndex, DateTime time, bool isHigh)
        {
            Price = price;
            BarIndex = barIndex;
            Time = time;
            IsHigh = isHigh;
            IsMitigated = false;
            TouchCount = 1;
        }
    }

    /// <summary>
    /// Scoring result for confluence analysis
    /// </summary>
    public class ScoringResult
    {
        public int TotalScore { get; set; }
        public bool AtPDLevel { get; set; }
        public bool AtSessionLevel { get; set; }
        public bool AtSwingLevel { get; set; }
        public bool AtORLevel { get; set; }
        public bool HasVolumeSpike { get; set; }
        public string Grade { get; set; }
        public string NearestLevel { get; set; }
        public double NearestLevelPrice { get; set; }

        public ScoringResult()
        {
            TotalScore = 0;
            Grade = "";
            NearestLevel = "";
        }
    }

    /// <summary>
    /// Static utility methods for time zone handling and drawing
    /// </summary>
    public static class TimeZoneUtilities
    {
        // Eastern Time Zone info
        private static readonly TimeZoneInfo EasternTimeZone;

        static TimeZoneUtilities()
        {
            try
            {
                // Try Windows time zone ID first
                EasternTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
            }
            catch
            {
                try
                {
                    // Try IANA time zone ID (Linux/Mac)
                    EasternTimeZone = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
                }
                catch
                {
                    // Fallback: Create a custom time zone (EST = UTC-5, no DST handling)
                    EasternTimeZone = TimeZoneInfo.CreateCustomTimeZone(
                        "EST",
                        TimeSpan.FromHours(-5),
                        "Eastern Standard Time",
                        "Eastern Standard Time");
                }
            }
        }

        /// <summary>
        /// Convert UTC time to Eastern Time
        /// </summary>
        public static DateTime ToEasternTime(DateTime utcTime)
        {
            if (utcTime.Kind == DateTimeKind.Unspecified)
                utcTime = DateTime.SpecifyKind(utcTime, DateTimeKind.Utc);

            return TimeZoneInfo.ConvertTimeFromUtc(utcTime, EasternTimeZone);
        }

        /// <summary>
        /// Convert Eastern Time to UTC
        /// </summary>
        public static DateTime ToUtcFromEastern(DateTime easternTime)
        {
            if (easternTime.Kind == DateTimeKind.Unspecified)
                easternTime = DateTime.SpecifyKind(easternTime, DateTimeKind.Unspecified);

            return TimeZoneInfo.ConvertTimeToUtc(easternTime, EasternTimeZone);
        }

        /// <summary>
        /// Get the current session type based on Eastern Time
        /// </summary>
        public static SessionType GetCurrentSession(DateTime utcTime)
        {
            DateTime etTime = ToEasternTime(utcTime);
            TimeSpan currentTime = etTime.TimeOfDay;

            // Asia: 6:00 PM - 12:00 AM ET
            if (currentTime >= new TimeSpan(18, 0, 0) || currentTime < new TimeSpan(0, 0, 0))
                return SessionType.Asia;

            // London: 2:00 AM - 5:00 AM ET
            if (currentTime >= new TimeSpan(2, 0, 0) && currentTime < new TimeSpan(5, 0, 0))
                return SessionType.London;

            // NY AM: 9:30 AM - 12:00 PM ET
            if (currentTime >= new TimeSpan(9, 30, 0) && currentTime < new TimeSpan(12, 0, 0))
                return SessionType.NYAM;

            // NY PM: 1:30 PM - 4:00 PM ET
            if (currentTime >= new TimeSpan(13, 30, 0) && currentTime < new TimeSpan(16, 0, 0))
                return SessionType.NYPM;

            return SessionType.None;
        }

        /// <summary>
        /// Check if we're in the Opening Range period (9:30 AM - 10:00 AM ET)
        /// </summary>
        public static bool IsOpeningRangePeriod(DateTime utcTime)
        {
            DateTime etTime = ToEasternTime(utcTime);
            TimeSpan currentTime = etTime.TimeOfDay;

            return currentTime >= new TimeSpan(9, 30, 0) && currentTime < new TimeSpan(10, 0, 0);
        }

        /// <summary>
        /// Check if it's a new trading day (session reset at 5:00 PM ET, or 6:00 PM on Sunday)
        /// </summary>
        public static bool IsNewTradingDay(DateTime currentUtc, DateTime previousUtc)
        {
            DateTime currentET = ToEasternTime(currentUtc);
            DateTime previousET = ToEasternTime(previousUtc);

            // Session reset time is 5:00 PM ET (17:00)
            TimeSpan resetTime = new TimeSpan(17, 0, 0);

            // Check if we crossed the 5:00 PM boundary
            if (currentET.Date == previousET.Date)
            {
                // Same calendar day - check if we crossed 5 PM
                return previousET.TimeOfDay < resetTime && currentET.TimeOfDay >= resetTime;
            }
            else
            {
                // Different calendar day - new session started
                return true;
            }
        }

        /// <summary>
        /// Check if the current time is within a specific session
        /// </summary>
        public static bool IsInSession(DateTime utcTime, SessionTimeRange session)
        {
            DateTime etTime = ToEasternTime(utcTime);
            TimeSpan currentTime = etTime.TimeOfDay;

            if (session.SpansMidnight)
            {
                // Session spans midnight (e.g., Asia: 18:00 - 00:00)
                return currentTime >= session.StartTime || currentTime < session.EndTime;
            }
            else
            {
                return currentTime >= session.StartTime && currentTime < session.EndTime;
            }
        }

        /// <summary>
        /// Get the start of the current trading session day
        /// </summary>
        public static DateTime GetSessionStartTime(DateTime utcTime)
        {
            DateTime etTime = ToEasternTime(utcTime);
            TimeSpan currentTime = etTime.TimeOfDay;
            TimeSpan sessionStart = new TimeSpan(18, 0, 0); // 6:00 PM ET

            DateTime sessionStartET;
            if (currentTime >= sessionStart)
            {
                // Session started today at 6 PM
                sessionStartET = etTime.Date.Add(sessionStart);
            }
            else
            {
                // Session started yesterday at 6 PM
                sessionStartET = etTime.Date.AddDays(-1).Add(sessionStart);
            }

            return ToUtcFromEastern(sessionStartET);
        }
    }

    /// <summary>
    /// Drawing utilities for chart rendering
    /// </summary>
    public static class DrawingUtilities
    {
        // Pre-defined colors
        public static readonly Color GoldColor = Color.FromArgb(255, 215, 0);           // PDH/PDL
        public static readonly Color WhiteColor = Color.White;                            // PDC
        public static readonly Color OrangeColor = Color.FromArgb(255, 165, 0);          // Opening Range
        public static readonly Color PurpleColor = Color.FromArgb(128, 0, 128);          // Asia session
        public static readonly Color BlueColor = Color.FromArgb(0, 128, 255);            // London session / BSL
        public static readonly Color GreenColor = Color.FromArgb(0, 200, 0);             // NY sessions
        public static readonly Color RedColor = Color.FromArgb(255, 50, 50);             // SSL
        public static readonly Color YellowColor = Color.FromArgb(255, 255, 0);          // Volume spike

        // Regime colors
        public static readonly Color TrendingColor = Color.FromArgb(0, 255, 100);        // Green
        public static readonly Color RangingColor = Color.FromArgb(255, 100, 100);       // Red
        public static readonly Color TransitionColor = Color.FromArgb(255, 255, 100);    // Yellow

        // Panel colors
        public static readonly Color PanelBackground = Color.FromArgb(200, 30, 30, 40);
        public static readonly Color PanelBorder = Color.FromArgb(255, 60, 60, 80);
        public static readonly Color TextColorPrimary = Color.White;
        public static readonly Color TextColorSecondary = Color.FromArgb(180, 180, 180);

        /// <summary>
        /// Draw a horizontal price level line on the chart
        /// </summary>
        public static void DrawHorizontalLevel(Graphics graphics, PriceLevel level,
            int chartWidth, Func<double, float> priceToY, Font font)
        {
            if (level == null || level.IsMitigated) return;

            float y = priceToY(level.Price);
            if (float.IsNaN(y) || float.IsInfinity(y)) return;

            using (Pen pen = new Pen(level.LineColor, level.LineWidth))
            {
                pen.DashStyle = level.LineStyle;
                graphics.DrawLine(pen, 0, y, chartWidth, y);
            }

            // Draw label on the right side
            if (!string.IsNullOrEmpty(level.Label))
            {
                using (Brush brush = new SolidBrush(level.LineColor))
                {
                    SizeF labelSize = graphics.MeasureString(level.Label, font);
                    float labelX = chartWidth - labelSize.Width - 5;
                    float labelY = y - labelSize.Height / 2;

                    // Background for better readability
                    using (Brush bgBrush = new SolidBrush(Color.FromArgb(180, 0, 0, 0)))
                    {
                        graphics.FillRectangle(bgBrush, labelX - 2, labelY - 1,
                            labelSize.Width + 4, labelSize.Height + 2);
                    }

                    graphics.DrawString(level.Label, font, brush, labelX, labelY);
                }
            }
        }

        /// <summary>
        /// Draw a panel with rounded corners
        /// </summary>
        public static void DrawPanel(Graphics graphics, Rectangle bounds, string title,
            string[] lines, Font titleFont, Font contentFont)
        {
            // Draw background
            using (Brush bgBrush = new SolidBrush(PanelBackground))
            {
                DrawRoundedRectangle(graphics, bounds, 8, bgBrush, null);
            }

            // Draw border
            using (Pen borderPen = new Pen(PanelBorder, 1))
            {
                DrawRoundedRectangle(graphics, bounds, 8, null, borderPen);
            }

            int yOffset = bounds.Y + 8;

            // Draw title
            if (!string.IsNullOrEmpty(title))
            {
                using (Brush titleBrush = new SolidBrush(TextColorPrimary))
                {
                    graphics.DrawString(title, titleFont, titleBrush, bounds.X + 10, yOffset);
                }
                yOffset += (int)graphics.MeasureString(title, titleFont).Height + 4;
            }

            // Draw content lines
            if (lines != null)
            {
                using (Brush contentBrush = new SolidBrush(TextColorSecondary))
                {
                    foreach (string line in lines)
                    {
                        if (!string.IsNullOrEmpty(line))
                        {
                            // Check for special color indicators
                            Brush lineBrush = contentBrush;
                            string displayLine = line;

                            if (line.StartsWith("[GREEN]"))
                            {
                                lineBrush = new SolidBrush(TrendingColor);
                                displayLine = line.Substring(7);
                            }
                            else if (line.StartsWith("[RED]"))
                            {
                                lineBrush = new SolidBrush(RangingColor);
                                displayLine = line.Substring(5);
                            }
                            else if (line.StartsWith("[YELLOW]"))
                            {
                                lineBrush = new SolidBrush(TransitionColor);
                                displayLine = line.Substring(8);
                            }
                            else if (line.StartsWith("[CHECK]"))
                            {
                                lineBrush = new SolidBrush(TrendingColor);
                                displayLine = "\u2713 " + line.Substring(7);
                            }
                            else if (line.StartsWith("[EMPTY]"))
                            {
                                lineBrush = new SolidBrush(TextColorSecondary);
                                displayLine = "\u25CB " + line.Substring(7);
                            }

                            graphics.DrawString(displayLine, contentFont, lineBrush, bounds.X + 10, yOffset);

                            if (lineBrush != contentBrush)
                                lineBrush.Dispose();
                        }
                        yOffset += (int)graphics.MeasureString("X", contentFont).Height + 2;
                    }
                }
            }
        }

        /// <summary>
        /// Draw a rounded rectangle
        /// </summary>
        private static void DrawRoundedRectangle(Graphics graphics, Rectangle bounds, int radius,
            Brush fillBrush, Pen borderPen)
        {
            using (GraphicsPath path = new GraphicsPath())
            {
                int diameter = radius * 2;
                Rectangle arc = new Rectangle(bounds.X, bounds.Y, diameter, diameter);

                // Top-left
                path.AddArc(arc, 180, 90);
                // Top-right
                arc.X = bounds.Right - diameter;
                path.AddArc(arc, 270, 90);
                // Bottom-right
                arc.Y = bounds.Bottom - diameter;
                path.AddArc(arc, 0, 90);
                // Bottom-left
                arc.X = bounds.X;
                path.AddArc(arc, 90, 90);

                path.CloseFigure();

                if (fillBrush != null)
                    graphics.FillPath(fillBrush, path);
                if (borderPen != null)
                    graphics.DrawPath(borderPen, path);
            }
        }

        /// <summary>
        /// Draw a filled rectangle for Opening Range
        /// </summary>
        public static void DrawFilledZone(Graphics graphics, float y1, float y2, int width, Color fillColor)
        {
            if (float.IsNaN(y1) || float.IsNaN(y2)) return;

            float top = Math.Min(y1, y2);
            float bottom = Math.Max(y1, y2);
            float height = bottom - top;

            using (Brush brush = new SolidBrush(Color.FromArgb(30, fillColor)))
            {
                graphics.FillRectangle(brush, 0, top, width, height);
            }
        }

        /// <summary>
        /// Highlight a bar background for volume spike
        /// </summary>
        public static void DrawBarHighlight(Graphics graphics, int x, int width, int chartTop, int chartBottom, Color highlightColor)
        {
            using (Brush brush = new SolidBrush(Color.FromArgb(60, highlightColor)))
            {
                graphics.FillRectangle(brush, x, chartTop, width, chartBottom - chartTop);
            }
        }

        /// <summary>
        /// Get color for market regime
        /// </summary>
        public static Color GetRegimeColor(MarketRegimeType regime)
        {
            switch (regime)
            {
                case MarketRegimeType.Trending:
                    return TrendingColor;
                case MarketRegimeType.Ranging:
                    return RangingColor;
                default:
                    return TransitionColor;
            }
        }

        /// <summary>
        /// Get text representation of market regime
        /// </summary>
        public static string GetRegimeText(MarketRegimeType regime)
        {
            switch (regime)
            {
                case MarketRegimeType.Trending:
                    return "TRENDING";
                case MarketRegimeType.Ranging:
                    return "RANGING";
                default:
                    return "TRANSITION";
            }
        }

        /// <summary>
        /// Get session label prefix
        /// </summary>
        public static string GetSessionPrefix(SessionType session)
        {
            switch (session)
            {
                case SessionType.Asia:
                    return "Asia";
                case SessionType.London:
                    return "LDN";
                case SessionType.NYAM:
                    return "NY-AM";
                case SessionType.NYPM:
                    return "NY-PM";
                default:
                    return "";
            }
        }

        /// <summary>
        /// Get session color
        /// </summary>
        public static Color GetSessionColor(SessionType session)
        {
            switch (session)
            {
                case SessionType.Asia:
                    return PurpleColor;
                case SessionType.London:
                    return BlueColor;
                case SessionType.NYAM:
                case SessionType.NYPM:
                    return GreenColor;
                default:
                    return Color.Gray;
            }
        }
    }

    /// <summary>
    /// Mathematical helper functions
    /// </summary>
    public static class MathUtilities
    {
        /// <summary>
        /// Calculate True Range for a single bar
        /// </summary>
        public static double TrueRange(double high, double low, double previousClose)
        {
            double range1 = high - low;
            double range2 = Math.Abs(high - previousClose);
            double range3 = Math.Abs(low - previousClose);

            return Math.Max(range1, Math.Max(range2, range3));
        }

        /// <summary>
        /// Calculate simple moving average of an array
        /// </summary>
        public static double SMA(double[] values, int period)
        {
            if (values == null || values.Length < period) return 0;

            double sum = 0;
            for (int i = values.Length - period; i < values.Length; i++)
            {
                sum += values[i];
            }
            return sum / period;
        }

        /// <summary>
        /// Calculate exponential moving average
        /// </summary>
        public static double EMA(double currentValue, double previousEMA, int period)
        {
            double multiplier = 2.0 / (period + 1);
            return (currentValue - previousEMA) * multiplier + previousEMA;
        }

        /// <summary>
        /// Smoothed moving average (Wilder's method)
        /// </summary>
        public static double SmoothedMA(double currentValue, double previousSMA, int period)
        {
            return (previousSMA * (period - 1) + currentValue) / period;
        }

        /// <summary>
        /// Calculate the highest value in a range
        /// </summary>
        public static double Highest(double[] values, int startIndex, int length)
        {
            if (values == null || startIndex < 0 || startIndex + length > values.Length)
                return double.MinValue;

            double highest = double.MinValue;
            for (int i = startIndex; i < startIndex + length; i++)
            {
                if (values[i] > highest)
                    highest = values[i];
            }
            return highest;
        }

        /// <summary>
        /// Calculate the lowest value in a range
        /// </summary>
        public static double Lowest(double[] values, int startIndex, int length)
        {
            if (values == null || startIndex < 0 || startIndex + length > values.Length)
                return double.MaxValue;

            double lowest = double.MaxValue;
            for (int i = startIndex; i < startIndex + length; i++)
            {
                if (values[i] < lowest)
                    lowest = values[i];
            }
            return lowest;
        }
    }
}
