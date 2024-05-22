![Tag](https://img.shields.io/badge/-GEO-brightgreen)

# NPoints

## Description

`NPoints( geoObj )`

Returns the number of points inside the geometry.

**geoObj** - The geometry data

### Return Type

`Number`

## Parameters

| Parameter |              | Type(s)           | Description                                    | `null` Behavior |
| --------- | ------------ | ----------------- | ---------------------------------------------- | --------------- |
| geoObj    | **required** | <code>Poly</code> | The geometry to count the number of points in. |

<span style='color:red'> -- ask later about the example and about type in the table </span>

Poly example
: "MULTIPOLYGON(((-80.33167719841 25.752285027612, -80.3309141099453 25.7523212648234, -80.3308443725109 25.7505238858123, -80.3308537602425 25.7502871322486, -80.3316020965576 25.750264181623, -80.3316530585289 25.7516206767768, -80.33167719841 25.752285027612)))",

## Usage

`NPoints` may be used in the query `select` and `where` clauses.

### JSON

```json
{
  "table": {
    "name": "Miami/Census_Block_2020"
  },
  "sqlselect": ["NPoints(Census_Block_2020)"]
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
        "exp": "NPoints(Census_Block_2020) >= 1000"
      }
    ]
  ],
  "sqlselect": ["NPoints(Census_Block_2020)"]
}
```

### SQL

```sql
SELECT NPoints(Census_Block_2020) as NumberOfPoints
FROM Miami.Census_Block_2020;
```

```sql
SELECT NPoints(Census_Block_2020) as NumberOfPoints
FROM Miami.Census_Block_2020
WHERE NPoints(Census_Block_2020) >= 1000;
```

### JavaScript

```javascript
const q = ml.query();
q.from("Miami/Census_Block_2020");
q.select("NPoints(Census_Block_2020)");
```

```javascript
const q = ml.query();
q.from("Miami/Census_Block_2020");
q.where("NPoints(Census_Block_2020) >= 1000");
q.select("NPoints(Census_Block_2020)");
```

### C#

```csharp
var q = new Query();
q.From("Miami/Census_Block_2020");
q.Select("NPoints(Census_Block_2020)");
```

```csharp
var q = new Query();
q.From("Miami/Census_Block_2020");
q.Where("NPoints(Census_Block_2020) >= 1000");
q.Select("NPoints(Census_Block_2020)");
```