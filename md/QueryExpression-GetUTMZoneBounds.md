# GetUTMZoneBounds

## Description

`GetUTMZoneBounds(utm)`

Converts UTM grid zone to geographic bounds.

### Return Type

`ShapeSetDouble` (see [Type Conversions](/docs/QueryExpression-Type))

## Parameters

| Parameter | Required     | Type(s)  | Description                                                 | `null` Behavior |
| :-------- | :----------- | :------- | :---------------------------------------------------------- | :-------------- |
| `utm`     | **Required** | `String` | Full UTM grid zone with numeric zone and single character latitude band | `null`          |

## Usage

`GetUTMZoneBounds` may be used in the query SELECT clause.

### Notes

- The UTM zone bounds are calculated using the WGS84 coordinate system.
