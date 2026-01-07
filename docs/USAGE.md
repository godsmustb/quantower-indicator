# Usage Guide

## Getting Started

After installing the NQ Setup Scanner, follow these steps to start using it effectively.

## Adding to Chart

1. Open Quantower and connect to your data provider
2. Open an NQ futures chart
3. **Recommended timeframe**: 15-minute
4. Right-click on the chart → **Indicators** → **Add Indicator**
5. Search for "NQ Setup Scanner"
6. Click **Add**

## Understanding the Display

### Market Regime Panel (Top-Right)

```
┌─────────────────────────┐
│ REGIME: + TRENDING      │
│ Chop: 32.4  ADX: 31.2   │
│ Bias: LONG (+DI > -DI)  │
└─────────────────────────┘
```

- **TRENDING** (Green): Low choppiness, high ADX - good for trend-following
- **RANGING** (Red): High choppiness, low ADX - look for reversals at extremes
- **TRANSITION** (Yellow): Mixed signals - wait for clarity

The **Bias** shows directional preference based on +DI vs -DI.

### Setup Scanner Panel (Below Regime)

```
┌──────────────────────────────────┐
│ SETUP SCORE: 3/5 ! WATCH         │
│ ✓ At PDL (21,298)                │
│ ✓ At Asia Low                    │
│ ✓ Volume Spike                   │
│ ○ Not at Swing Level             │
│ ○ Outside Opening Range          │
└──────────────────────────────────┘
```

Shows which confluence factors are active:
- **Checkmark (✓)**: Factor is present
- **Circle (○)**: Factor not present

### Price Levels on Chart

| Level Type | Color | Style | Labels |
|------------|-------|-------|--------|
| PDH/PDL | Gold | Solid 2px | PDH, PDL |
| PDC | White | Dashed 1px | PDC |
| Asia Session | Purple | Dotted | Asia H, Asia L |
| London Session | Blue | Dotted | LDN H, LDN L |
| NY Sessions | Green | Dotted | NY-AM H/L, NY-PM H/L |
| Opening Range | Orange | Solid 2px | OR-H, OR-L |
| BSL (Swing High) | Blue | Dashed | BSL [price] |
| SSL (Swing Low) | Red | Dashed | SSL [price] |

### Volume Spike Highlight

When volume exceeds 1.5x the 20-bar average, the bar background is highlighted in yellow.

## Trading with the Indicator

### High-Probability Setups (Score 4-5)

When you see a HIGH ALERT:

1. **Confirm regime**: Is the market trending or ranging?
2. **Check bias**: Does the directional bias align with your trade idea?
3. **Look for price action**: Wait for a clear reversal or continuation pattern
4. **Manage risk**: Place stops beyond the key level

### Watch Setups (Score 2-3)

When you see WATCH status:

1. **Be patient**: Price is approaching but not at multiple confluences
2. **Set alerts**: Use Quantower's alert system to notify when price reaches levels
3. **Plan ahead**: Decide your action if score increases

### Using Sessions

**Asia Session (6 PM - 12 AM ET)**
- Often sets up the range for the next day
- Look for breakouts of Asia high/low during London or NY

**London Session (2 AM - 5 AM ET)**
- High volatility period
- Often sets the daily direction

**NY AM Session (9:30 AM - 12 PM ET)**
- Most active period for NQ
- Opening Range (first 30 min) often defines the day

**NY PM Session (1:30 PM - 4 PM ET)**
- Often sees reversals or trend continuation
- Watch for moves back to previous session levels

### Liquidity Raids

When you see "SSL SWEPT" or "BSL SWEPT" alerts:

1. **This is a key signal**: Price has taken out liquidity
2. **Watch for reversal**: If price closes back inside, expect a move in the opposite direction
3. **Don't chase**: Wait for confirmation before entering

## Configuring Parameters

### Adjusting Sensitivity

**More conservative (fewer signals)**:
- Increase Level Proximity (ticks) to 15-20
- Increase Volume Spike Multiplier to 2.0

**More aggressive (more signals)**:
- Decrease Level Proximity to 5-8
- Decrease Volume Spike Multiplier to 1.3

### Timeframe Considerations

**Lower timeframes (1-5 min)**:
- Reduce Swing Lookback to 500-1000
- Consider reducing Level Proximity

**Higher timeframes (30min+)**:
- Increase Swing Lookback to 2500+
- Increase Level Proximity

### Customizing Display

To reduce chart clutter:
- Toggle off levels you don't use (Session, Swing, etc.)
- Disable OR Fill if you find it distracting
- Move panels by adjusting code (advanced users)

## Tips and Best Practices

1. **Start with defaults**: The default settings are optimized for NQ 15-minute charts

2. **Don't ignore regime**: A high score in a ranging market means something different than in a trending market

3. **Use with context**: This indicator works best combined with:
   - Support/resistance analysis
   - Order flow (if available)
   - Market internals

4. **Backtesting**: Review past price action at scored levels to build confidence

5. **Risk management**: Even 5/5 setups can fail - always use stops

6. **Timing matters**: The highest probability setups often occur during:
   - London/NY overlap
   - First hour of NY session
   - Major economic releases

## Common Questions

**Q: Why isn't PDH/PDL showing?**
A: Levels are set at session reset (5 PM ET). You need at least one full session of data.

**Q: Why are swing levels not appearing?**
A: Check Swing Lookback setting. Default needs ~1920 bars of data.

**Q: Can I use this on other instruments?**
A: Yes, but it's optimized for NQ. Adjust parameters for other instruments.

**Q: Why do some levels disappear?**
A: Levels are marked as "mitigated" when price trades through them significantly.
