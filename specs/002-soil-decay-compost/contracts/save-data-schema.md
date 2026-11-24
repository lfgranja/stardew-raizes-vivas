# Save Data Schema Contract

**Feature Branch**: `002-soil-decay-compost` | **Date**: 2026-09-06

## Overview

This contract defines the JSON schemas for persisting composting bin state and soil health data through save/load cycles.

## Soil Health Data (Existing)

### Key Format
```
soil_health_data_{sanitizedSaveId}
```

### JSON Schema
```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "type": "object",
  "properties": {
    "LocationHealthData": {
      "type": "object",
      "patternProperties": {
        "^[a-zA-Z0-9_]{1,100}$": {
          "type": "object",
          "patternProperties": {
            "^-?\\d{1,5},-?\\d{1,5}$": {
              "type": "number",
              "minimum": 0,
              "maximum": 100
            }
          },
          "additionalProperties": false
        }
      },
      "additionalProperties": false
    }
  },
  "required": ["LocationHealthData"]
}
```

### Example
```json
{
  "LocationHealthData": {
    "Farm": {
      "10,20": 85.0,
      "10,21": 72.5,
      "11,20": 90.0
    },
    "Greenhouse": {
      "5,5": 100.0
    }
  }
}
```

---

## Composting Bin Data (New)

### Key Format
```
composting_bins_{sanitizedSaveId}_{locationName}
```

### JSON Schema
```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "type": "object",
  "properties": {
    "LocationBinData": {
      "type": "object",
      "patternProperties": {
        "^[a-zA-Z0-9_]{1,100}$": {
          "type": "object",
          "patternProperties": {
            "^-?\\d{1,5},-?\\d{1,5}$": {
              "type": "object",
              "properties": {
                "State": {
                  "type": "string",
                  "enum": ["Empty", "Processing", "Ready"]
                },
                "InputItemId": {
                  "type": ["string", "null"]
                },
                "InputTimestamp": {
                  "type": ["integer", "null"]
                },
                "MaturationLevel": {
                  "type": "integer",
                  "minimum": 1,
                  "maximum": 5
                },
                "ConsecutiveIdleDays": {
                  "type": "integer",
                  "minimum": 0,
                  "maximum": 14
                }
              },
              "required": ["State", "MaturationLevel", "ConsecutiveIdleDays"],
              "additionalProperties": false
            }
          },
          "additionalProperties": false
        }
      },
      "additionalProperties": false
    }
  },
  "required": ["LocationBinData"]
}
```

### Example
```json
{
  "LocationBinData": {
    "Farm": {
      "15,25": {
        "State": "Processing",
        "InputItemId": "Object_(o)178",
        "InputTimestamp": 1200,
        "MaturationLevel": 3,
        "ConsecutiveIdleDays": 0
      },
      "15,26": {
        "State": "Ready",
        "InputItemId": "Object_(o)178",
        "InputTimestamp": 960,
        "MaturationLevel": 2,
        "ConsecutiveIdleDays": 0
      },
      "16,25": {
        "State": "Empty",
        "InputItemId": null,
        "InputTimestamp": null,
        "MaturationLevel": 1,
        "ConsecutiveIdleDays": 5
      }
    }
  }
}
```

---

## Validation Rules

### On Load
1. Validate JSON structure matches schema
2. Clamp `MaturationLevel` to [1, 5]
3. Clamp `ConsecutiveIdleDays` to [0, 14]
4. Validate `State` is one of: Empty, Processing, Ready
5. If `State == Processing`:
   - Calculate elapsed time from `InputTimestamp`
   - If elapsed ≥ 2880 minutes: Set `State = Ready`
6. If `State == Empty`:
   - Clear `InputItemId` and `InputTimestamp`

### On Save
1. Only serialize non-default states (sparse storage)
2. Skip bins with `State == Empty` AND `MaturationLevel == 1` AND `ConsecutiveIdleDays == 0`
3. Serialize all `Processing` and `Ready` bins
4. Serialize `Empty` bins with non-default maturation or idle days

---

## Migration

### From No Previous Data
- First load: No composting bin data exists
- Initialize empty dictionary
- No migration needed

### From Future Versions
- Schema version field to be added if breaking changes occur
- Unknown fields ignored (forward compatibility)
- Missing fields use defaults (backward compatibility)

---

## Size Limits

| Limit | Value | Rationale |
|-------|-------|-----------|
| Max bins per location | 500 | Matches `MaxTilesPerLocation` |
| Max locations per save | 50 | Matches `MaxLocationsPerSave` |
| Max bins per save | 30000 | Matches `MaxTilesPerSave` |
| Max key length | 200 chars | Matches `MaxDataKeyLength` |

---

## Error Handling

| Error | Response |
|-------|----------|
| Invalid JSON | Log error, initialize empty |
| Schema violation | Log warning, skip invalid entries |
| Missing required fields | Use defaults, log warning |
| Corrupted tile coordinates | Skip entry, log warning |
| Negative timestamp | Reset to Empty state |
| Excessive maturation | Clamp to 5, log warning |
