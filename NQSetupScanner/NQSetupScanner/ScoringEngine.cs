// ============================================================================
// NQ Setup Scanner v1.0
// ScoringEngine.cs - Confluence scoring logic for setup quality assessment
// ============================================================================

using System;
using System.Collections.Generic;
using System.Text;

namespace NQSetupScanner
{
    /// <summary>
    /// Scoring engine for calculating confluence at key levels
    /// Maximum score of 5 points based on level proximity and volume
    /// </summary>
    public class ScoringEngine
    {
        // Configuration
        private readonly int _proximityTicks;
        private readonly double _tickSize;
        private readonly double _proximityDistance;

        // Level references (set externally)
        public LevelManager LevelManager { get; set; }
        public VolumeAnalyzer VolumeAnalyzer { get; set; }

        // Current scoring state
        private ScoringResult _currentResult;

        /// <summary>
        /// Initialize the Scoring Engine
        /// </summary>
        /// <param name="proximityTicks">Number of ticks to consider "at level" (default 10)</param>
        /// <param name="tickSize">Tick size for the instrument (default 0.25 for NQ)</param>
        public ScoringEngine(int proximityTicks = 10, double tickSize = 0.25)
        {
            _proximityTicks = proximityTicks;
            _tickSize = tickSize;
            _proximityDistance = _proximityTicks * _tickSize;

            _currentResult = new ScoringResult();
        }

        /// <summary>
        /// Calculate the confluence score for the current price
        /// </summary>
        /// <param name="currentPrice">Current price (typically close)</param>
        /// <returns>Scoring result with breakdown</returns>
        public ScoringResult CalculateScore(double currentPrice)
        {
            _currentResult = new ScoringResult();

            if (LevelManager == null)
            {
                return _currentResult;
            }

            // Check PDH/PDL (Previous Day High/Low)
            CheckPDLevels(currentPrice);

            // Check Session H/L
            CheckSessionLevels(currentPrice);

            // Check Swing H/L (BSL/SSL)
            CheckSwingLevels(currentPrice);

            // Check Opening Range H/L
            CheckORLevels(currentPrice);

            // Check Volume Spike
            CheckVolumeSpike();

            // Calculate grade
            CalculateGrade();

            return _currentResult;
        }

        /// <summary>
        /// Check proximity to Previous Day levels
        /// </summary>
        private void CheckPDLevels(double price)
        {
            if (LevelManager.PDH != null && !LevelManager.PDH.IsMitigated)
            {
                if (IsWithinProximity(price, LevelManager.PDH.Price))
                {
                    _currentResult.AtPDLevel = true;
                    _currentResult.TotalScore++;
                    UpdateNearestLevel("PDH", LevelManager.PDH.Price, price);
                    return;
                }
            }

            if (LevelManager.PDL != null && !LevelManager.PDL.IsMitigated)
            {
                if (IsWithinProximity(price, LevelManager.PDL.Price))
                {
                    _currentResult.AtPDLevel = true;
                    _currentResult.TotalScore++;
                    UpdateNearestLevel("PDL", LevelManager.PDL.Price, price);
                }
            }
        }

        /// <summary>
        /// Check proximity to Session levels
        /// </summary>
        private void CheckSessionLevels(double price)
        {
            var sessionLevels = LevelManager.GetSessionLevels();

            foreach (var level in sessionLevels)
            {
                if (level != null && !level.IsMitigated)
                {
                    if (IsWithinProximity(price, level.Price))
                    {
                        _currentResult.AtSessionLevel = true;
                        _currentResult.TotalScore++;
                        UpdateNearestLevel(level.Label, level.Price, price);
                        return; // Only count once
                    }
                }
            }
        }

        /// <summary>
        /// Check proximity to Swing levels (BSL/SSL)
        /// </summary>
        private void CheckSwingLevels(double price)
        {
            // Check BSL levels
            foreach (var level in LevelManager.BSLLevels)
            {
                if (level != null && !level.IsMitigated)
                {
                    if (IsWithinProximity(price, level.Price))
                    {
                        _currentResult.AtSwingLevel = true;
                        _currentResult.TotalScore++;
                        UpdateNearestLevel("BSL", level.Price, price);
                        return; // Only count once
                    }
                }
            }

            // Check SSL levels
            foreach (var level in LevelManager.SSLLevels)
            {
                if (level != null && !level.IsMitigated)
                {
                    if (IsWithinProximity(price, level.Price))
                    {
                        _currentResult.AtSwingLevel = true;
                        _currentResult.TotalScore++;
                        UpdateNearestLevel("SSL", level.Price, price);
                        return; // Only count once
                    }
                }
            }
        }

        /// <summary>
        /// Check proximity to Opening Range levels
        /// </summary>
        private void CheckORLevels(double price)
        {
            if (LevelManager.ORHigh != null && !LevelManager.ORHigh.IsMitigated)
            {
                if (IsWithinProximity(price, LevelManager.ORHigh.Price))
                {
                    _currentResult.AtORLevel = true;
                    _currentResult.TotalScore++;
                    UpdateNearestLevel("OR-H", LevelManager.ORHigh.Price, price);
                    return;
                }
            }

            if (LevelManager.ORLow != null && !LevelManager.ORLow.IsMitigated)
            {
                if (IsWithinProximity(price, LevelManager.ORLow.Price))
                {
                    _currentResult.AtORLevel = true;
                    _currentResult.TotalScore++;
                    UpdateNearestLevel("OR-L", LevelManager.ORLow.Price, price);
                }
            }
        }

        /// <summary>
        /// Check for volume spike on current bar
        /// </summary>
        private void CheckVolumeSpike()
        {
            if (VolumeAnalyzer != null && VolumeAnalyzer.IsSpike)
            {
                _currentResult.HasVolumeSpike = true;
                _currentResult.TotalScore++;
            }
        }

        /// <summary>
        /// Check if price is within proximity of a level
        /// </summary>
        private bool IsWithinProximity(double price, double level)
        {
            return Math.Abs(price - level) <= _proximityDistance;
        }

        /// <summary>
        /// Update nearest level if this one is closer
        /// </summary>
        private void UpdateNearestLevel(string label, double levelPrice, double currentPrice)
        {
            double distance = Math.Abs(currentPrice - levelPrice);

            if (string.IsNullOrEmpty(_currentResult.NearestLevel) ||
                distance < Math.Abs(currentPrice - _currentResult.NearestLevelPrice))
            {
                _currentResult.NearestLevel = label;
                _currentResult.NearestLevelPrice = levelPrice;
            }
        }

        /// <summary>
        /// Calculate the grade based on total score
        /// </summary>
        private void CalculateGrade()
        {
            if (_currentResult.TotalScore >= 4)
            {
                _currentResult.Grade = "HIGH ALERT";
            }
            else if (_currentResult.TotalScore >= 2)
            {
                _currentResult.Grade = "WATCH";
            }
            else
            {
                _currentResult.Grade = "";
            }
        }

        /// <summary>
        /// Get display lines for the scoring panel
        /// </summary>
        public string[] GetPanelLines()
        {
            var lines = new List<string>();

            // Header with score and grade
            string gradeEmoji = "";
            string gradeColor = "";

            if (_currentResult.TotalScore >= 4)
            {
                gradeEmoji = "*";
                gradeColor = "[GREEN]";
            }
            else if (_currentResult.TotalScore >= 2)
            {
                gradeEmoji = "!";
                gradeColor = "[YELLOW]";
            }

            string header = $"{gradeColor}SETUP SCORE: {_currentResult.TotalScore}/5";
            if (!string.IsNullOrEmpty(_currentResult.Grade))
            {
                header += $" {gradeEmoji} {_currentResult.Grade}";
            }
            lines.Add(header);

            // Factor checklist
            if (_currentResult.AtPDLevel)
            {
                string detail = !string.IsNullOrEmpty(_currentResult.NearestLevel) &&
                               (_currentResult.NearestLevel == "PDH" || _currentResult.NearestLevel == "PDL")
                    ? $" ({_currentResult.NearestLevelPrice:F0})"
                    : "";
                lines.Add($"[CHECK]At PD Level{detail}");
            }
            else
            {
                lines.Add("[EMPTY]Not at PD Level");
            }

            if (_currentResult.AtSessionLevel)
            {
                lines.Add("[CHECK]At Session Level");
            }
            else
            {
                lines.Add("[EMPTY]Not at Session Level");
            }

            if (_currentResult.AtSwingLevel)
            {
                lines.Add("[CHECK]At Swing Level");
            }
            else
            {
                lines.Add("[EMPTY]Not at Swing Level");
            }

            if (_currentResult.AtORLevel)
            {
                lines.Add("[CHECK]At Opening Range");
            }
            else
            {
                lines.Add("[EMPTY]Outside Opening Range");
            }

            if (_currentResult.HasVolumeSpike)
            {
                lines.Add("[CHECK]Volume Spike");
            }
            else
            {
                lines.Add("[EMPTY]No Volume Spike");
            }

            return lines.ToArray();
        }

        /// <summary>
        /// Check if alert should be triggered (score >= 4)
        /// </summary>
        public bool ShouldAlert => _currentResult.TotalScore >= 4;

        /// <summary>
        /// Get current scoring result
        /// </summary>
        public ScoringResult CurrentResult => _currentResult;

        /// <summary>
        /// Get alert message for high score
        /// </summary>
        public string GetAlertMessage()
        {
            if (!ShouldAlert) return "";

            var sb = new StringBuilder();
            sb.Append($"HIGH ALERT! Score: {_currentResult.TotalScore}/5");

            if (!string.IsNullOrEmpty(_currentResult.NearestLevel))
            {
                sb.Append($" at {_currentResult.NearestLevel}");
            }

            var factors = new List<string>();
            if (_currentResult.AtPDLevel) factors.Add("PD");
            if (_currentResult.AtSessionLevel) factors.Add("Session");
            if (_currentResult.AtSwingLevel) factors.Add("Swing");
            if (_currentResult.AtORLevel) factors.Add("OR");
            if (_currentResult.HasVolumeSpike) factors.Add("Vol");

            if (factors.Count > 0)
            {
                sb.Append($" [{string.Join("+", factors)}]");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Find the nearest level to the current price (for any level type)
        /// </summary>
        /// <param name="currentPrice">Current price</param>
        /// <returns>Tuple of (label, price, distance)</returns>
        public (string Label, double Price, double Distance) FindNearestLevel(double currentPrice)
        {
            string nearestLabel = "";
            double nearestPrice = 0;
            double nearestDistance = double.MaxValue;

            if (LevelManager == null) return (nearestLabel, nearestPrice, nearestDistance);

            var allLevels = LevelManager.GetAllLevels();

            foreach (var level in allLevels)
            {
                if (level != null && !level.IsMitigated)
                {
                    double distance = Math.Abs(currentPrice - level.Price);
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearestLabel = level.Label;
                        nearestPrice = level.Price;
                    }
                }
            }

            return (nearestLabel, nearestPrice, nearestDistance);
        }
    }
}
