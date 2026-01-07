# Installation Guide

## Prerequisites

Before installing the NQ Setup Scanner, ensure you have:

1. **Quantower Platform** installed (FREE license is sufficient)
   - Download from [https://www.quantower.com/](https://www.quantower.com/)
   - Note your installation path (commonly `C:\Quantower`)

2. **Visual Studio 2022/2026** (or later)
   - Download Community edition (free) from [https://visualstudio.microsoft.com/](https://visualstudio.microsoft.com/)

3. **.NET Framework 4.8** (usually pre-installed on Windows 10/11)

4. **(Optional) Quantower Algo Extension** for Visual Studio
   - Install from Visual Studio Marketplace or Extensions menu
   - This is helpful but NOT required

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

### Step 1: Install Quantower First

**IMPORTANT**: You must have Quantower installed before building!

1. Download Quantower from https://www.quantower.com/
2. Install it (default location: `C:\Quantower`)
3. Run it at least once to create all folders

### Step 2: Clone the Repository

```bash
git clone https://github.com/godsmustB/Quantower-Indicator.git
cd Quantower-Indicator
```

Or download and extract the ZIP file from GitHub.

### Step 3: Open the Solution

1. Navigate to the `NQSetupScanner` folder
2. Double-click `NQSetupScanner.sln` to open in Visual Studio
3. Wait for Visual Studio to load

### Step 4: Add the Quantower Reference (REQUIRED)

You will see errors because the project needs the Quantower API DLL. Here's how to fix it:

#### Method A: Manual Reference (Recommended)

1. In Visual Studio, look at the **Solution Explorer** (usually on the right side)
2. Expand your project **NQSetupScanner**
3. Right-click on **References** (or **Dependencies** in newer VS)
4. Select **Add Reference...** (or **Add Project Reference** → **Browse**)
5. Click **Browse** button at the bottom
6. Navigate to your Quantower installation:
   ```
   C:\Quantower\Bin\
   ```
7. Find and select **`TradingPlatform.BusinessLayer.dll`**
8. Click **Add**, then **OK**

#### Method B: Edit the .csproj File

1. Right-click the project in Solution Explorer
2. Select **Edit Project File** (or Unload Project, then Edit)
3. Find this section near the top:
   ```xml
   <QuantowerPath Condition="'$(QuantowerPath)' == ''">C:\Quantower</QuantowerPath>
   ```
4. Change `C:\Quantower` to your actual Quantower installation path
5. Save and reload the project

#### Method C: Set Environment Variable

1. Open Windows System Properties → Environment Variables
2. Add a new User variable:
   - Name: `QuantowerPath`
   - Value: `C:\Quantower` (or your path)
3. Restart Visual Studio

### Step 5: Verify the Reference

After adding the reference:
1. In Solution Explorer, expand **References**
2. You should see **TradingPlatform.BusinessLayer**
3. If there's a yellow warning triangle, the path is wrong - try again

### Step 6: Build the Solution

1. Select **Release** configuration from the dropdown (top toolbar)
2. Press **Ctrl+Shift+B** or go to **Build** → **Build Solution**
3. Check the **Output** window (View → Output) for "Build succeeded"

### Step 7: Copy the DLL to Quantower

After successful build, the DLL will be at:
```
NQSetupScanner\NQSetupScanner\bin\Release\NQSetupScanner.dll
```

Copy this file to:
```
C:\Quantower\Settings\Scripts\Indicators\
```

**Note**: The updated .csproj includes an auto-copy step that should do this automatically if your Quantower path is correct.

### Step 8: Use in Quantower

1. Open Quantower
2. Open a chart (NQ futures recommended)
3. Right-click on the chart
4. Select **Indicators** → **Add Indicator**
5. Search for "NQ Setup Scanner"
6. Click **Add** to add it to your chart

## Troubleshooting

### Error: "TradingPlatform.BusinessLayer could not be found"

This is the most common error. Solutions:

1. **Verify Quantower is installed**
   - Open File Explorer
   - Navigate to `C:\Quantower\Bin\`
   - Confirm `TradingPlatform.BusinessLayer.dll` exists

2. **Manually add the reference** (see Step 4 Method A above)

3. **Check the path in .csproj**
   - Open `.csproj` file
   - Verify the `QuantowerPath` property matches your installation

### Error: "The referenced component could not be found"

- You added the reference but the file was moved/deleted
- Re-add the reference using Step 4 Method A

### Error: ".NET Framework 4.8 not found"

1. Download .NET Framework 4.8 from Microsoft
2. Install it and restart Visual Studio

### Build succeeds but indicator doesn't appear in Quantower

1. Verify the DLL is in the correct folder:
   ```
   C:\Quantower\Settings\Scripts\Indicators\NQSetupScanner.dll
   ```
2. Restart Quantower completely (File → Exit, then reopen)
3. Check Quantower logs for errors

### Finding Your Quantower Path

If you're not sure where Quantower is installed:

1. Right-click the Quantower desktop shortcut
2. Select **Open file location**
3. This shows you the installation folder
4. The `Bin` subfolder contains the DLLs

Common locations:
- `C:\Quantower`
- `D:\Quantower`
- `C:\Program Files\Quantower`
- `%LOCALAPPDATA%\Quantower`

## Updating

To update to a new version:

1. Close Quantower
2. Pull latest changes: `git pull`
3. Rebuild in Visual Studio
4. Restart Quantower

## Uninstalling

1. Close Quantower
2. Delete `NQSetupScanner.dll` from:
   ```
   C:\Quantower\Settings\Scripts\Indicators\
   ```
3. (Optional) Delete the source code folder
