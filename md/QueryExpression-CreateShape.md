# CreateShape

## Description

`CreateShape(multiRow, func)`

Creates a closed polygon from the set of points

`CreateShape(lineSet)`

Creates a closed polygon from the linesets

### Return Type

`ShapeSetDouble` (see [Type Conversions](/docs/QueryExpression-Type))

## Parameters

| Parameter  | Required     | Type(s)               | Description                             | `null` Behavior |
| :--------- | :----------- | :-------------------- | :-------------------------------------- | :-------------- |
| `multiRow` | **Required** | `MultiPoint`, `Column<MultiPoint>` | The start point of the line.            | Returns `null`  |
| `func`     | **Required** | `Point`, `Column<XY>` | The end point of the line.              | Returns `null`  |
| `lineSet`  | **Required** | `Line` `Column<Line>` | The end point of the line.              | Divides the line at the 180th meridian. | Returns `null`  |

## Usage

`CreateShape` may be used in the query SELECT clauses for analyzing data and applying conditional logic.


```json
{
  "table": {
    "name": "test/allshapes"
  },
  "sqlselect": ["CreateShape(allshapes)"]
}
```

```json
{
  "take": 1,
  "table": {
    "name": "test/allshapes"
  },
  "sqlselect": [
    "CreateShape(LineFromWKT('(-112.11914062674626 41.420761289004076,-105.96679687674626 33.8460869780613,-91.90429687674626 34.137572929286634)'))"
  ]
}
```

```sql
SELECT CreateShape(allshapes) 
FROM test/allshapes;
```

```sql
SELECT CreateShape(LineFromWKT('(-112.11914062674626 41.420761289004076,-105.96679687674626 33.8460869780613,-91.90429687674626 34.137572929286634)')) 
FROM test/allshapes
LIMIT 1;
```

```js
const q = ml.query();
q.from("test/allshapes");
q.select("CreateShape(allshapes)");
```

```js
const q = ml.query();
q.from("test/allshapes");
q.select("CreateShape(LineFromWKT('(-112.11914062674626 41.420761289004076,-105.96679687674626 33.8460869780613,-91.90429687674626 34.137572929286634)'))");
q.take(1);
```

(-89.35546875174626 45.99187335267636),(-91.64062500174623 38.38759827687156),(-79.68750000174624 40.22362624553127),(-89.35546875174626 45.99187335267636)
