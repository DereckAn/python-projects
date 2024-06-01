Difference

## Description

`Difference(a, b)`

- Returns a shape of `a` where the overlap with shape `b` has been removed.

### Return Type

`ShapeSetDouble`, `Poly`

## Parameters

| Parameter | Required   | Type(s)                  | Description                                                                | `null` Behavior |
| --------- | ---------- | ------------------------ | -------------------------------------------------------------------------- | --------------- |
| a         | `Required` | `ShapeSetDouble`, `Poly` | Object containing a geometrical shape built by several geometrical points. | Returns `null`  |
| b         | `Required` | `ShapeSetDouble`, `Poly` | Object containing a geometrical shape built by several geometrical points. | Returns `null`  |

## Usage

`Difference` may be used in the query `select` and `where` clauses.

### JSON

```json
{
  "table": {
    "name": "Miami/Census_Block_2020"
  },
  "sqlselect": ["Difference(Census_Block_2020, Census_Block_2020)"]
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
        "exp": "Difference(Census_Block_2020, Census_Block_2020) != Census_Block_2020"
      }
    ]
  ],
  "sqlselect": ["Difference(Census_Block_2020, Census_Block_2020)"]
}
```

### SQL

```sql
SELECT Difference(Census_Block_2020, Census_Block_2020) as Difference
FROM Miami.Census_Block_2020;
```

```sql
SELECT Difference(Census_Block_2020, Census_Block_2020) as Difference
FROM Miami.Census_Block_2020
WHERE Difference(Census_Block_2020, Census_Block_2020) != Census_Block_2020;
```

### JavaScript

```javascript
const q = ml.query();
q.from("Miami/Census_Block_2020");
q.select("Difference(Census_Block_2020, Census_Block_2020)");
```

```javascript
const q = ml.query();
q.from("Miami/Census_Block_2020");
q.where(
  "Difference(Census_Block_2020, Census_Block_2020) != Census_Block_2020"
);
q.select("Difference(Census_Block_2020, Census_Block_2020)");
```
