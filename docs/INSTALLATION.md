# Installation Guide

## Prerequisites

Before installing the NQ Setup Scanner, ensure you have:

1. **Quantower Platform** installed (FREE license is sufficient)
   - Download from [https://www.quantower.com/](https://www.quantower.com/)

2. **Visual Studio 2022** (or later)
   - Download Community edition (free) from [https://visualstudio.microsoft.com/](https://visualstudio.microsoft.com/)

3. **Quantower Algo Extension** for Visual Studio
   - Install from Visual Studio Marketplace or Extensions menu

4. **.NET Framework 4.8** (usually pre-installed on Windows 10/11)

## Method 1: Quick Install (Pre-built DLL)

If you just want to use the indicator without building from source:

1. Go to the [Releases](https://github.com/godsmustB/Quantower-Indicator/releases) page
2. Download the latest `NQSetupScanner.dll` file
3. Copy the DLL to your Quantower indicators folder:
   ```
   C:\Quantower\Settings\Scripts\Indicators\
   ```
   (Adjust path if you installed Quantower elsewhere)
4. Restart Quantower
5. The indicator will appear in the indicators list

## Method 2: Build from Source

### Step 1: Clone the Repository

```bash
git clone https://github.com/godsmustB/Quantower-Indicator.git
cd Quantower-Indicator
```

Or download and extract the ZIP file from GitHub.

### Step 2: Configure Quantower Algo Extension

1. Open Visual Studio 2022
2. Go to **Tools** → **Options**
3. Navigate to **Quantower Algo** section
4. Set the Quantower installation path (e.g., `C:\Quantower`)
5. Click **OK** to save

### Step 3: Open the Solution

1. Navigate to the `NQSetupScanner` folder
2. Double-click `NQSetupScanner.sln` to open in Visual Studio
3. Wait for Visual Studio to load and restore any dependencies

### Step 4: Resolve References (if needed)

If you see reference errors:

1. Right-click on **References** in Solution Explorer
2. Select **Add Reference**
3. Browse to your Quantower installation:
   ```
   C:\Quantower\Bin\
   ```
4. Add reference to `TradingPlatform.BusinessLayer.dll`

### Step 5: Build the Solution

1. Select **Release** configuration (or Debug for development)
2. Press **F6** or go to **Build** → **Build Solution**
3. Check the Output window for "Build succeeded"

### Step 6: Verify Deployment

The Quantower Algo extension should automatically copy the built DLL to:
```
C:\Quantower\Settings\Scripts\Indicators\NQSetupScanner.dll
```

If not automatically deployed, manually copy:
```
NQSetupScanner\bin\Release\NQSetupScanner.dll
```
to the Quantower indicators folder.

### Step 7: Use in Quantower

1. Open Quantower
2. Open a chart (NQ futures recommended)
3. Right-click on the chart
4. Select **Indicators** → **Add Indicator**
5. Search for "NQ Setup Scanner"
6. Click **Add** to add it to your chart

## Troubleshooting

### "Reference not found" errors

- Ensure Quantower is installed and the path is correct in Quantower Algo settings
- Try manually adding the reference to `TradingPlatform.BusinessLayer.dll`

### Indicator not appearing in Quantower

- Check that the DLL was copied to the correct folder
- Restart Quantower completely (close all windows)
- Check for build errors in Visual Studio

### Build errors related to .NET Framework

- Ensure .NET Framework 4.8 is installed
- Right-click project → Properties → Target Framework should be 4.8

### Performance issues

- Reduce the Swing Lookback parameter if loading is slow
- Consider using a higher timeframe chart

## Updating

To update to a new version:

1. Close Quantower
2. Replace the DLL in the indicators folder
3. Restart Quantower

For source updates:
1. Pull the latest changes: `git pull`
2. Rebuild the solution
3. Restart Quantower

## Uninstalling

1. Close Quantower
2. Delete `NQSetupScanner.dll` from:
   ```
   C:\Quantower\Settings\Scripts\Indicators\
   ```
3. (Optional) Delete the source code folder
