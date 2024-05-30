# GeoDistance

## Description

`GeoDistance ( a, b, unit )`

Calculates the distance between two shapes. By default the result is in meters.

### Return Type
`Double`

## Parameters
| Parameter  | Required | Accepted Types     | Description                                                  | `null` Behavior                     |
|------------|--------------|----------|--------------------------------------------------------------|-------------------------------------|
| `a`    | **Required** | `Shape` | The first shape to compare.
| `b`    | **Required** | `Shape` | The second shape to compare.
| `unit`    | **Optional** | `String` | Unit of measurement: m (meters), km (kilometers), mi (miles), or nmi (Nautical miles).

## Usage

### JSON
```json
{
    "table": {
        "name": "test/hotels"
    },
    "sqlselect": [
        "GeoDistance(ShapeFromWKT('POINT(10.98 7.9)'), ShapeFromWKT('POINT(11.98 8.9)'))"
    ]
}
```
