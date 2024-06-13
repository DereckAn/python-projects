# Stitch

## Description

`Stitch()`

- Takes multiple grouped MultiPoint geometries, and stitches them into a single MultiPoint geometry in the order provided by the query. Returns null if no geometries in the grouping.

### Return Type

`ShapeSetDouble`

## Parameters

| Parameter  | Required     | Type(s)            | Description                         | `null` Behavior |
| :--------- | :----------- | :----------------- | :---------------------------------- | :-------------- |
| `multiRow` | **Required** | `List<int>`        | The first shapeset to be stitched.  | Returns `null`  |
| `mptFunc`  | **Required** | `MultipointDouble` | The second shapeset to be stitched. | Returns `null`  |

## Usage

`Stitch` may be used in the query `select` and `where` clauses.

## Examples

### JSON

```json
{
  "table": {
    "name": "test/hotels"
  },
  "sqlselect": ["Stitch(Hotels, Hotels)"]
}
```

```json
{
  "table": {
    "name": "test/hotels"
  },
  "sqlselect": ["Stitch(Hotels, Hotels, 0.01)"]
}
```

```json
{
  "table": {
    "name": "test/hotels"
  },
  "sqlselect": ["Stitch(Hotels, Hotels, 0.01, 100)"]
}
```

```json
{
  "table": {
    "name": "test/hotels"
  },
  "sqlselect": ["Stitch(Hotels, Hotels, 0.01, 100, 1000)"]
}
```

### SQL

```sql
SELECT Stitch(Hotels, Hotels) AS StitchedShapes
FROM test.hotels;
```

```sql
SELECT Stitch(Hotels, Hotels, 0.01) AS StitchedShapes
FROM test.hotels;
```

```sql
SELECT Stitch(Hotels, Hotels, 0.01, 100) AS StitchedShapes
FROM test.hotels;
```

```sql
SELECT Stitch(Hotels, Hotels, 0.01, 100, 1000) AS StitchedShapes
FROM test.hotels;
```

### JavaScript

```js
const q = ml.query();
q.from("test/hotels");
q.select("Stitch(Hotels, Hotels) AS StitchedShapes");
```

```js
const q = ml.query();
q.from("test/hotels");
q.select("Stitch(Hotels, Hotels, 0.01) AS StitchedShapes");
```

```js
const q = ml.query();
q.from("test/hotels");
q.select("Stitch(Hotels, Hotels, 0.01, 100) AS StitchedShapes");
```

```js
const q = ml.query();
q.from("test/hotels");
q.select("Stitch(Hotels, Hotels, 0.01, 100, 1000) AS StitchedShapes");
```

