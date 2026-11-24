# Remediation: Boundary Value Specification Conflict

**Feature**: Soil Health Visualization (001-soil-health-visualization)
**Date**: 2026-09-05
**Status**: Proposed

---

## 1. Contradiction Analysis

### 1.1 Source Documents

| Document | Location | Definition |
|----------|----------|------------|
| spec.md (Clarification) | Line 96 | "Poor: 0-33, Moderate: 34-66, Healthy: 67-100 (even thirds)" |
| spec.md (Key Entities) | Line 143 | "Poor (0-33), Moderate (34-66), Healthy (67-100)" |
| data-model.md (HealthCategory) | Lines 57-62 | Poor: 0-33, Moderate: 34-66, Healthy: 67-100 |
| data-model.md (Boundary Handling) | Line 64 | "Values at exact boundaries (33, 66) belong to the lower category" |
| IColorInterpolationService.md | Interpolation Rules | Range 0-33: t=value/33, Range 34-66: t=(value-34)/33, Range 67-100: Solid |

### 1.2 Boundary Value Analysis

| Value | Category (from ranges) | Boundary Rule | Interpolation Result | Conflict? |
|-------|------------------------|---------------|---------------------|-----------|
| 0 | Poor | N/A | t=0/33=0.0 → Pure Poor (red) | ✓ Consistent |
| 33 | Poor (0-33) | Lower category = Poor | t=33/33=1.0 → Pure Moderate (yellow) | ✗ **CONFLICT** |
| 34 | Moderate (34-66) | N/A | t=(34-34)/33=0.0 → Pure Moderate (yellow) | ✓ Consistent |
| 66 | Moderate (34-66) | Lower category = Moderate | t=(66-34)/33≈0.97 → Near Healthy (yellow-green) | ⚠ Partial |
| 67 | Healthy (67-100) | N/A | Solid Healthy (green) | ✓ Consistent |
| 100 | Healthy (67-100) | N/A | Solid Healthy (green) | ✓ Consistent |

### 1.3 Core Contradiction

**At value 33:**
- Category assignment: Poor (from range 0-33)
- Interpolation result: Pure Moderate color (yellow)
- **Result**: A tile categorized as "Poor" renders in yellow (Moderate color)

This is a **category-color mismatch**. The boundary handling rule ("lower category") is consistent with the range definition, but the interpolation formula `t = value / 33` causes value 33 to reach the Moderate color endpoint.

### 1.4 Root Cause

The interpolation rules assume **half-open ranges** `[0, 34)`, `[34, 67)`, `[67, 100]` but the spec defines **closed integer ranges** `[0, 33]`, `[34, 66]`, `[67, 100]`. The off-by-one discrepancy at the boundaries creates the contradiction.

### 1.5 Additional Ambiguities

1. **Non-integer values**: What category does 33.5 belong to? The integer ranges don't address this.
2. **Boundary at 66**: The interpolation reaches ~97% toward Healthy, but the category remains Moderate. This is visually confusing.
3. **"Even thirds" claim**: 101 values (0-100) divided by 3 = 33.67, so perfect even division is impossible with integers.

---

## 2. Proposed Harmonized Definition

### 2.1 Authoritative Definition

Use **half-open intervals** aligned with interpolation boundaries:

| Category | Range (half-open) | Integer Values | Threshold Rule |
|----------|-------------------|----------------|----------------|
| Poor | [0, 34) | 0-33 | `value < 34` |
| Moderate | [34, 67) | 34-66 | `34 ≤ value < 67` |
| Healthy | [67, 100] | 67-100 | `value ≥ 67` |

### 2.2 Justification

1. **Alignment with interpolation**: The interpolation formula `t = value / 34` for the first range naturally maps [0, 34) → [0, 1]
2. **Clean boundary transitions**: At value 34, interpolation reaches Moderate color exactly as category changes
3. **Preserves "even thirds" intent**: Integer ranges 0-33, 34-66, 67-100 remain unchanged for display purposes
4. **Handles non-integer values**: 33.5 correctly maps to Poor category (33.5 < 34)

### 2.3 Resolved Boundary Analysis

| Value | Category | Interpolation | Result |
|-------|----------|---------------|--------|
| 0 | Poor | t=0/34=0.0 → Pure Poor | ✓ Red |
| 33 | Poor | t=33/34≈0.97 → Near Moderate | ✓ Orange-red |
| 33.5 | Poor | t=33.5/34≈0.985 → Near Moderate | ✓ Orange-red |
| 34 | Moderate | t=0/33=0.0 → Pure Moderate | ✓ Yellow |
| 66 | Moderate | t=32/33≈0.97 → Near Healthy | ✓ Yellow-green |
| 66.9 | Moderate | t=32.9/33≈0.997 → Near Healthy | ✓ Yellow-green |
| 67 | Healthy | Solid Healthy | ✓ Green |
| 100 | Healthy | Solid Healthy | ✓ Green |

---

## 3. Proposed Text Changes

### 3.1 spec.md Changes

#### Change 1: Clarification Session (Line 96)

**Current:**
```
- Q: What are the exact health value thresholds for Poor, Moderate, and Healthy categories? → A: Poor: 0-33, Moderate: 34-66, Healthy: 67-100 (even thirds)
```

**Proposed:**
```
- Q: What are the exact health value thresholds for Poor, Moderate, and Healthy categories? → A: Poor: 0-33 (value < 34), Moderate: 34-66 (34 ≤ value < 67), Healthy: 67-100 (value ≥ 67). Integer ranges for display; half-open intervals for computation.
```

#### Change 2: Key Entities - Color Mapping (Line 143)

**Current:**
```
- **Color Mapping**: The translation from a numeric health value to a visual color, using thresholds: Poor (0-33), Moderate (34-66), Healthy (67-100). Uses linear RGB interpolation between adjacent category colors.
```

**Proposed:**
```
- **Color Mapping**: The translation from a numeric health value to a visual color, using thresholds: Poor (0-33), Moderate (34-66), Healthy (67-100). Uses linear RGB interpolation between adjacent category colors. Boundaries use half-open intervals: [0, 34), [34, 67), [67, 100].
```

### 3.2 data-model.md Changes

#### Change 3: HealthCategory Table (Lines 57-62)

**Current:**
```
| Value | Name | Range | Default Color | Pattern |
|-------|------|-------|---------------|---------|
| 0 | Poor | 0-33 | Red (#FF0000) | Stripes |
| 1 | Moderate | 34-66 | Yellow (#FFFF00) | Dots |
| 2 | Healthy | 67-100 | Green (#00FF00) | Solid |
| 3 | Unknown | N/A | Gray (#808080) | None |
```

**Proposed:**
```
| Value | Name | Range | Default Color | Pattern |
|-------|------|-------|---------------|---------|
| 0 | Poor | [0, 34) | Red (#FF0000) | Stripes |
| 1 | Moderate | [34, 67) | Yellow (#FFFF00) | Dots |
| 2 | Healthy | [67, 100] | Green (#00FF00) | Solid |
| 3 | Unknown | N/A | Gray (#808080) | None |
```

#### Change 4: Boundary Handling Rule (Line 64)

**Current:**
```
**Boundary Handling**: Values at exact boundaries (33, 66) belong to the lower category. Interpolation occurs between adjacent category colors within each range.
```

**Proposed:**
```
**Boundary Handling**: Half-open interval rules apply: 33 → Poor, 34 → Moderate, 66 → Moderate, 67 → Healthy. Interpolation occurs between adjacent category colors within each range, reaching the next category color at the upper boundary.
```

### 3.3 IColorInterpolationService.md Changes

#### Change 5: Interpolation Rules Table

**Current:**
```
| Range | Category A | Category B | Interpolation |
|-------|------------|------------|---------------|
| 0-33 | Poor (0) | Moderate (34) | Lerp(Poor, Moderate, t) where t = value / 33 |
| 34-66 | Moderate (34) | Healthy (67) | Lerp(Moderate, Healthy, t) where t = (value - 34) / 33 |
| 67-100 | Healthy | Healthy | Solid Healthy color |
```

**Proposed:**
```
| Range | Category A | Category B | Interpolation |
|-------|------------|------------|---------------|
| [0, 34) | Poor (0) | Moderate (34) | Lerp(Poor, Moderate, t) where t = value / 34 |
| [34, 67) | Moderate (34) | Healthy (67) | Lerp(Moderate, Healthy, t) where t = (value - 34) / 33 |
| [67, 100] | Healthy | Healthy | Solid Healthy color |
```

#### Change 6: Interface Documentation

**Current (in XML doc):**
```csharp
/// <summary>
/// Gets the color for a health value with interpolation.
/// Uses category thresholds: Poor (0-33), Moderate (34-66), Healthy (67-100).
/// </summary>
```

**Proposed:**
```csharp
/// <summary>
/// Gets the color for a health value with interpolation.
/// Uses category thresholds: Poor [0, 34), Moderate [34, 67), Healthy [67, 100].
/// </summary>
```

---

## 4. Implementation Notes

### 4.1 Category Resolution Logic

```csharp
public HealthCategory GetCategoryForHealth(float healthValue)
{
    if (float.IsNaN(healthValue) || float.IsInfinity(healthValue))
        return HealthCategory.Unknown;
    
    return healthValue switch
    {
        < 34 => HealthCategory.Poor,
        < 67 => HealthCategory.Moderate,
        _ => HealthCategory.Healthy
    };
}
```

### 4.2 Interpolation Logic

```csharp
public Color GetColorForHealth(float healthValue)
{
    healthValue = Math.Clamp(healthValue, 0, 100);
    
    if (healthValue < 34)
    {
        float t = healthValue / 34;
        return Lerp(poorColor, moderateColor, t);
    }
    else if (healthValue < 67)
    {
        float t = (healthValue - 34) / 33;
        return Lerp(moderateColor, healthyColor, t);
    }
    else
    {
        return healthyColor;
    }
}
```

### 4.3 Display Formatting

For tooltips and UI display, continue using integer ranges:
- "Soil Health: 33% (Poor)" — not "Soil Health: 33% ([0, 34))"
- Category names remain "Poor", "Moderate", "Healthy"

---

## 5. Verification Checklist

- [ ] Value 0 → Poor category, Pure red color
- [ ] Value 33 → Poor category, Near-yellow color (transitioning)
- [ ] Value 33.5 → Poor category, Near-yellow color (transitioning)
- [ ] Value 34 → Moderate category, Pure yellow color
- [ ] Value 66 → Moderate category, Near-green color (transitioning)
- [ ] Value 66.9 → Moderate category, Near-green color (transitioning)
- [ ] Value 67 → Healthy category, Pure green color
- [ ] Value 100 → Healthy category, Pure green color
- [ ] NaN → Unknown category, Gray color
- [ ] All boundary values have consistent category-color mapping

---

## 6. Summary

The contradiction arises from misaligned range definitions between category assignment (closed integer ranges) and interpolation (half-open ranges). The remediation:

1. **Preserves** the "even thirds" integer ranges (0-33, 34-66, 67-100) for display
2. **Adds** half-open interval notation for computation: [0, 34), [34, 67), [67, 100]
3. **Fixes** the interpolation divisor from 33 to 34 for the first range
4. **Clarifies** the boundary handling rule with explicit examples

This ensures that:
- Category assignment and color interpolation are always consistent
- Non-integer values are handled correctly
- The visual transition smoothly reaches each category color at the boundary
