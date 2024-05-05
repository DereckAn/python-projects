var lineStringExample =
  "LINESTRING(-113.994140625 42.00032514831619,-111.09375000000001 41.96765920367815,-111.04980468750001 41.07935114946898,-109.07226562500001 41.04621681452065,-109.0283203125 37.020098201368114,-113.994140625 37.020098201368114,-113.994140625 41.934976500546554)";

function getCoordenates(lineString) {
  // delete "LINESTRING(" at the beginning and ")" at the end, and modify the string ","
  var coords = lineString.slice(11, -1).split(",");

  // Convierte [longitud, latitud]
  var coordList = coords.map((coord) => {
    var parts = coord.trim().split(" ");
    return [parseFloat(parts[1]), parseFloat(parts[0])];
  });

  console.log("lista de coordenadas:", coordList);
  console.log("Tipo de dato de coordList:", typeof coordList);
  return coordList;
}

function rotate(origin, point, angle) {
  let radians = angle * Math.PI / 180.0;
  let nx = Math.cos(radians) * (point[0]-origin[0]) - Math.sin(radians) * (point[1]-origin[1]) + origin[0];
  let ny = Math.sin(radians) * (point[0]-origin[0]) + Math.cos(radians) * (point[1]-origin[1]) + origin[1];
  return [nx, ny];
}

function rotateLineString(lineString) {
  let lonList = [];
  let latList = [];
  lineString.forEach((coord) => {
    lonList.push(parseFloat(coord[0]));
    latList.push(parseFloat(coord[1]));
  });

  let origin = [(Math.max(...lonList) + Math.min(...lonList)) / 2, (Math.max(...latList) + Math.min(...latList)) / 2];
  let rotatedCoords = lineString.map((coord) => rotate(origin, coord, 90));


  console.log("lista de longitudes:", lonList);
  console.log("lista de latitudes:", latList);
  console.log("punto de origen:", origin);
  console.log(typeof origin);
  console.log("maximo de longitudes:", Math.max(...lonList));
  console.log("minimo de longitudes:", Math.min(...lonList));
  console.log("maximo de latitudes:", Math.max(...latList));
  console.log("minimo de latitudes:", Math.min(...latList));
  console.log("coordenadas rotadas:", rotatedCoords);
  return { lonList, latList, rotatedCoords };
}

function convertLineStringToObj(newCoords) {
  
  return {
    "data":[
      [
        newCoords,
        "Lines"
      ]
    ],
    "columns": [{"name": "geom"}, {"name":"name"}]
  };
}

let rotatedCoords = rotateLineString(getCoordenates(lineStringExample));
let lineObj = convertLineStringToObj(rotatedCoords);

console.log(lineObj)
