# GeoDistance

## Description

`GeoDistance(a, b, unit)`

- Calculates the distance between two shapes. By default the result is in meters.

### Return Type

`Double`

## Parameters

| Parameter | Required     | Type(s)  | Description                                                                           | `null` Behavior |
| :-------- | :----------- | :------- | :------------------------------------------------------------------------------------ | :-------------- |
| `a`       | **Required** | `Double` | The latitude of the first point.                                                      | Returns `null`  |
| `b`       | **Required** | `Double` | The longitude of the first point.                                                     | Returns `null`  |
| `unit`    | Optional     | `Double` | Unit of measurement: m (meters), km (kilometers), mi (miles), or nmi (Nautical miles) | Defaults to `m` |

## Usage

`GeoDistance` may be used in the query `select` and `where` clauses.

## Examples

### JSON

#### Calculates the distance between two points

- This example calculates the distance between two points.

```json
{
  "table": {
    "name": "test/hotels"
  },
  "sqlselect": [
    "GeoDistance(Latitude, Longitude, 38.370205476911806, -75.57013660669327) as Distance"
  ]
}
```
```json
{
  "table": {
    "name": "test/hotels"
  },
  "sqlselect": [
     "GeoDistance(Point(25.751234, -80.12345), Point(26.2345, -79.234))"
  ]
}
```
```json
{
  "table": {
    "name": "test/hotels"
  },
  "where": [
    [
      {
        "exp": "GeoDistance(Latitude, Longitude, 38.370205476911806, -75.57013660669327) < 10"
      }
    ]
  ],
  "sqlselect": ["Address", "CityID"]
}
```



### SQL

```sql
SELECT GeoDistance(Latitude, Longitude, 38.370205476911806, -75.57013660669327) as Distance
FROM test.hotels;
```

```sql
SELECT Address, CityID
FROM test.hotels
WHERE GeoDistance(Latitude, Longitude, 38.370205476911806, -75.57013660669327) < 10;
```

### JavaScript

```js
const q = ml.query();
q.from("test/hotels");
q.select(
  "GeoDistance(Latitude, Longitude, 38.370205476911806, -75.57013660669327) as Distance"
);
```

```js
const q = ml.query();
q.from("test/hotels");
q.select("Address", "CityID");
q.where(
  "GeoDistance(Latitude, Longitude, 38.370205476911806, -75.57013660669327) < 10"
);
```

### C

```csharp
var q = new Query();
q.From("test/hotels");
q.Select("GeoDistance(Latitude, Longitude, 38.370205476911806, -75.57013660669327) as Distance");
```

```csharp
var q = new Query();
q.From("test/hotels");
q.Select("Address", "CityID");
q.Where("GeoDistance(Latitude, Longitude, 38.370205476911806, -75.57013660669327) < 10");
```
