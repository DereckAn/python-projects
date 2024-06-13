# CreateLine

## Description

`CreateLine(mpt)` - Converts the provided Multipoint geometry into a Line. Returns null if input geometry is null.

`CreateLine(distanceLimit, unit)` - Creates lines from the set of points, after sorting the points by the values in the second column and splitting according to the distance limit specified.

`CreateLine(timeSpan, distanceLimit, unit)` - Creates lines from the set of points, after sorting the points by the values in the second column and splitting according to a combination of the time span specified and the distance limit.

`CreateLine(timeSpan)` - Creates a lines from the set of points, after sorting the points by the values in the second column and splitting according to the time span specified.

`CreateLine(timeSpan, distanceLimit, distanceThreshold, unit)` - Creates lines from the set of points, after sorting the points by the values in the second column and splitting according to a combination of the time span specified and the distance limit.

`CreateLine ( )` - Creates a line from the set of points, after sorting the points by the values in the second column.

`CreateLine(from, to, splitAtDateLine)` - Creates a line from two points.

### Return Type

`Line`

## Parameters

| Parameter           | Required | Type(s)       | Description                  | `null` Behavior        |
| :------------------ | :------- | :------------ | :--------------------------- | :--------------------- |
| `unit`              | Optional | `Point`, `XY` | The start point of the line. | Defaults `k`           |
| `distanceThreshold` | Optional | `Point`, `XY` | The end point of the line.   | Defaults `cumualitive` |
| `splitAtDateLine`   | Optional | `Point`, `XY` | The end point of the line.   | default `True`       |

## Usage

`CreateLine` may be used in the query SELECT and WHERE clauses for analyzing data and applying conditional logic.

### Notes

- `CreateLine` only works within `Line` range of values (i.e. -2,147,483,648 to 2,147,483,647)

## Examples

### JSON

#### Create a line from a point to another point

- This example creates a line from a point to another point.

```json
{
  "table": {
    "name": "test/hotels"
  },
  "sqlselect": [
    "CreateLine(Point(12.345453, -34.56778), Point(12.345453, -34.56778))"
  ]
}
```

### SQL

```sql
SELECT CreateLine(Point(12.345453, -34.56778), Point(12.345453, -34.56778)) AS Line
FROM test.hotels;
```

### JavaScript

```js
const q = ml.query();
q.from("test/hotels");
q.select(
  "CreateLine(Point(12.345453, -34.56778), Point(12.345453, -34.56778)) AS Line"
);
```

### C

```csharp
var q = new Query();
q.From("test/hotels");
q.Select("CreateLine(Point(12.345453, -34.56778), Point(12.345453, -34.56778)) AS Line");
```
