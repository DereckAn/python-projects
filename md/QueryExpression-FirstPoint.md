![Tag](https://img.shields.io/badge/-GEO-brightgreen)

# LastPoint

## Description

`LastPoint ( mpt )`

Returns the last point in a MultiPoint geometry, or null if none

**mpt** - MultiPoint

## Return Type

`Coordinate`

## Parameters

| Parameter |              | Type(s) | Description | `null` Behavior |
| --------- | ------------ | ------- | ----------- | --------------- |
| mpt   | **required** |   `Column<Double>` |  Returns the last point in a MultiPoint geometry   |  None   |

## Usage

`LastPoint` may be used in the query `select` and `where` clauses.

<span style='color:red'> -- ask later about the where clause and how to use it </span>


### JSON

```json
{
  "table": {
    "name": "Miami/Census_Block_2020"
  },
  "sqlselect": ["LastPoint(Census_Block_2020)"]
}
```

### SQL

```sql
SELECT LastPoint(Census_Block_2020) FROM Miami.Census_Block_2020
```

### JavaScript

```javascript
const q = ml.query();
q.from("Miami/Census_Block_2020");
q.select("LastPoint(Census_Block_2020)");
```



[hello wolrd ](./QueryExpression-Floor.md)
