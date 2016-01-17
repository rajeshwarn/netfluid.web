﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="GeometryConverter.cs" company="Joerg Battermann">
//   Copyright © Joerg Battermann 2014
// </copyright>
// <summary>
//   Defines the GeometryConverter type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Netfluid.Geo;
using Netfluid.Json;
using Netfluid.Json.Linq;

namespace Netfluid.Geo.Converters
{
    /// <summary>
    /// Converts <see cref="IGeometryObject"/> types to and from JSON.
    /// </summary>
    internal class GeometryConverter : JsonConverter
    {
        /// <summary>
        ///     Determines whether this instance can convert the specified object type.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <returns>
        ///     <c>true</c> if this instance can convert the specified object type; otherwise, <c>false</c>.
        /// </returns>
        public override bool CanConvert(Type objectType)
        {
            return typeof(IGeometryObject).IsAssignableFrom(objectType);
        }

        /// <summary>
        ///     Reads the JSON representation of the object.
        /// </summary>
        /// <param name="reader">The <see cref="T:Netfluid.Json.JsonReader" /> to read from.</param>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="existingValue">The existing value of object being read.</param>
        /// <param name="serializer">The calling serializer.</param>
        /// <returns>
        ///     The object value.
        /// </returns>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            switch (reader.TokenType)
            {
                case JsonToken.Null:
                    return null;
                case JsonToken.StartObject:
                    var value = JObject.Load(reader);
                    return ReadGeoJson(value);
                case JsonToken.StartArray:
                    var values = JArray.Load(reader);
                    var geometries = new List<IGeometryObject>(values.Count);
                    geometries.AddRange(values.Cast<JObject>().Select(ReadGeoJson));
                    return geometries;
            }

            throw new JsonReaderException("expected null, object or array token but received " + reader.TokenType);
        }

        /// <summary>
        /// Writes the JSON representation of the object.
        /// </summary>
        /// <param name="writer">The <see cref="T:Netfluid.Json.JsonWriter" /> to write to.</param>
        /// <param name="value">The value.</param>
        /// <param name="serializer">The calling serializer.</param>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value);
        }

        /// <summary>
        /// Reads the geo json.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        /// <exception cref="Netfluid.Json.JsonReaderException">
        /// json must contain a "type" property
        /// or
        /// type must be a valid geojson geometry object type
        /// </exception>
        /// <exception cref="System.NotSupportedException">
        /// Feature and FeatureCollection types are Feature objects and not Geometry objects
        /// </exception>
        private static IGeometryObject ReadGeoJson(JObject value)
        {
            JToken token;

            if (!value.TryGetValue("type", StringComparison.OrdinalIgnoreCase, out token))
            {
                throw new JsonReaderException("json must contain a \"type\" property");
            }

            GeoJSONObjectType geoJsonType;

            if (!Enum.TryParse(token.Value<string>(), true, out geoJsonType))
            {
                throw new JsonReaderException("type must be a valid geojson geometry object type");
            }

            switch (geoJsonType)
            {
                case GeoJSONObjectType.Point:
                    return value.ToObject<Point>();
                case GeoJSONObjectType.MultiPoint:
                    return value.ToObject<MultiPoint>();
                case GeoJSONObjectType.LineString:
                    return value.ToObject<LineString>();
                case GeoJSONObjectType.MultiLineString:
                    return value.ToObject<MultiLineString>();
                case GeoJSONObjectType.Polygon:
                    return value.ToObject<Polygon>();
                case GeoJSONObjectType.MultiPolygon:
                    return value.ToObject<MultiPolygon>();
                case GeoJSONObjectType.GeometryCollection:
                    return value.ToObject<GeometryCollection>();
                case GeoJSONObjectType.Feature:
                case GeoJSONObjectType.FeatureCollection:
                default:
                    throw new NotSupportedException("Feature and FeatureCollection types are Feature objects and not Geometry objects");
            }
        }
    }
}