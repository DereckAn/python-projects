# DWithin

## Description

`DWithin ( point, xy2, projection, originalProjection, distance, shapeType, hash )`

Determines if a point is within the provided point, line, or polygon and the added distance

### Return Type

`Boolean`

## Parameters

| Parameter          | Required     | Type(s)                                          | Description                                         | `null` Behavior |
| ------------------ | ------------ | ------------------------------------------------ | --------------------------------------------------- | --------------- |
| point              | **Required** | <code>Point</code> &#124;&#124; <code> XY</code> | The point to be tested.                             | Returns `false` |
| xy2                | **Required** | <code>Point</code> &#124;&#124; <code> XY</code> | The point to be tested.                             | Returns `false` |
| projection         | **Optional** | <code>Projection</code>                          | The projection to use for the distance calculation. | Returns `false` |
| originalProjection | **Optional** | <code>Projection</code>                          | The projection to use for the distance calculation. | Returns `false` |
| distance           | **Optional** | <code>Double</code>                              | The distance to test.                               | Returns `false` |
| shapeType          | **Optional** | <code>ShapeType</code>                           | The shape type to test.                             | Returns `false` |
| hash               | **Optional** | <code>String</code>                              | The hash to test.                                   | Returns `false` |

## Usage

`DWithin` may be used in the query `select` and `where` clauses.

### JSON

```json
{
  "table": {
    "name": "Miami/Census_Block_2020"
  },
  "sqlselect": ["DWithin(Census_Block_2020,Census_Block_2020)"]
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
        "exp": "DWithin(Census_Block_2020,Census_Block_2020)"
      }
    ]
  ],
  "sqlselect": ["DWithin(Census_Block_2020,Census_Block_2020)"]
}
```

### SQL

```sql
SELECT DWithin(Census_Block_2020,Census_Block_2020) as DWithin
FROM Miami.Census_Block_2020;
```

```sql
SELECT Census_Block_2020
FROM Miami.Census_Block_2020
WHERE DWithin(Census_Block_2020,Census_Block_2020);
```

### JavaScript

```javascript
const q = ml.query();
q.from("Miami/Census_Block_2020");
q.select("DWithin(Census_Block_2020,Census_Block_2020)");
```

```javascript
const q = ml.query();
q.from("Miami/Census_Block_2020");
q.where("DWithin(Census_Block_2020,Census_Block_2020)");
q.select("DWithin(Census_Block_2020,Census_Block_2020)");
```

### C

```csharp
var q = new Query();
q.From("Miami/Census_Block_2020");
q.Select("DWithin(Census_Block_2020,Census_Block_2020)");
```

```csharp
var q = new Query();
q.From("Miami/Census_Block_2020");
q.Where("DWithin(Census_Block_2020,Census_Block_2020)");
q.Select("DWithin(Census_Block_2020,Census_Block_2020)");
```
