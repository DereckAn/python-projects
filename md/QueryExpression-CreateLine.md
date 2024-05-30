# CreateLine

## Description

`CreateLine ( mpt )`

Converts the provided Multipoint geometry into a Line. Returns null if input geometry is null.

`CreateLine ( distanceLimit, unit )`

Creates lines from the set of points, after sorting the points by the values in the second column and splitting according to the distance limit specified.

**distanceLimit**

**unit** - Optional. Default: K

`CreateLine ( timeSpan, distanceLimit, unit )`

Creates lines from the set of points, after sorting the points by the values in the second column and splitting according to a combination of the time span specified and the distance limit.
**timeSpan**

**distanceLimit**	

**unit** - Optional. Default: K

`CreateLine ( timeSpan )`

Creates a lines from the set of points, after sorting the points by the values in the second column and splitting according to the time span specified.

`CreateLine ( timeSpan, distanceLimit, distanceThreshold, unit )`

Creates lines from the set of points, after sorting the points by the values in the second column and splitting according to a combination of the time span specified and the distance limit.

**timeSpan**	

**distanceLimit**	

**distanceThreshold** - Optional. Default: cumulative

**unit** - Optional. Default: K

`CreateLine ( )`

Creates a line from the set of points, after sorting the points by the values in the second column.

`CreateLine ( from, to, splitAtDateLine )`

Creates a line from two points.

**from**	

**to**	

**splitAtDateLine** - Optional. Default: True



### Return Type

`Line`

## Parameters

| Parameter |              | Type(s)                     | Description                           | `null` Behavior |
| --------- | ------------ | --------------------------- | ------------------------------------- | --------------- |
| geoObj    | **required** | <code>ShapeSetDouble</code> | The geometry to create the line from. |

## Usage

`CreateLine` may be used in the query `select` and `where` clauses.`

### JSON

```json
{
  "table": {
    "name": "Miami/Census_Block_2020"
  },
  "sqlselect": ["CreateLine(Census_Block_2020)"]
}
```

```json
{
  "table": {
    "name": "Miami/Census_Block_2020"
  },
  "where": [
    [
      {
        "exp": "CreateLine(Census_Block_2020) = Census_Block_2020"
      }
    ]
  ],
  "sqlselect": ["CreateLine(Census_Block_2020)"]
}
```

### SQL

```sql
SELECT CreateLine(Census_Block_2020) as Line
FROM Miami.Census_Block_2020;
```

```sql
SELECT Census_Block_2020
FROM Miami.Census_Block_2020
WHERE CreateLine(Census_Block_2020) = Census_Block_2020;
```

### JavaScript

```javascript
const q = ml.query();
q.from("Miami/Census_Block_2020");
q.select("CreateLine(Census_Block_2020)");
```

```javascript
const q = ml.query();
q.from("Miami/Census_Block_2020");
q.where("CreateLine(Census_Block_2020) = Census_Block_2020");
q.select("CreateLine(Census_Block_2020)");
```

### C

```csharp
var q = new Query();
q.From("Miami/Census_Block_2020");
q.Select("CreateLine(Census_Block_2020)");
```

```csharp
var q = new Query();
q.From("Miami/Census_Block_2020");
q.Where("CreateLine(Census_Block_2020) = Census_Block_2020");
q.Select("CreateLine(Census_Block_2020)");
```
