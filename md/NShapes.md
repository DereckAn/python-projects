![Tag](https://img.shields.io/badge/-GEO-brightgreen)

# NShapes

## Description

`NShapes( geoObj, includeHoles )`

Returns the number of shapes inside the shapeset (default excludes the holes).

**geoObj** - The geometry data

**includeHoles** - Only applicable to polygons. If true, this will count a hole as a shape (default false)
Optional. Default: False

### Return Type

`List of Numbers`

<span style='color:red'> -- ask later</span>

## Parameters

| Parameter |              | Type(s)           | Description                                    | `null` Behavior |
| --------- | ------------ | ----------------- | ---------------------------------------------- | --------------- |
| geoObj    | **required** | <code>ShapeSetDouble</code> | The geometry to count the number of shapes in. |
| includeHoles    | **optional** | <code>Boolean</code> | Only applicable to polygons.

## Usage

`NShapes` may be used in the query `select` and `where` clauses.

### JSON

```json
{
    "table": {
        "name": "Miami/Count_Land_Outline"
    },
    "sqlselect": [
        "NShapes(Count_Land_Outline)"
    ]
}
```

```json
{
    "table": {
        "name": "Miami/Count_Land_Outline"
    },
    "where": [
        [
            {
                "exp": "NShapes(Count_Land_Outline) >= 1"
            }
        ]
    ],
    "sqlselect": [
        "NShapes(Count_Land_Outline)"
    ]
}
```

### SQL

```sql
SELECT NShapes(Count_Land_Outline) as NumberOfShapes
FROM Miami/Count_Land_Outline
```

```sql
SELECT Count_Land_Outline
FROM Miami/Count_Land_Outline
WHERE NShapes(Count_Land_Outline) >= 1
```

### JavaScript

```javascript
const q = ml.query();
q.from("Miami/Count_Land_Outline");
q.select("Count_Land_Outline");
```

```javascript
const q = ml.query();
q.from("Miami/Count_Land_Outline");
q.where("NShapes(Count_Land_Outline) >= 1");
q.select("Count_Land_Outline");
```
