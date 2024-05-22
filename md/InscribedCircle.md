<!-- ![Tag](https://img.shields.io/badge/-GEO-brightgreen) -->

# InscribedCircle

## Description

`InscribedCircle ( shapeset, precision )`

Creates the largest inscribed circle for a given shapeset.

**shapeset** - The shapeset to operate on

**precision** - Precision of the grid to use in the creation of the inscribed circle. Smaller values will be more precisce and increase computation time. Use zero to auto-calculate a best guess on size. Negative values are not allowed. Default: 0.0001
Optional. Default: 0.0001

### Return Type

`Poly`
<span style='color:red'> -- ask later about return </span>


## Parameters

| Parameter |              | Type(s)           | Description                                  | `null` Behavior |
| --------- | ------------ | ----------------- | -------------------------------------------- | --------------- |
| geoObj    | **required** | <code>Poly</code> | The geometry to get the inscribed circle of. |
| precision | **optional** | <code>float</code> | Precision of the grid to use in the creation of the inscribed circle.

<span style='color:red'> -- ask later about the example and about type in the table </span>

Poly example
: "MULTIPOLYGON(((-80.33167719841 25.752285027612, -80.3309141099453 25.7523212648234, -80.3308443725109 25.7505238858123, -80.3308537602425 25.7502871322486, -80.3316020965576 25.750264181623, -80.3316530585289 25.7516206767768, -80.33167719841 25.752285027612)))",

## Usage

`InscribedCircle` may be used in the query `select` and `where` clauses.

### JSON

```json
{
  "table": {
    "name": "Miami/Census_Block_2020"
  },
  "sqlselect": ["InscribedCircle(Census_Block_2020)"]
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
                "exp": "InscribedCircle(Census_Block_2020,0) = Census_Block_2020"
            }
        ]
    ],
    "sqlselect": [
        "InscribedCircle(Census_Block_2020,0)"
    ]
}
```

### SQL

```sql
SELECT InscribedCircle(Census_Block_2020) as InscribedCircle
FROM Miami.Census_Block_2020;
```

```sql
SELECT Census_Block_2020
FROM Miami.Census_Block_2020
WHERE InscribedCircle(Census_Block_2020) = Census_Block_2020;
```

### JavaScript

```javascript
const q = ml.query();
q.from("Miami/Census_Block_2020");
q.select("InscribedCircle(Census_Block_2020)");
```

```javascript
const q = ml.query();
q.from("Miami/Census_Block_2020");
q.where("InscribedCircle(Census_Block_2020) = Census_Block_2020");
q.select("InscribedCircle(Census_Block_2020)");
```

### C#

```
var q = new Query();
q.From("Miami/Census_Block_2020");
q.Select("InscribedCircle(Census_Block_2020)");
```

```
var q = new Query();
q.From("Miami/Census_Block_2020");
q.Where("InscribedCircle(Census_Block_2020) = Census_Block_2020");
q.Select("InscribedCircle(Census_Block_2020)");
```
