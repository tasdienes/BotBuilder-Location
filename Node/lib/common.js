"use strict";
var botbuilder_1 = require('botbuilder');
var place_1 = require('./place');
function createBaseDialog(options) {
    return new botbuilder_1.IntentDialog(options)
        .matches(/help/i, function (session) {
        session.send("help message").sendBatch();
    })
        .matches(/^reset$/i, function (session) {
        session.endDialogWithResult({ response: { reset: true } });
        return;
    });
}
exports.createBaseDialog = createBaseDialog;
function processLocation(location, includeStreetAddress) {
    var place = new place_1.Place();
    place.type = location.entityType;
    place.name = location.name;
    if (location.address) {
        place.formattedAddress = location.address.formattedAddress;
        place.country = location.address.countryRegion;
        place.locality = location.address.locality;
        place.postalCode = location.address.postalCode;
        place.region = location.address.adminDistrict;
        if (includeStreetAddress) {
            place.streetAddress = location.address.addressLine;
        }
    }
    if (location.point && location.point.coordinates && location.point.coordinates.length == 2) {
        place.geo = new place_1.Geo();
        place.geo.latitude = location.point.coordinates[0];
        place.geo.longitude = location.point.coordinates[1];
    }
    return place;
}
exports.processLocation = processLocation;
function buildPlaceFromGeo(latitude, longitude) {
    var place = new place_1.Place();
    place.geo = new place_1.Geo();
    place.geo.latitude = latitude;
    place.geo.longitude = longitude;
    return place;
}
exports.buildPlaceFromGeo = buildPlaceFromGeo;