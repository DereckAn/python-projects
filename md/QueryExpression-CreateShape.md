# CreateShape

## Description
`CreateShape(multiRow, func)`

Creates a closed polygon from the set of points

`CreateShape(lineSet)`

Creates a closed polygon from the linesets

### Return Type
`ShapeSetDouble` (see [Type Conversions](/docs/QueryExpression-Type))

## Parameters

| Parameter  | Required     | Type(s)                            | Description                                                 | `null` Behavior |
| :--------- | :----------- | :--------------------------------- | :---------------------------------------------------------- | :-------------- |
| `multiRow` | **Required** | `MultiPoint`, `Column<MultiPoint>` | This is a multipoint but it will be transfromed to a shape. | Returns `null`  |
| `func`     | **Required** | `Point`, `Column<XY>`              | The end point of the line.                                  | Returns `null`  |
| `lineSet`  | **Required** | `Line` `Column<Line>`              | This is a line set but it will be transfromed to a shape.   | Returns `null`  |

## Usage
`CreateShape` may be used in the query SELECT clauses for analyzing data and applying conditional logic.

### Notes
- `CreateShape` must have at least three points.
