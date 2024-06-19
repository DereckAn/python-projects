 ProjectedWKT

## Description
`ProjectedWKT ( point, projection, operation, operationMinX, operationMinY, operationMaxX, operationMaxY )`

Project the given point to the provided projection and return it as a WKT string.

### Return Type
`String` (see [Type Conversion](/docs/QueryExpression-Type))

## Parameters
| Parameter | Required | Type(s) | Description | `null` Behavior |
|:----------|:---------|:--------|:------------|:----------------|
| `point` | **Required** | `Point`, `Column<Point>` | The point to project. | Returns `null` |
| `projection` | **Required** | `Projection`, `Column<Projection>` | The projection to project the point to. | Returns `null` |
| `operation` | Optional | `Operation`, `Column<Operation>` | The operation to apply to the projected point. | Returns `null` |
| `operationMinX` | Optional | `Double`, `Column<Double>` | The minimum X value of the operation. | Returns `null` |
| `operationMinY` | Optional | `Double`, `Column<Double>` | The minimum Y value of the operation. | Returns `null` |
| `operationMaxX` | Optional | `Double`, `Column<Double>` | The maximum X value of the operation. | Returns `null` |
| `operationMaxY` | Optional | `Double`, `Column<Double>` | The maximum Y value of the operation. | Returns `null` |

## Usage
`ProjectedWKT` may be used in the query SELECT and WHERE clauses for analyzing data and applying conditional logic.

### Notes
- The `ProjectedWKT` function is useful for creating a WKT string from a projected point. It can be used in conjunction with the `ToWKT` function to create a WKT string from a point.

