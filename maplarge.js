let lineStringExample =
  "LINESTRING(-113.994140625 42.00032514831619,-111.09375000000001 41.96765920367815,-111.04980468750001 41.07935114946898,-109.07226562500001 41.04621681452065,-109.0283203125 37.020098201368114,-113.994140625 37.020098201368114,-113.994140625 41.934976500546554)";

function getCoordenates(lineString) {
  // delete "LINESTRING(" at the beginning and ")" at the end, and modify the string ","
  let coords = lineString.slice(11, -1).split(",");

  // Convierte [longitud, latitud]
  let coordList = coords.map((coord) => {
    let parts = coord.trim().split(" ");
    return [parseFloat(parts[1]), parseFloat(parts[0])];
  });

  return coordList;
}

function rotate(origin, point, angle) {
  let radians = (angle * Math.PI) / 180.0;
  let nx =
    Math.cos(radians) * (point[0] - origin[0]) -
    Math.sin(radians) * (point[1] - origin[1]) +
    origin[0];
  let ny =
    Math.sin(radians) * (point[0] - origin[0]) +
    Math.cos(radians) * (point[1] - origin[1]) +
    origin[1];
  return [nx, ny];
}

function rotateLineString(lineString) {
  let lonList = [];
  let latList = [];
  lineString.forEach((coord) => {
    lonList.push(parseFloat(coord[0]));
    latList.push(parseFloat(coord[1]));
  });

  let origin = [
    (Math.max(...lonList) + Math.min(...lonList)) / 2,
    (Math.max(...latList) + Math.min(...latList)) / 2,
  ];
  let rotatedCoords = lineString.map((coord) => rotate(origin, coord, 90));
  return rotatedCoords;
}

function convertLineStringToObj(newCoords) {
  return {
    data: [[newCoords, "Lines"]],
    columns: [{ name: "geom" }, { name: "name" }],
  };
}

let rotatedCoords = rotateLineString(getCoordenates(lineStringExample));
let lineObj = convertLineStringToObj(rotatedCoords);

// function convertCoordsToString(rotatedCoords) {
//   let coordStrings = rotatedCoords
//     .map((coord) => `${coord[1]} ${coord[0]}`)
//     .join(",");
//   let lineString = `LINESTRING(${coordStrings})`;
//   return lineString;
// }

// let lineString = convertCoordsToString(rotatedCoords.rotatedCoords);

// console.log(lineString);
console.log(lineObj);
console.log(rotatedCoords);
console.log("Tipo de dato de lineObj:", typeof lineObj);
