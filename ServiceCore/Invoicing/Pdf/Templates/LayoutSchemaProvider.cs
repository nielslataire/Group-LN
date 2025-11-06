using System;
using Newtonsoft.Json.Schema;

namespace ServiceCore.Invoicing.Pdf.Templates
{
    public static class LayoutSchemaProvider
    {
        private static readonly Lazy<JSchema> Schema = new(() =>
            JSchema.Parse("""
            {
              "$schema": "http://json-schema.org/draft-07/schema#",
              "type": "object",
              "properties": {
                "version": { "type": "integer" },
                "page": {
                  "type": "object",
                  "properties": {
                    "margin": { "type": ["number", "integer"] },
                    "backgroundImage": { "type": "string" },
                    "bands": {
                      "type": "object",
                      "properties": {
                        "top":    { "$ref": "#/definitions/band" },
                        "bottom": { "$ref": "#/definitions/band" }
                      },
                      "additionalProperties": false
                    }
                  },
                  "additionalProperties": true
                },
                "theme": {
                  "type": "object",
                  "properties": {
                    "primary":     { "type": "string" },
                    "secondary":   { "type": "string" },
                    "fontFamily":  { "type": "string" },
                    "logoSource":  { "type": "string" }
                  },
                  "additionalProperties": true
                },
                "sections": {
                  "type": "array",
                  "items": {
                    "type": "object",
                    "properties": {
                      "type":    { "type": "string" },
                      "visible": { "type": "boolean" }
                    },
                    "required": ["type"],
                    "additionalProperties": true
                  }
                }
              },
              "required": ["version", "sections"],
              "definitions": {
                "band": {
                  "type": "object",
                  "properties": {
                    "height": { "type": ["number", "integer"] },
                    "color":  { "type": "string" }
                  },
                  "additionalProperties": false
                }
              }
            }
            """));

        public static JSchema GetSchema() => Schema.Value;

        public static string GetSchemaJson() => Schema.Value.ToString();
    }
}
