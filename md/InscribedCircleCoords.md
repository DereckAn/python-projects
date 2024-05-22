![Tag](https://img.shields.io/badge/-GEO-brightgreen)

# InscribedCircleCoords

## Description

`InscribedCircleCoords ( shapeset, precision )`

Calculates the position of the largest inscribed circle for a given shapeset.

**shapeset** - The shapeset to operate on

**precision** - Precision of the grid to use in the creation of the inscribed circle. Smaller values will be more precisce and increase computation time. Use zero to auto-calculate a best guess on size. Negative values are not allowed. Default: 0.0001
Optional. Default: 0.0001

## Return Type

`List<Coordinate>`

## Parameters

| Parameter          |              | Type(s)                                      | Description                | `null` Behavior |
| ------------------ | ------------ | -------------------------------------------- | -------------------------- | --------------- |
| shapeset or geoObj | **required** | <code>Coordinate</code> or <code>Poly</code> | The shapeset to operate on |
| precision          | **optional** | <code>Number</code>                          | The radius of the circle.  |

<span style='color:red'> -- ask later about the example and about type in the table </span>

## Usage

`InscribedCircleCoords` may be used in the query `select` clauses.

<span style='color:red'> -- ask later about the where clause and how to use it </span>


### JSON

```json
{
  "table": {
    "name": "Miami/Census_Block_2020"
  },
  "sqlselect": ["InscribedCircleCoords(Census_Block_2020)"]
}
```

### SQL

```sql
SELECT InscribedCircleCoords(Census_Block_2020) FROM Miami.Census_Block_2020
```

### JavaScript

```javascript
const q = ml.query();
q.from("Miami/Census_Block_2020");
q.select("InscribedCircleCoords(Census_Block_2020)");
```
