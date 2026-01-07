# NQ Setup Scanner v1.0

A comprehensive multi-factor confluence indicator for NQ futures trading on the Quantower platform.

## Features

### Phase 1 (FREE License Compatible)

- **Market Regime Detection** - Choppiness Index + ADX to identify trending, ranging, or transitional market conditions
- **Previous Day Levels** - PDH, PDL, PDC automatically drawn with labels
- **Session Kill Zones** - Asia, London, NY AM, and NY PM session high/low tracking
- **Opening Range** - First 30 minutes (9:30-10:00 AM ET) high/low with optional fill
- **Swing Liquidity Levels** - BSL/SSL detection with 6-month lookback and mitigation tracking
- **Volume Spike Detection** - Highlight bars with volume > 1.5x 20-bar average
- **Confluence Scoring** - 0-5 point scoring system for setup quality

## Requirements

- Quantower Platform (FREE license supported)
- Visual Studio 2022 or later
- .NET Framework 4.8
- Quantower Algo extension for Visual Studio

## Installation

### Quick Install (Pre-built DLL)

1. Download `NQSetupScanner.dll` from the Releases page
2. Copy to `C:\Quantower\Settings\Scripts\Indicators\`
3. Restart Quantower
4. Add indicator to your NQ chart

### Build from Source

1. Clone this repository
2. Open `NQSetupScanner/NQSetupScanner.sln` in Visual Studio 2022
3. Configure Quantower path in Tools → Options → Quantower Algo
4. Build solution (F6)
5. DLL auto-deploys to Quantower indicators folder

See [docs/INSTALLATION.md](docs/INSTALLATION.md) for detailed instructions.

## Usage

1. Open an NQ futures chart (15-minute timeframe recommended)
2. Right-click → Indicators → Find "NQ Setup Scanner"
3. Add to chart
4. Configure parameters in settings panel

See [docs/USAGE.md](docs/USAGE.md) for detailed usage guide.

## Configuration

### Market Regime Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| Choppiness Period | 14 | Period for Choppiness Index calculation |
| ADX Period | 14 | Period for ADX calculation |
| Trending Threshold | 38.2 | Choppiness below this = trending |
| Ranging Threshold | 61.8 | Choppiness above this = ranging |

### Level Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| Swing Lookback Bars | 1920 | ~6 months of 15-min bars |
| Level Proximity (ticks) | 10 | Distance to consider "at level" |
| Mitigation Ticks | 10 | Ticks through level to mark mitigated |

### Volume Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| Volume MA Period | 20 | Moving average period for volume |
| Volume Spike Multiplier | 1.5 | Multiplier threshold for spike |

### Display Options

- Show PDH/PDL (on/off)
- Show Session Levels (on/off)
- Show Opening Range (on/off)
- Show Swing Levels (on/off)
- Show OR Fill (on/off)
- Show Regime Panel (on/off)
- Show Scoring Panel (on/off)

### Alert Options

- Enable Sound Alerts (on/off)
- Alert on High Score (on/off)
- Alert on Volume Spike at Level (on/off)
- Alert on Liquidity Raid (on/off)

## Scoring System

| Factor | Points |
|--------|--------|
| At PDH/PDL (within 10 ticks) | +1 |
| At Session H/L (within 10 ticks) | +1 |
| At Swing H/L (within 10 ticks) | +1 |
| At Opening Range H/L | +1 |
| Volume Spike present | +1 |

### Grades

- **4-5 points**: HIGH ALERT - Key level with confirmation
- **2-3 points**: WATCH - Approaching key level
- **0-1 points**: No alert

## File Structure

```
NQSetupScanner/
├── NQSetupScanner.sln           # Visual Studio Solution
└── NQSetupScanner/
    ├── NQSetupScanner.csproj    # Project file
    ├── NQSetupScanner.cs        # Main indicator class
    ├── MarketRegime.cs          # Choppiness + ADX logic
    ├── LevelManager.cs          # PDH/PDL, Sessions, OR, Swings
    ├── VolumeAnalysis.cs        # Volume spike detection
    ├── ScoringEngine.cs         # Confluence scoring
    └── Utilities.cs             # Helpers, time zones, drawing
```

## Roadmap

See [docs/ROADMAP.md](docs/ROADMAP.md) for planned Phase 2 and Phase 3 features.

## License

MIT License - See [LICENSE](LICENSE) for details.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## Disclaimer

This indicator is for educational and informational purposes only. Trading futures involves substantial risk of loss. Past performance is not indicative of future results. Always do your own research and trade responsibly.
