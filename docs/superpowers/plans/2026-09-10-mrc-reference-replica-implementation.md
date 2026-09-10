# MRC v0.0.15 Reference Replica Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build MRC v0.0.15 as a native-WPF, reference-locked reconstruction of the Owner-authored Runner Control UI while preserving all verified MRC runtime, lifecycle, ownership, updater, diagnostics, and safety behavior.

**Architecture:** Keep `MRC.Core` lifecycle/runtime authority unchanged. Rebuild only the WPF presentation shell around the existing `RunnerDashboardViewModel` and `RunnerOperationsService`, move non-reference instrumentation into secondary drawers, retain a three-slot lifecycle rail, and add a small CLI Help token-color correction. Visual acceptance is driven by reference-measured geometry plus fresh rendered evidence, with human review authoritative over automated structural checks.

**Tech Stack:** .NET 10, WPF/XAML, C#, PowerShell, GitHub Actions, existing MRC zero-dependency executable test harnesses.

**Spec:** `docs/superpowers/specs/2026-09-10-mrc-reference-replica-design.md`

## Global Constraints

- Implementation version is exactly `0.0.15`; final product target remains `0.1.0`.
- Create `precert-v0.0.15` from the final approved plan commit before the first implementation edit. Do not continue implementation on `precert-v0.0.14`.
- Installed/published `v0.0.14` is frozen historical/live evidence and must not be rewritten.
- The Owner-authored 2048x1222 Runner Control screenshot is the primary visual authority for v0.0.15 where safety/runtime truth does not conflict.
- Native WPF only. No WebView2, browser shell, Direct2D rewrite, or external UI framework.
- Preserve exact machine authority `DOONCHYSCOMPUTI` and runner root `D:\Git_Runners_Main`.
- Preserve MANAGED / EXTERNAL / UNATTRIBUTED ownership rules; external/unattributed runtimes never gain control actions.
- Preserve `RunnerOperationsService` as the GUI lifecycle orchestration boundary. Presentation code must not call process primitives or `RunnerEngine` lifecycle methods directly.
- Preserve BUSY protection and explicit FORCE STOP confirmation.
- Preserve TURN ALL OFF behavior: BUSY runners are skipped, never force-stopped.
- Preserve 20 FPS shared animation architecture; no per-card timers.
- Primary card action rail has exactly three slots. DETAILS is secondary and never becomes a fourth primary slot.
- 2048x1222 must render four columns; 1200x760 two columns; 1180x760 two columns; 900x560 one column.
- Reduce column count before shrinking primary controls.
- Required reference targets at 2048x1222: ~41 px left field inset, ~469 px card width, ~146–154 px card height, ~22 px column/row gaps, ~18–19 px card inner horizontal padding, ~137x46 px enabled START control, ~3.1:1 card aspect.
- Help table structure remains `COMMAND | ALIASES | DESCRIPTION`. Interactive Help colors become literal `MRC` = yellow, option token = cyan, aliases = gray, description = white; redirected output remains plain deterministic text.
- Every implementation task follows RED → prove intended failure → minimal GREEN → focused test → appropriate full regression → commit.
- No visual PASS from XAML/static checks alone. Fresh rendered evidence is mandatory.
- Global certification remains HOLD unless every applicable category is exactly 10.0/10 and live Main-PC evidence is complete.

## Execution Bootstrap

At implementation time, create an isolated branch/worktree from the commit containing this plan. The branch name is fixed:

```bash
git switch precert-v0.0.14
git pull --ff-only
git switch -c precert-v0.0.15
```

If execution uses a separate worktree, create it from the same plan commit and name its branch `precert-v0.0.15`. Verify before Task 1:

```bash
git branch --show-current
git status --short
```

Expected:

```text
precert-v0.0.15
```

and no uncommitted files.

## File Structure Map

### Presentation/theme files

- Create `src/MRC.Gui/Theme/ReplicaTheme.xaml` — reference-derived colors, brushes, shadows, button/card/badge/orb/drawer styles.
- Create `src/MRC.Gui/Presentation/ReferenceReplicaMetrics.cs` — canonical geometry constants and responsive-column calculation inputs.
- Modify `src/MRC.Gui/MainWindow.xaml` — composition only: ambient background, hero header, runner grid/card template, Control Drawer, System Drawer.
- Modify `src/MRC.Gui/MainWindow.xaml.cs` — drawer/focus wiring, primary-slot dispatch, existing lifecycle handler reuse, no process/lifecycle primitives.
- Modify `src/MRC.Gui/Presentation/RunnerDashboardViewModel.cs` only for bounded shell/drawer presentation state if required.
- Modify `src/MRC.Gui/Presentation/RunnerRowViewModel.cs` only for presentation-derived primary-slot text/eligibility if the XAML contract cannot stay declarative.

### CLI files

- Modify `src/MRC.Cli/CliPalette.cs` — add a semantic command-token tone mapped to yellow.
- Modify `src/MRC.Cli/CliTableFormatter.cs` — segment canonical command cells into yellow `MRC` + cyan option token.
- Modify `src/MRC.Cli/CliPresentation.cs` — segmented Help heading `MRC // COMMAND REFERENCE` without changing table structure or other command output semantics.

### Evidence/CI/release files

- Create `scripts/render-v0.0.15-preview.ps1` — canonical v0.0.15 render set, including drawer-open and BUSY-focused frames.
- Modify `tools/MRC.Pass3.Preview/Program.cs` — deterministic preview states needed by v0.0.15 evidence.
- Create `.github/workflows/precert-v0.0.15.yml` — v0.0.15 development regression + render artifact workflow.
- Create `.github/workflows/precert-v0.0.15-release.yml` — exact-head v0.0.15 prerelease workflow.
- Create `.release/v0.0.15.trigger` only after release authorization.
- Add v0.0.15 RED/GREEN contracts under `tests/MRC.Redesign.Tests/Task35...Task46...`.
- Migrate inherited presentation tests only where they encode visual rules explicitly superseded by `Auth/0017_V0015_ReferenceReplica.md`; never weaken runtime/safety assertions.

---

### Task 1: v0.0.15 Branch, Identity, and Development Lane

**Files:**
- Modify: `Directory.Build.props`
- Create: `.github/workflows/precert-v0.0.15.yml`
- Create: `tests/MRC.Redesign.Tests/Task35V0015IdentityContract.cs`
- Modify only if required by current-version pins: `tests/MRC.Tests/Program.cs`, `tests/MRC.Redesign.Tests/Task19V013BuildIdentityContract.cs`, other exact `0.0.14` current-build assertions

**Interfaces:**
- Consumes: `BuildInfo.Version`, `ReleaseAuthority.Current`
- Produces: exact current identity `0.0.15`, channel `precert`, stage `PreCertification`, final target `0.1.0`; CI lane `precert-v0.0.15`

- [ ] **Step 1: Write the failing identity test**

```csharp
using System.Runtime.CompilerServices;
using MRC.Core;

internal static class Task35V0015IdentityContract
{
    [ModuleInitializer]
    internal static void Run()
    {
        if (BuildInfo.Version != "0.0.15")
            throw new InvalidOperationException($"BuildInfo.Version is '{BuildInfo.Version}', expected 0.0.15.");

        var release = ReleaseAuthority.Current;
        if (release.Version != "0.0.15" ||
            release.Channel != "precert" ||
            release.Stage != ReleaseStage.PreCertification ||
            release.FinalTarget != "0.1.0")
            throw new InvalidOperationException($"v0.0.15 release authority mismatch: {release}.");

        Console.WriteLine("PASS  Task35 v0.0.15 identity contract");
    }
}
```

- [ ] **Step 2: Run the redesign harness and prove RED**

Run on Windows/CI:

```powershell
dotnet run --project tests/MRC.Redesign.Tests/MRC.Redesign.Tests.csproj -c Release
```

Expected failure contains:

```text
BuildInfo.Version is '0.0.14', expected 0.0.15.
```

No production file is changed before this failure is observed.

- [ ] **Step 3: Bump assembly identity**

Change `Directory.Build.props` to:

```xml
<Project>
  <PropertyGroup>
    <Version>0.0.15</Version>
    <AssemblyVersion>0.0.15.0</AssemblyVersion>
    <FileVersion>0.0.15.0</FileVersion>
    <InformationalVersion>0.0.15</InformationalVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <ContinuousIntegrationBuild Condition="'$(CI)' == 'true'">true</ContinuousIntegrationBuild>
  </PropertyGroup>
</Project>
```

- [ ] **Step 4: Add the v0.0.15 development workflow**

Create `.github/workflows/precert-v0.0.15.yml` with this execution order:

```yaml
name: MRC v0.0.15 Pre-Cert Development

on:
  push:
    branches: [ precert-v0.0.15 ]
  workflow_dispatch:

permissions:
  contents: read

jobs:
  regression:
    name: v0.0.15 Full Regression Gate
    runs-on: windows-latest
    timeout-minutes: 45
    steps:
      - name: Checkout
        uses: actions/checkout@v7
      - name: Setup .NET from global.json
        uses: actions/setup-dotnet@v6
        with:
          global-json-file: global.json
      - name: Run redesign and v0.0.15 acceptance harness
        shell: pwsh
        run: ./scripts/verify-redesign.ps1
      - name: Run PASS4 operations and updater regression
        shell: pwsh
        run: ./scripts/verify-pass4.ps1
      - name: Run PASS3 presentation regression
        shell: pwsh
        run: ./scripts/verify-pass3.ps1
      - name: Run PASS2 runtime regression
        shell: pwsh
        run: ./scripts/verify-pass2.ps1
      - name: Run PASS1 foundation and distribution regression
        shell: pwsh
        run: ./scripts/verify-pass1.ps1
```

Do not add render steps until Task 10 creates the v0.0.15 renderer.

- [ ] **Step 5: Migrate only stale version pins**

Any inherited test that asserts the *current* build is `0.0.14` becomes a v0.0.15 current-build assertion or a historical v0.0.14 invariant. Do not change behavioral expectations.

- [ ] **Step 6: Verify GREEN through the full pre-render ladder**

Run:

```powershell
./scripts/verify-redesign.ps1
./scripts/verify-pass4.ps1
./scripts/verify-pass3.ps1
./scripts/verify-pass2.ps1
./scripts/verify-pass1.ps1
```

Expected: all pass on exact branch HEAD.

- [ ] **Step 7: Commit**

```bash
git add Directory.Build.props .github/workflows/precert-v0.0.15.yml tests
git commit -m "chore: open v0.0.15 reference replica lane"
```

---

### Task 2: Reference Geometry and Theme Authority

**Files:**
- Create: `src/MRC.Gui/Presentation/ReferenceReplicaMetrics.cs`
- Create: `src/MRC.Gui/Theme/ReplicaTheme.xaml`
- Modify: `src/MRC.Gui/MRC.Gui.csproj` only if explicit Resource inclusion is required by the current SDK settings
- Create: `tests/MRC.Redesign.Tests/Task36V0015ReplicaMetricsContract.cs`

**Interfaces:**
- Produces: `ReferenceReplicaMetrics.ColumnCountForWidth(double)` and immutable reference geometry constants
- Consumes: no lifecycle/runtime APIs

- [ ] **Step 1: Write the RED geometry contract**

```csharp
using System.Runtime.CompilerServices;
using MRC.Gui.Presentation;

internal static class Task36V0015ReplicaMetricsContract
{
    [ModuleInitializer]
    internal static void Run()
    {
        Require(ReferenceReplicaMetrics.CanonicalWidth == 2048, "Canonical reference width drifted.");
        Require(ReferenceReplicaMetrics.CardTargetWidth == 469, "Reference card target width drifted.");
        Require(ReferenceReplicaMetrics.CardTargetHeight == 150, "Reference card target height drifted.");
        Require(ReferenceReplicaMetrics.CardGap == 22, "Reference card gap drifted.");
        Require(ReferenceReplicaMetrics.ActionHeight == 46, "Reference action height drifted.");
        Require(ReferenceReplicaMetrics.ColumnCountForWidth(2048) == 4, "2048 must use four columns.");
        Require(ReferenceReplicaMetrics.ColumnCountForWidth(1200) == 2, "1200 must use two columns.");
        Require(ReferenceReplicaMetrics.ColumnCountForWidth(1180) == 2, "1180 must use two columns.");
        Require(ReferenceReplicaMetrics.ColumnCountForWidth(900) == 1, "900 must use one column.");
        Console.WriteLine("PASS  Task36 v0.0.15 reference geometry contract");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
```

- [ ] **Step 2: Prove RED**

Run:

```powershell
./scripts/verify-redesign.ps1
```

Expected failure: `ReferenceReplicaMetrics` does not exist.

- [ ] **Step 3: Implement immutable reference metrics**

Create:

```csharp
namespace MRC.Gui.Presentation;

public static class ReferenceReplicaMetrics
{
    public const double CanonicalWidth = 2048;
    public const double CanonicalHeight = 1222;
    public const double CardTargetWidth = 469;
    public const double CardTargetHeight = 150;
    public const double CardGap = 22;
    public const double CardHorizontalPadding = 19;
    public const double ActionHeight = 46;
    public const double LargeFieldInset = 41;
    public const double MinimumCardSlotWidth = 470;
    private const double WindowChromeAllowance = 48;

    public static int ColumnCountForWidth(double windowWidth)
    {
        if (double.IsNaN(windowWidth) || double.IsInfinity(windowWidth) || windowWidth <= 0) return 1;
        var usable = Math.Max(0, windowWidth - WindowChromeAllowance);
        return Math.Clamp((int)Math.Floor(usable / MinimumCardSlotWidth), 1, 4);
    }
}
```

- [ ] **Step 4: Create the reference theme dictionary**

Create `src/MRC.Gui/Theme/ReplicaTheme.xaml` beginning with these named tokens:

```xml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <Color x:Key="ReplicaBackgroundColor">#090E10</Color>
    <Color x:Key="ReplicaCardColor">#182127</Color>
    <Color x:Key="ReplicaCardBorderColor">#3A474C</Color>
    <Color x:Key="ReplicaAmberColor">#FFC857</Color>
    <Color x:Key="ReplicaAmberHoverColor">#FFD978</Color>
    <Color x:Key="ReplicaIdleColor">#2AD98B</Color>
    <Color x:Key="ReplicaBusyColor">#FFD166</Color>
    <Color x:Key="ReplicaOffColor">#9AA4AA</Color>
    <Color x:Key="ReplicaErrorColor">#FF7B45</Color>
    <Color x:Key="ReplicaWhiteColor">#F5F7F8</Color>
    <Color x:Key="ReplicaMutedColor">#8D969B</Color>
    <Color x:Key="ReplicaWarmBloomColor">#9C4D16</Color>

    <SolidColorBrush x:Key="ReplicaBackgroundBrush" Color="{StaticResource ReplicaBackgroundColor}" />
    <SolidColorBrush x:Key="ReplicaCardBrush" Color="{StaticResource ReplicaCardColor}" />
    <SolidColorBrush x:Key="ReplicaCardBorderBrush" Color="{StaticResource ReplicaCardBorderColor}" />
    <SolidColorBrush x:Key="ReplicaAmberBrush" Color="{StaticResource ReplicaAmberColor}" />
    <SolidColorBrush x:Key="ReplicaIdleBrush" Color="{StaticResource ReplicaIdleColor}" />
    <SolidColorBrush x:Key="ReplicaWhiteBrush" Color="{StaticResource ReplicaWhiteColor}" />
    <SolidColorBrush x:Key="ReplicaMutedBrush" Color="{StaticResource ReplicaMutedColor}" />
</ResourceDictionary>
```

These are starting reconstruction tokens. Task 11 visual review is authorized to tune token values only when rendered evidence shows a mismatch; geometry/safety changes require their own RED.

- [ ] **Step 5: Verify focused GREEN**

Run:

```powershell
./scripts/verify-redesign.ps1
```

Expected: Task36 passes and no prior redesign test regresses.

- [ ] **Step 6: Commit**

```bash
git add src/MRC.Gui/Presentation/ReferenceReplicaMetrics.cs src/MRC.Gui/Theme/ReplicaTheme.xaml tests/MRC.Redesign.Tests/Task36V0015ReplicaMetricsContract.cs
git commit -m "feat: add v0.0.15 reference geometry and theme authority"
```

---

### Task 3: Compact Three-Slot Runner Card Anatomy

**Files:**
- Modify: `src/MRC.Gui/MainWindow.xaml`
- Modify: `src/MRC.Gui/MainWindow.xaml.cs`
- Modify: `src/MRC.Gui/Presentation/RunnerResponsiveLayout.cs` to delegate to `ReferenceReplicaMetrics`
- Create: `tests/MRC.Redesign.Tests/Task37V0015RunnerCardContract.cs`

**Interfaces:**
- Consumes: `RunnerRowViewModel`, `RunnerOperationsService`, existing START/STOP/RESTART/FORCE STOP eligibility
- Produces: compact reference card with exactly three primary lifecycle slots and secondary DETAILS affordance

- [ ] **Step 1: Write the RED card-structure contract**

```csharp
using System.Runtime.CompilerServices;

internal static class Task37V0015RunnerCardContract
{
    [ModuleInitializer]
    internal static void Run()
    {
        var root = Directory.GetCurrentDirectory();
        var xaml = File.ReadAllText(Path.Combine(root, "src", "MRC.Gui", "MainWindow.xaml"));
        var code = File.ReadAllText(Path.Combine(root, "src", "MRC.Gui", "MainWindow.xaml.cs"));

        Require(xaml.Contains("x:Name=\"ThreeSlotActionRail\"", StringComparison.Ordinal), "Three-slot action rail is missing.");
        Require(xaml.Contains("Height=\"46\"", StringComparison.Ordinal), "Reference action height is missing.");
        Require(xaml.Contains("MinHeight=\"146\"", StringComparison.Ordinal) || xaml.Contains("Height=\"150\"", StringComparison.Ordinal), "Compact reference card height is missing.");
        Require(!xaml.Contains("Content=\"DETAILS\"", StringComparison.Ordinal) || xaml.Contains("x:Name=\"CardDetailsAffordance\"", StringComparison.Ordinal), "DETAILS remains a fourth primary action.");
        Require(xaml.Contains("Content=\"FORCE STOP\"", StringComparison.Ordinal), "BUSY primary slot cannot expose FORCE STOP.");
        Require(code.Contains("RunnerPrimaryAction_OnClick", StringComparison.Ordinal), "Primary START/FORCE STOP dispatcher is missing.");
        Console.WriteLine("PASS  Task37 v0.0.15 compact runner-card contract");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
```

- [ ] **Step 2: Prove RED**

Run:

```powershell
./scripts/verify-redesign.ps1
```

Expected: failure on missing `ThreeSlotActionRail` and/or compact card geometry.

- [ ] **Step 3: Replace the existing action rail with exactly three columns**

The main card template must use this primary rail shape:

```xml
<Grid x:Name="ThreeSlotActionRail" Height="46" Margin="0,12,0,0">
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="*" />
        <ColumnDefinition Width="10" />
        <ColumnDefinition Width="*" />
        <ColumnDefinition Width="10" />
        <ColumnDefinition Width="*" />
    </Grid.ColumnDefinitions>

    <Button Grid.Column="0"
            CommandParameter="{Binding}"
            Click="RunnerPrimaryAction_OnClick">
        <Button.Style>
            <Style TargetType="Button" BasedOn="{StaticResource ReplicaPrimaryActionButtonStyle}">
                <Setter Property="Content" Value="START" />
                <Setter Property="IsEnabled" Value="{Binding CanStart}" />
                <Style.Triggers>
                    <DataTrigger Binding="{Binding State}" Value="{x:Static runtime:RunnerState.BUSY}">
                        <Setter Property="Content" Value="FORCE STOP" />
                        <Setter Property="IsEnabled" Value="True" />
                        <Setter Property="Style" Value="{StaticResource ReplicaForceStopButtonStyle}" />
                    </DataTrigger>
                </Style.Triggers>
            </Style>
        </Button.Style>
    </Button>

    <Button Grid.Column="2" Content="STOP"
            IsEnabled="{Binding CanStop}"
            CommandParameter="{Binding}"
            Click="RunnerStop_OnClick"
            Style="{StaticResource ReplicaSecondaryActionButtonStyle}" />

    <Button Grid.Column="4" Content="RESTART"
            IsEnabled="{Binding CanRestart}"
            CommandParameter="{Binding}"
            Click="RunnerRestart_OnClick"
            Style="{StaticResource ReplicaSecondaryActionButtonStyle}" />
</Grid>
```

If WPF rejects assigning `Style` from inside a `Style` trigger, keep a single button style and switch only Content/Background/BorderBrush/Foreground through data triggers. Do not add a fourth rail slot.

- [ ] **Step 4: Move DETAILS to a secondary header affordance**

Use a 28x28 quiet button near the state badge:

```xml
<Button x:Name="CardDetailsAffordance"
        Width="28" Height="28"
        Content="i"
        ToolTip="Runner details"
        AutomationProperties.Name="Runner details"
        CommandParameter="{Binding}"
        Click="RunnerDetails_OnClick"
        Style="{StaticResource ReplicaDetailsAffordanceStyle}" />
```

- [ ] **Step 5: Preserve FORCE STOP confirmation semantics**

Refactor only the GUI entry point so BUSY slot 1 calls the existing confirmation path:

```csharp
private async void RunnerPrimaryAction_OnClick(object sender, RoutedEventArgs e)
{
    if (!TryGetCardRow(sender, out var row) || _operations is null) return;

    if (row.State == RunnerState.BUSY)
    {
        await ExecuteForceStopAsync(row);
        return;
    }

    if (!row.CanStart) return;
    await ExecuteCardOperationAsync(row, "START", () => _operations.Start(row.Runner));
}
```

Move the current body of `RunnerMoreControl_OnClick` into:

```csharp
private async Task ExecuteForceStopAsync(RunnerRowViewModel row)
```

and retain the exact two-call semantics:

```csharp
_operations.ForceStopBusy(row.Runner, confirmed: false)
_operations.ForceStopBusy(row.Runner, confirmed: true)
```

with the existing explicit `MessageBox` warning between them.

- [ ] **Step 6: Delegate responsive count to reference metrics**

```csharp
public static int ColumnCountForWidth(double windowWidth) =>
    ReferenceReplicaMetrics.ColumnCountForWidth(windowWidth);
```

- [ ] **Step 7: Run focused + safety regressions**

```powershell
./scripts/verify-redesign.ps1
./scripts/verify-pass4.ps1
./scripts/verify-pass2.ps1
```

Expected: Task37 passes; lifecycle/safety suites remain green.

- [ ] **Step 8: Commit**

```bash
git add src/MRC.Gui tests/MRC.Redesign.Tests/Task37V0015RunnerCardContract.cs
git commit -m "feat: reconstruct compact three-slot runner cards"
```

---

### Task 4: Luminous State Orb and State-Specific Card Lighting

**Files:**
- Modify: `src/MRC.Gui/Theme/ReplicaTheme.xaml`
- Modify: `src/MRC.Gui/MainWindow.xaml`
- Create: `tests/MRC.Redesign.Tests/Task38V0015StateOrbContract.cs`

**Interfaces:**
- Consumes: `RunnerRowViewModel.State`, `RunnerRowViewModel.AnimationIntensity`
- Produces: layered state orb and state-dependent card border/glow without any new timer

- [ ] **Step 1: Write RED**

```csharp
using System.Runtime.CompilerServices;

internal static class Task38V0015StateOrbContract
{
    [ModuleInitializer]
    internal static void Run()
    {
        var root = Directory.GetCurrentDirectory();
        var xaml = File.ReadAllText(Path.Combine(root, "src", "MRC.Gui", "MainWindow.xaml"));
        var theme = File.ReadAllText(Path.Combine(root, "src", "MRC.Gui", "Theme", "ReplicaTheme.xaml"));
        var row = File.ReadAllText(Path.Combine(root, "src", "MRC.Gui", "Presentation", "RunnerRowViewModel.cs"));

        Require(xaml.Contains("x:Name=\"StateOrb\"", StringComparison.Ordinal), "Reference state orb is missing.");
        Require(xaml.Contains("RadialGradientBrush", StringComparison.Ordinal), "State orb lacks radial luminosity.");
        Require(xaml.Contains("AnimationIntensity", StringComparison.Ordinal), "State orb does not bind shared animation intensity.");
        Require(theme.Contains("ReplicaIdleGlowBrush", StringComparison.Ordinal), "IDLE glow token is missing.");
        Require(!row.Contains("DispatcherTimer", StringComparison.Ordinal), "Per-row timer was introduced.");
        Console.WriteLine("PASS  Task38 v0.0.15 state-orb contract");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
```

- [ ] **Step 2: Prove RED**

```powershell
./scripts/verify-redesign.ps1
```

Expected failure: missing `StateOrb` / `ReplicaIdleGlowBrush`.

- [ ] **Step 3: Add layered orb markup**

Use a fixed visual housing around 30–34 px at 2048 scale:

```xml
<Grid x:Name="StateOrb" Width="32" Height="32" Margin="0,0,12,0">
    <Ellipse Fill="#0C1215" Stroke="#4B575D" StrokeThickness="1" />
    <Ellipse Width="19" Height="19" Opacity="{Binding AnimationIntensity}">
        <Ellipse.Fill>
            <RadialGradientBrush>
                <GradientStop Color="#F035E08A" Offset="0" />
                <GradientStop Color="#8035E08A" Offset="0.42" />
                <GradientStop Color="#0035E08A" Offset="1" />
            </RadialGradientBrush>
        </Ellipse.Fill>
    </Ellipse>
    <Ellipse Width="6" Height="6" Fill="{Binding StateBrush}" />
</Grid>
```

If the existing row model does not expose `StateBrush`, keep the state mapping in XAML style/data triggers rather than adding a new runtime concept.

- [ ] **Step 4: Add state card lighting triggers**

Theme card style must map:

```text
IDLE      green edge + restrained green outer glow
BUSY      amber edge + restrained amber outer glow
OFF       cool neutral border, no active glow
STARTING  blue edge/glow
STOPPING  purple edge/glow
ERROR     orange/red edge/glow
```

Use `DropShadowEffect` only on the card/orb; never animate shadow creation per frame.

- [ ] **Step 5: Verify timer invariant and presentation**

```powershell
./scripts/verify-redesign.ps1
./scripts/verify-pass3.ps1
```

Expected: no per-row timers, exactly one shared animation timer remains.

- [ ] **Step 6: Commit**

```bash
git add src/MRC.Gui/Theme/ReplicaTheme.xaml src/MRC.Gui/MainWindow.xaml tests/MRC.Redesign.Tests/Task38V0015StateOrbContract.cs
git commit -m "feat: add reference luminous runner state system"
```

---

### Task 5: Hero Header and Ambient Reference Background

**Files:**
- Modify: `src/MRC.Gui/MainWindow.xaml`
- Modify: `src/MRC.Gui/MainWindow.xaml.cs`
- Modify: `src/MRC.Gui/Theme/ReplicaTheme.xaml`
- Create: `tests/MRC.Redesign.Tests/Task39V0015HeroAtmosphereContract.cs`

**Interfaces:**
- Consumes: machine identity, authorization boundary, total/ready counts, refresh timestamp
- Produces: exact primary text `MAIN PC • LOCAL-FIRST CONTROL`, large `MAIN RUNNER CONTROL`, large live inventory capsule, layered amber/cool atmosphere

- [ ] **Step 1: Write RED**

```csharp
using System.Runtime.CompilerServices;

internal static class Task39V0015HeroAtmosphereContract
{
    [ModuleInitializer]
    internal static void Run()
    {
        var xaml = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "src", "MRC.Gui", "MainWindow.xaml"));
        Require(xaml.Contains("MAIN PC • LOCAL-FIRST CONTROL", StringComparison.Ordinal), "Exact eyebrow text is missing.");
        Require(xaml.Contains("MAIN RUNNER CONTROL", StringComparison.Ordinal), "Hero title is missing.");
        Require(xaml.Contains("x:Name=\"LiveInventoryPill\"", StringComparison.Ordinal), "Live inventory capsule is missing.");
        Require(xaml.Contains("x:Name=\"ReferenceAmberBloom\"", StringComparison.Ordinal), "Reference amber bloom is missing.");
        Require(xaml.Contains("RadialGradientBrush", StringComparison.Ordinal), "Ambient radial composition is missing.");
        Console.WriteLine("PASS  Task39 v0.0.15 hero/atmosphere contract");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
```

- [ ] **Step 2: Prove RED**

```powershell
./scripts/verify-redesign.ps1
```

Expected: missing exact eyebrow / live pill / reference amber bloom.

- [ ] **Step 3: Recompose the header**

Primary header structure:

```xml
<Grid Margin="32,28,32,18">
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="Auto" />
        <ColumnDefinition Width="*" />
        <ColumnDefinition Width="Auto" />
    </Grid.ColumnDefinitions>

    <Button x:Name="MenuButton" Width="58" Height="58" Click="MenuButton_OnClick"
            Style="{StaticResource ReplicaMenuButtonStyle}" AutomationProperties.Name="Open controls">☰</Button>

    <StackPanel Grid.Column="1" Margin="22,0,0,0">
        <TextBlock Text="MAIN PC • LOCAL-FIRST CONTROL"
                   FontFamily="Cascadia Mono, Consolas" FontSize="13" FontWeight="SemiBold"
                   Foreground="{StaticResource ReplicaMutedBrush}" />
        <TextBlock Text="MAIN RUNNER CONTROL"
                   FontSize="52" FontWeight="Bold" Foreground="{StaticResource ReplicaWhiteBrush}"
                   Margin="0,7,0,0" />
    </StackPanel>

    <StackPanel Grid.Column="2" HorizontalAlignment="Right">
        <Border x:Name="LiveInventoryPill" Style="{StaticResource ReplicaLivePillStyle}">
            <StackPanel Orientation="Horizontal">
                <Ellipse Width="10" Height="10" Fill="{StaticResource ReplicaIdleBrush}" Margin="0,0,10,0" />
                <TextBlock x:Name="LiveInventoryText" FontWeight="Bold" />
            </StackPanel>
        </Border>
        <TextBlock x:Name="LocalTruthText" HorizontalAlignment="Right" Margin="0,10,0,0"
                   FontFamily="Cascadia Mono, Consolas" Foreground="{StaticResource ReplicaMutedBrush}" />
    </StackPanel>
</Grid>
```

Set live pill text from truthful current state, for example:

```csharp
LiveInventoryText.Text = $"LOCAL INVENTORY LIVE • {_dashboard.TotalCount} CONTROL-READY";
```

If boundary is blocked, replace `CONTROL-READY` with the existing blocked boundary truth; do not show a green live state while controls are unauthorized.

- [ ] **Step 4: Build the ambient background layers**

Use named layers behind all interactive content:

```xml
<Grid x:Name="ReferenceAmbientLayer" IsHitTestVisible="False" ClipToBounds="True">
    <Rectangle Fill="{StaticResource ReplicaBackgroundBrush}" />
    <Ellipse x:Name="ReferenceAmberBloom" Width="1050" Height="720"
             HorizontalAlignment="Right" VerticalAlignment="Bottom" Margin="0,0,-220,-250">
        <Ellipse.Fill>
            <RadialGradientBrush>
                <GradientStop Color="#909C4D16" Offset="0" />
                <GradientStop Color="#409C4D16" Offset="0.48" />
                <GradientStop Color="#009C4D16" Offset="1" />
            </RadialGradientBrush>
        </Ellipse.Fill>
    </Ellipse>
    <Ellipse Width="850" Height="480" HorizontalAlignment="Left" VerticalAlignment="Top" Margin="-250,-170,0,0">
        <Ellipse.Fill>
            <RadialGradientBrush>
                <GradientStop Color="#3025505A" Offset="0" />
                <GradientStop Color="#0025505A" Offset="1" />
            </RadialGradientBrush>
        </Ellipse.Fill>
    </Ellipse>
</Grid>
```

- [ ] **Step 5: Verify GREEN**

```powershell
./scripts/verify-redesign.ps1
./scripts/verify-pass3.ps1
```

Expected: Task39 green; no accessibility/timer/state regression.

- [ ] **Step 6: Commit**

```bash
git add src/MRC.Gui tests/MRC.Redesign.Tests/Task39V0015HeroAtmosphereContract.cs
git commit -m "feat: reconstruct reference hero and ambient shell"
```

---

### Task 6: Control Drawer for Search, Filters, Counters, Bulk Actions

**Files:**
- Modify: `src/MRC.Gui/MainWindow.xaml`
- Modify: `src/MRC.Gui/MainWindow.xaml.cs`
- Modify: `src/MRC.Gui/Theme/ReplicaTheme.xaml`
- Create: `tests/MRC.Redesign.Tests/Task40V0015ControlDrawerContract.cs`

**Interfaces:**
- Consumes: current search/filter/counter/bulk handlers and bindings
- Produces: left overlay drawer; `/` opens/focuses search; Escape closes drawer; primary runner field remains unobstructed when drawer is closed

- [ ] **Step 1: Write RED**

```csharp
using System.Runtime.CompilerServices;

internal static class Task40V0015ControlDrawerContract
{
    [ModuleInitializer]
    internal static void Run()
    {
        var root = Directory.GetCurrentDirectory();
        var xaml = File.ReadAllText(Path.Combine(root, "src", "MRC.Gui", "MainWindow.xaml"));
        var code = File.ReadAllText(Path.Combine(root, "src", "MRC.Gui", "MainWindow.xaml.cs"));
        Require(xaml.Contains("x:Name=\"ControlDrawer\"", StringComparison.Ordinal), "Control Drawer is missing.");
        Require(xaml.Contains("TURN ALL ON", StringComparison.Ordinal) && xaml.Contains("TURN ALL OFF", StringComparison.Ordinal), "Bulk controls were lost.");
        Require(xaml.Contains("SearchBox", StringComparison.Ordinal) && xaml.Contains("Tag=\"BUSY\"", StringComparison.Ordinal), "Search/filter controls were lost.");
        Require(code.Contains("OpenControlDrawer", StringComparison.Ordinal), "Drawer open helper is missing.");
        Require(code.Contains("CloseControlDrawer", StringComparison.Ordinal), "Drawer close helper is missing.");
        Require(code.Contains("Key.Escape", StringComparison.Ordinal), "Escape close behavior is missing.");
        Console.WriteLine("PASS  Task40 v0.0.15 control drawer contract");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
```

- [ ] **Step 2: Prove RED**

```powershell
./scripts/verify-redesign.ps1
```

Expected: missing `ControlDrawer` / helpers.

- [ ] **Step 3: Move, do not duplicate, existing instrumentation into drawer**

Create an overlay `Border x:Name="ControlDrawer"` aligned left, initially collapsed, containing the existing counter bindings, filter buttons, `SearchBox`, TURN ALL ON/OFF, and manual refresh button. Remove their old permanent top-level rows from the primary layout after confirming they exist once in the drawer.

Use these exact helpers:

```csharp
private bool _controlDrawerOpen;

private void OpenControlDrawer(bool focusSearch = false)
{
    _controlDrawerOpen = true;
    ControlDrawer.Visibility = Visibility.Visible;
    DrawerScrim.Visibility = Visibility.Visible;
    if (focusSearch)
    {
        SearchBox.Focus();
        SearchBox.SelectAll();
    }
}

private void CloseControlDrawer()
{
    _controlDrawerOpen = false;
    ControlDrawer.Visibility = Visibility.Collapsed;
    DrawerScrim.Visibility = Visibility.Collapsed;
}
```

- [ ] **Step 4: Wire menu, slash, and Escape deterministically**

```csharp
private void MenuButton_OnClick(object sender, RoutedEventArgs e)
{
    if (_controlDrawerOpen) CloseControlDrawer();
    else OpenControlDrawer();
}
```

Update keyboard handling:

```csharp
if (e.Key == Key.Escape && _controlDrawerOpen)
{
    CloseControlDrawer();
    e.Handled = true;
    return;
}

if (!SearchBox.IsKeyboardFocusWithin && Keyboard.Modifiers == ModifierKeys.None &&
    e.Key is Key.OemQuestion or Key.Divide)
{
    OpenControlDrawer(focusSearch: true);
    e.Handled = true;
}
```

- [ ] **Step 5: Run behavior regressions**

```powershell
./scripts/verify-redesign.ps1
./scripts/verify-pass4.ps1
./scripts/verify-pass3.ps1
```

Expected: search/filter/counters/bulk assertions remain green; lifecycle routing unchanged.

- [ ] **Step 6: Commit**

```bash
git add src/MRC.Gui tests/MRC.Redesign.Tests/Task40V0015ControlDrawerContract.cs
git commit -m "feat: move secondary controls into reference drawer"
```

---

### Task 7: System Drawer and External/Unattributed Read-Only Evidence

**Files:**
- Modify: `src/MRC.Gui/MainWindow.xaml`
- Modify: `src/MRC.Gui/MainWindow.xaml.cs`
- Modify: `src/MRC.Gui/Theme/ReplicaTheme.xaml`
- Create: `tests/MRC.Redesign.Tests/Task41V0015SystemDrawerContract.cs`

**Interfaces:**
- Consumes: existing `SystemFindingsSummary` and runtime report findings
- Produces: compact bottom affordance + expandable read-only System Drawer

- [ ] **Step 1: Write RED**

```csharp
using System.Runtime.CompilerServices;

internal static class Task41V0015SystemDrawerContract
{
    [ModuleInitializer]
    internal static void Run()
    {
        var root = Directory.GetCurrentDirectory();
        var xaml = File.ReadAllText(Path.Combine(root, "src", "MRC.Gui", "MainWindow.xaml"));
        var code = File.ReadAllText(Path.Combine(root, "src", "MRC.Gui", "MainWindow.xaml.cs"));
        Require(xaml.Contains("x:Name=\"SystemDrawer\"", StringComparison.Ordinal), "System Drawer is missing.");
        Require(xaml.Contains("SystemFindingsSummary", StringComparison.Ordinal), "System findings summary was lost.");
        Require(code.Contains("_engine.RefreshReport()", StringComparison.Ordinal), "Runtime report source was changed.");
        Require(!code.Contains("_engine.ForceStopBusy(", StringComparison.Ordinal), "GUI bypassed operations boundary.");
        Console.WriteLine("PASS  Task41 v0.0.15 system drawer contract");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
```

- [ ] **Step 2: Prove RED**

```powershell
./scripts/verify-redesign.ps1
```

Expected: missing `SystemDrawer`.

- [ ] **Step 3: Implement read-only bottom drawer**

The collapsed state keeps one compact status affordance. The expanded state shows `SystemFindingsSummary` and existing external/unattributed detail text. Do not bind lifecycle buttons inside this surface.

Use presentation-only toggle state:

```csharp
private bool _systemDrawerOpen;

private void SystemSummary_OnClick(object sender, RoutedEventArgs e)
{
    _systemDrawerOpen = !_systemDrawerOpen;
    SystemDrawer.Visibility = _systemDrawerOpen ? Visibility.Visible : Visibility.Collapsed;
}
```

- [ ] **Step 4: Verify external isolation**

```powershell
./scripts/verify-redesign.ps1
./scripts/verify-pass4.ps1
./scripts/verify-pass2.ps1
```

Expected: external/unattributed findings remain separate and never become controllable rows.

- [ ] **Step 5: Commit**

```bash
git add src/MRC.Gui tests/MRC.Redesign.Tests/Task41V0015SystemDrawerContract.cs
git commit -m "feat: add read-only system evidence drawer"
```

---

### Task 8: CLI Help Command-Token Color Grammar

**Files:**
- Modify: `src/MRC.Cli/CliPalette.cs`
- Modify: `src/MRC.Cli/CliTableFormatter.cs`
- Modify: `src/MRC.Cli/CliPresentation.cs`
- Create: `tests/MRC.Redesign.Tests/Task42V0015HelpTokenContract.cs`

**Interfaces:**
- Produces: semantic `CliTone.Command`; Help command cell segments `MRC` yellow + option cyan; aliases gray; descriptions white
- Preserves: redirected plain text, table width/wrapping, all aliases

- [ ] **Step 1: Write RED**

```csharp
using System.Runtime.CompilerServices;
using MRC.Cli;

internal static class Task42V0015HelpTokenContract
{
    [ModuleInitializer]
    internal static void Run()
    {
        var lines = CliPresentation.HelpLines(108);
        var version = lines.Single(line => line.Text.StartsWith("MRC --version", StringComparison.Ordinal));
        Require(version.Segments.Any(s => s.Text == "MRC" && s.Tone == CliTone.Command), "MRC command token is not semantic yellow.");
        Require(version.Segments.Any(s => s.Text.Contains("--version", StringComparison.Ordinal) && s.Tone == CliTone.Heading), "Option token is not cyan heading tone.");
        Require(version.Segments.Any(s => s.Text.Contains("MRC -v", StringComparison.Ordinal) && s.Tone == CliTone.Secondary), "Aliases are not gray/secondary.");
        Require(CliPalette.ColorFor(CliTone.Command) == ConsoleColor.Yellow, "Command tone is not yellow.");
        Console.WriteLine("PASS  Task42 v0.0.15 Help command-token contract");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
```

- [ ] **Step 2: Prove RED**

```powershell
./scripts/verify-redesign.ps1
```

Expected failure: `CliTone.Command` does not exist.

- [ ] **Step 3: Add semantic command tone**

Change enum/palette:

```csharp
public enum CliTone
{
    Normal = 0,
    Heading = 1,
    Success = 2,
    Warning = 3,
    Error = 4,
    Path = 5,
    Metadata = 6,
    Secondary = 7,
    Command = 8
}
```

and:

```csharp
CliTone.Command => ConsoleColor.Yellow,
```

- [ ] **Step 4: Segment canonical command cells**

Add to `CliTableFormatter`:

```csharp
private static IReadOnlyList<CliSegment> CommandSegments(string command, int paddedWidth)
{
    var trimmed = command.Trim();
    if (trimmed.Equals("MRC", StringComparison.Ordinal))
        return new[] { new CliSegment("MRC".PadRight(paddedWidth), CliTone.Command) };

    const string prefix = "MRC ";
    if (!trimmed.StartsWith(prefix, StringComparison.Ordinal))
        return new[] { new CliSegment(trimmed.PadRight(paddedWidth), CliTone.Heading) };

    var option = trimmed[prefix.Length..];
    var used = 4 + option.Length;
    return new[]
    {
        new CliSegment("MRC", CliTone.Command),
        new CliSegment(" ", CliTone.Normal),
        new CliSegment(option, CliTone.Heading),
        new CliSegment(new string(' ', Math.Max(0, paddedWidth - used)), CliTone.Normal)
    };
}
```

Use these segments instead of a single cyan command segment while keeping alias and description segments unchanged.

- [ ] **Step 5: Segment the Help heading**

Replace the single-tone heading with:

```csharp
new CliLine(
    CliTone.Heading,
    new CliSegment("MRC", CliTone.Command),
    new CliSegment(" // COMMAND REFERENCE", CliTone.Heading))
```

- [ ] **Step 6: Verify interactive semantics and redirected determinism**

```powershell
./scripts/verify-redesign.ps1
./scripts/verify-pass3.ps1
```

Also run redirected output:

```powershell
$out = & ./src/MRC.Cli/bin/Release/net10.0/MRC.exe --help | Out-String
if ($out -match "\x1B") { throw 'Redirected Help contains ANSI escape bytes.' }
```

Expected: no ANSI/control coloring in redirected text.

- [ ] **Step 7: Commit**

```bash
git add src/MRC.Cli tests/MRC.Redesign.Tests/Task42V0015HelpTokenContract.cs
git commit -m "feat: color Help MRC command tokens yellow"
```

---

### Task 9: GUI Single-Instance Truth Consistency

**Files:**
- Modify: `src/MRC.Cli/GuiLauncher.cs` and/or `src/MRC.Cli/GuiInstanceActivator.cs` only after the RED isolates the actual race
- Create: `tests/MRC.Redesign.Tests/Task43V0015GuiInstanceTruthContract.cs`

**Interfaces:**
- Consumes: `IGuiInstanceActivator.TryActivate(TimeSpan)`
- Produces: stable live truth: an already-running GUI reports `AlreadyRunningActivated`; a genuine first launch reports `Opened`; no second GUI process is intentionally launched after successful activation

- [ ] **Step 1: Add deterministic unit coverage around launcher branching**

```csharp
using System.Runtime.CompilerServices;
using MRC.Cli;

internal static class Task43V0015GuiInstanceTruthContract
{
    [ModuleInitializer]
    internal static void Run()
    {
        var active = new GuiLauncher(new FakeActivator(true));
        var result = active.Launch();
        if (result.Outcome != GuiLaunchOutcome.AlreadyRunningActivated)
            throw new InvalidOperationException("Successful activation was not reported as ALREADY RUNNING.");

        Console.WriteLine("PASS  Task43 v0.0.15 GUI instance truth contract");
    }

    private sealed class FakeActivator : IGuiInstanceActivator
    {
        private readonly bool _success;
        public FakeActivator(bool success) => _success = success;
        public GuiActivationResult TryActivate(TimeSpan timeout) =>
            new(_success, _success ? "activated" : "not found");
    }
}
```

If `GuiActivationResult` constructor shape differs, use the exact current type signature from `GuiInstanceActivator.cs`; do not invent a second activation model.

- [ ] **Step 2: Add an integration test for activation race behavior**

Create a fake activator that fails the first probe and succeeds on one bounded retry. The expected launcher result is `AlreadyRunningActivated`, not `Opened`.

The production design target is:

```csharp
var activation = _activator.TryActivate(TimeSpan.FromMilliseconds(300));
if (!activation.Success)
{
    activation = _activator.TryActivate(TimeSpan.FromMilliseconds(300));
}
if (activation.Success)
{
    return new GuiLaunchResult(
        GuiLaunchOutcome.AlreadyRunningActivated,
        "Main Runner Control is already running; existing window activation requested.");
}
```

Only implement this retry if the RED demonstrates the current one-probe behavior is the source of the inconsistent live report. If the RED reveals a different root cause, fix that root cause and preserve the same outcome contract.

- [ ] **Step 3: Verify no duplicate-process regression**

```powershell
./scripts/verify-redesign.ps1
./scripts/verify-pass4.ps1
```

Expected: duplicate GUI prevention tests remain green.

- [ ] **Step 4: Commit**

```bash
git add src/MRC.Cli tests/MRC.Redesign.Tests/Task43V0015GuiInstanceTruthContract.cs
git commit -m "fix: stabilize GUI single-instance launch truth"
```

---

### Task 10: v0.0.15 Preview Renderer and Evidence Matrix

**Files:**
- Create: `scripts/render-v0.0.15-preview.ps1`
- Modify: `tools/MRC.Pass3.Preview/Program.cs`
- Modify: `.github/workflows/precert-v0.0.15.yml`
- Create: `tests/MRC.Redesign.Tests/Task44V0015EvidenceContract.cs`

**Interfaces:**
- Produces deterministic evidence frames:
  - `MRC-v0.0.15-2048x1222.png`
  - `MRC-v0.0.15-1200x760.png`
  - `MRC-v0.0.15-1180x760.png`
  - `MRC-v0.0.15-900x560.png`
  - `MRC-v0.0.15-900x560-busy.png`
  - `MRC-v0.0.15-1200x760-drawer.png`
  - `MRC-v0.0.15-2048x1222-mixed.png`

- [ ] **Step 1: Write RED evidence contract**

```csharp
using System.Runtime.CompilerServices;

internal static class Task44V0015EvidenceContract
{
    [ModuleInitializer]
    internal static void Run()
    {
        var script = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "scripts", "render-v0.0.15-preview.ps1"));
        foreach (var name in new[]
        {
            "MRC-v0.0.15-2048x1222.png",
            "MRC-v0.0.15-1200x760.png",
            "MRC-v0.0.15-1180x760.png",
            "MRC-v0.0.15-900x560.png",
            "MRC-v0.0.15-900x560-busy.png",
            "MRC-v0.0.15-1200x760-drawer.png",
            "MRC-v0.0.15-2048x1222-mixed.png"
        })
            if (!script.Contains(name, StringComparison.Ordinal))
                throw new InvalidOperationException($"Missing v0.0.15 evidence frame {name}.");

        Console.WriteLine("PASS  Task44 v0.0.15 evidence contract");
    }
}
```

- [ ] **Step 2: Prove RED**

```powershell
./scripts/verify-redesign.ps1
```

Expected: missing render script/frame names.

- [ ] **Step 3: Extend preview CLI deterministically**

`tools/MRC.Pass3.Preview/Program.cs` must support explicit preview options, using the existing state-focus mechanism rather than runtime discovery:

```text
--width <int>
--height <int>
--output <path>
--focus-state BUSY
--drawer control
--mixed-state
```

The preview model must include OFF, IDLE, and BUSY rows in `--mixed-state` mode and must never execute runner operations.

- [ ] **Step 4: Create v0.0.15 render script**

The PowerShell script invokes the preview tool once per required frame and then validates dimensions using the same image-dimension verification method already proven in v0.0.14.

Required output root:

```powershell
$artifactRoot = Join-Path $repoRoot 'artifacts\v0.0.15'
```

- [ ] **Step 5: Add render/upload steps to development CI**

Append to `.github/workflows/precert-v0.0.15.yml`:

```yaml
      - name: Render v0.0.15 GUI previews
        shell: pwsh
        run: ./scripts/render-v0.0.15-preview.ps1

      - name: Upload v0.0.15 GUI evidence
        uses: actions/upload-artifact@v6
        with:
          name: MRC-v0.0.15-gui-previews
          path: artifacts/v0.0.15
          if-no-files-found: error
```

- [ ] **Step 6: Verify exact-head CI including evidence upload**

Expected: redesign → PASS4 → PASS3 → PASS2 → PASS1 → seven renders → artifact upload all green on the same SHA.

- [ ] **Step 7: Commit**

```bash
git add scripts/render-v0.0.15-preview.ps1 tools/MRC.Pass3.Preview/Program.cs .github/workflows/precert-v0.0.15.yml tests/MRC.Redesign.Tests/Task44V0015EvidenceContract.cs
git commit -m "test: add v0.0.15 reference evidence matrix"
```

---

### Task 11: Reference Comparison and Human Visual Acceptance Gate

**Files:**
- Create: `tools/reference-comparison/README.md`
- Create: `scripts/build-v0.0.15-reference-comparison.ps1`
- Create: `tests/MRC.Redesign.Tests/Task45V0015ReferenceComparisonContract.cs`
- No production GUI file changes unless a visual finding receives its own RED and bounded correction commit

**Interfaces:**
- Consumes: Owner reference image supplied for review + exact-head v0.0.15 screenshots
- Produces: side-by-side comparison PNG and 50% overlay/difference diagnostic package

- [ ] **Step 1: Write RED comparison contract**

```csharp
using System.Runtime.CompilerServices;

internal static class Task45V0015ReferenceComparisonContract
{
    [ModuleInitializer]
    internal static void Run()
    {
        var root = Directory.GetCurrentDirectory();
        var script = File.ReadAllText(Path.Combine(root, "scripts", "build-v0.0.15-reference-comparison.ps1"));
        Require(script.Contains("reference-side-by-side.png", StringComparison.Ordinal), "Side-by-side comparison output is missing.");
        Require(script.Contains("reference-overlay-50.png", StringComparison.Ordinal), "50% overlay output is missing.");
        Require(script.Contains("2048x1222", StringComparison.Ordinal), "Canonical comparison is not anchored to 2048x1222.");
        Console.WriteLine("PASS  Task45 v0.0.15 reference comparison contract");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
```

- [ ] **Step 2: Prove RED**

```powershell
./scripts/verify-redesign.ps1
```

Expected: comparison script missing.

- [ ] **Step 3: Build the diagnostic comparison script**

The script takes two explicit paths:

```powershell
param(
    [Parameter(Mandatory=$true)][string]$Reference,
    [Parameter(Mandatory=$true)][string]$Candidate,
    [string]$OutputRoot = './artifacts/v0.0.15/reference-comparison'
)
```

It validates that both source images are 2048x1222, creates `reference-side-by-side.png`, and creates `reference-overlay-50.png`. Use installed .NET/System.Drawing-compatible tooling already available in the Windows CI/runtime environment; do not add a new package dependency solely for the comparison.

- [ ] **Step 4: Run the exact-head render and inspect every frame at realistic size**

The human review matrix is mandatory:

```text
2048x1222 primary     four columns, reference card proportions, hero/live pill, ambient bloom
1200x760 primary      two columns, no clipping, primary composition preserved
1180x760 primary      two columns, no clipping
900x560 primary       one column, readable controls
900x560 BUSY          full FORCE STOP visible, no squeeze
1200 drawer-open      search/filter/counters/bulk controls fully usable
2048 mixed-state      OFF/IDLE/BUSY orb, border, badge, and action semantics visibly distinct
```

- [ ] **Step 5: Score visual categories individually**

Required categories, each exactly `10.0/10` for PASS:

```text
Reference composition fidelity
Hero/header fidelity
Card geometry/density
State orb fidelity
State border/glow fidelity
Action rail fidelity
Enabled/disabled clarity
Ambient background fidelity
Typography hierarchy
Responsive quality
Drawer integration
System evidence integration
Accessibility/readability
```

Any category below 10.0/10 creates a named visual finding and HOLD. Fix each finding through a dedicated RED/green bounded correction before rescoring.

- [ ] **Step 6: Commit comparison tooling only after its contract is green**

```bash
git add scripts/build-v0.0.15-reference-comparison.ps1 tools/reference-comparison/README.md tests/MRC.Redesign.Tests/Task45V0015ReferenceComparisonContract.cs
git commit -m "test: add reference replica comparison evidence"
```

---

### Task 12: v0.0.15 Release Packaging and Exact-Head Provenance

**Precondition:** Task 11 visual matrix is 10.0/10 in every applicable category. Do not execute this task while visual status is HOLD.

**Files:**
- Create: `.github/workflows/precert-v0.0.15-release.yml`
- Create: `tests/MRC.Redesign.Tests/Task46V0015ReleaseContract.cs`
- Modify: `tests/MRC.Tests/Program.cs` current package identity assertions to `0.0.15`
- Create later, only after workflow is development-green: `.release/v0.0.15.trigger`

**Interfaces:**
- Produces: `MRC-v0.0.15-win-x64.zip`, `SHA256SUMS.txt`, exact-head `v0.0.15` prerelease, seven required visual assets

- [ ] **Step 1: Write RED release contract**

The structural test must require:

```text
workflow branch = precert-v0.0.15
trigger path = .release/v0.0.15.trigger
package = MRC-v0.0.15-win-x64.zip
manifest version = 0.0.15
channel = precert
releaseStage = PreCertification
finalTarget = 0.1.0
published tag = v0.0.15
post-publish tag SHA = GITHUB_SHA
seven v0.0.15 evidence assets
prerelease = true
```

Representative contract code:

```csharp
var workflow = File.ReadAllText(Path.Combine(root, ".github", "workflows", "precert-v0.0.15-release.yml"));
Require(workflow.Contains("branches: [ precert-v0.0.15 ]", StringComparison.Ordinal), "Release branch isolation is missing.");
Require(workflow.Contains(".release/v0.0.15.trigger", StringComparison.Ordinal), "Release trigger isolation is missing.");
Require(workflow.Contains("MRC-v0.0.15-win-x64.zip", StringComparison.Ordinal), "v0.0.15 package identity is missing.");
Require(workflow.Contains("git/ref/tags/v0.0.15", StringComparison.Ordinal), "Exact-head Git-tag verification is missing.");
```

- [ ] **Step 2: Prove RED**

```powershell
./scripts/verify-redesign.ps1
```

Expected: v0.0.15 release workflow missing.

- [ ] **Step 3: Create release workflow by porting the proven v0.0.14 pipeline**

Preserve this order:

```text
acceptance harness
PASS4
PASS3
PASS2
PASS1
v0.0.15 seven-frame render
package
SHA256 verification
manifest verification
packaged CLI --version verification
publish/replace exact-head prerelease
explicit non-forced Git tag ref update if an existing prerelease is being replaced
post-publish exact SHA read-back
```

All versioned paths and checks become `0.0.15`; final target remains `0.1.0`.

- [ ] **Step 4: Verify workflow structure through normal development CI**

Do not create the trigger yet. Push the workflow and require exact-head development CI green.

- [ ] **Step 5: Create release trigger only after development GREEN**

Create `.release/v0.0.15.trigger` containing audit text with the immediately preceding accepted candidate SHA and visual-review status.

- [ ] **Step 6: Follow release workflow to terminal success**

Capture:

```text
release workflow run id
trigger commit SHA
package SHA-256
published tag target SHA
release metadata target SHA
asset inventory
```

Require tag target SHA == release metadata target SHA == trigger commit SHA.

- [ ] **Step 7: Independently read the published release/tag through GitHub API**

Do not trust the workflow success flag alone. Verify `prerelease=true`, exact tag target, package asset digest when available, checksum asset, and all seven evidence images.

- [ ] **Step 8: Commit any final release-contract migration before trigger; never rewrite v0.0.14**

---

### Task 13: Main-PC Update and Live Certification Gauntlet

**Precondition:** v0.0.15 prerelease provenance is independently verified.

**Files:**
- No source changes during evidence collection
- If a live defect appears, create a named finding and return to a RED/GREEN correction task before continuing certification

**Interfaces:**
- Consumes: installed v0.0.14, published v0.0.15 prerelease
- Produces: direct Main-PC runtime evidence required by `Auth/0009_Test.md`

- [ ] **Step 1: Prove update discovery from the installed build**

Run on Main-PC:

```powershell
MRC --version
MRC --check
```

Expected before update:

```text
Version: 0.0.14
Available: 0.0.15
Status: UPDATE AVAILABLE
```

`--check` must state that no changes were made.

- [ ] **Step 2: Update through MRC itself**

```powershell
MRC --update
MRC --version
```

Expected:

```text
MRC updated 0.0.14 -> 0.0.15
Version: 0.0.15
Channel: precert
Stage: PRE-CERTIFICATION
Final target: 0.1.0
```

Previous 0.0.14 remains retained for rollback.

- [ ] **Step 3: Verify live Help color grammar**

```powershell
MRC --help
```

Visually verify:

```text
literal MRC token = yellow
--option token = cyan
aliases = gray
description = white
```

- [ ] **Step 4: Verify bare-MRC single-instance truth repeatedly**

Run:

```powershell
MRC
MRC
MRC
```

Expected after the first window exists: every later invocation reports `ALREADY RUNNING`, `RESTORED + FOCUSED`, `READY`; Task43 does not pass live if any later invocation reports `NEW INSTANCE` while the same GUI is still running.

- [ ] **Step 5: Owner visual review against the reference**

Capture the live 2048-scale or full-screen Main-PC GUI and compare directly to the Owner reference. Re-score every Task 11 visual category. Any value below 10.0/10 = HOLD.

- [ ] **Step 6: Individual lifecycle gauntlet on one safe managed runner**

Use a currently OFF managed runner when possible:

```text
OFF -> START -> STARTING -> IDLE
IDLE -> STOP -> STOPPING -> OFF
OFF -> START -> IDLE
IDLE -> RESTART -> verified OFF -> STARTING -> IDLE
```

Repeat sufficiently to prove no duplicate listener sessions are created.

- [ ] **Step 7: Bulk control gauntlet**

Prove:

```text
TURN ALL ON starts only verified OFF managed runners
TURN ALL OFF stops only IDLE managed runners
BUSY managed runners are skipped and reported
external/unattributed runtimes are untouched
```

- [ ] **Step 8: BUSY protection gauntlet**

With a safe BUSY managed runner or controlled test job:

```text
normal STOP blocked/protected
RESTART blocked while BUSY
primary slot reads FORCE STOP
FORCE STOP first requests explicit confirmation
cancel confirmation leaves job running
```

Do not confirm the destructive stop unless interruption is intentionally safe.

- [ ] **Step 9: External Lotto isolation**

Verify the known external `actions.runner.doonchy16-cloud-Lotto_engine.Lotto_MainPC_Runner` remains visible as SYSTEM/external evidence and never gains managed controls.

- [ ] **Step 10: Closing-window invariant**

Start at least one safe managed runner, close the MRC GUI, and verify the runner continues running. Reopen MRC and verify state is rediscovered correctly.

- [ ] **Step 11: Final certification decision**

Only mark v0.0.15 pre-cert acceptance PASS if all applicable categories are exactly 10.0/10:

```text
Reference visual fidelity
Responsive visual quality
CLI Help presentation
Single-instance launch truth
Environment/root boundary
Discovery/runtime state
Individual lifecycle controls
BUSY protection
Bulk controls
External isolation
Update/recovery
Packaging/provenance
Accessibility/readability
```

Any unresolved or unverified item remains HOLD. Passing v0.0.15 pre-cert does not itself authorize final `v0.1.0`; final release still follows the governing PASS 5 authority.

---

## Plan Self-Review Checklist

Before execution, verify the plan against the locked spec:

- Spec sections 1–4: version, native WPF, authority precedence → Tasks 1–2.
- Sections 5, 9, 10, 12: measured geometry, card anatomy, state system, responsive model → Tasks 2–4.
- Sections 6, 8, 11: primary composition, hero, atmosphere → Task 5.
- Section 13: secondary instrumentation → Tasks 6–7.
- Section 14: Help color grammar → Task 8.
- Functional acceptance item for single-instance truth → Task 9 + Task 13.
- Sections 19–20: render/reference evidence and visual scoring → Tasks 10–11.
- Section 22: v0.0.15 release identity/provenance → Task 12.
- Section 21 + governing `Auth/0009_Test.md`: live lifecycle/safety gate → Task 13.
- No task authorizes a new lifecycle engine, updater model, runner service manager, or external/unattributed control path.
- No placeholder strings, undefined future tasks, or unspecified implementation decisions remain in this plan.
