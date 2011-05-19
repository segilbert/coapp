//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.Scripting.Languages.PropertySheet {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Exceptions;
    using Extensions;

    public static class RulePropertyExtensions {
        public static string Name(this IEnumerable<RuleProperty> properties) {
            return properties.Any() ? properties.First().Name : string.Empty;
        }

        public static bool AsBoolean(this IEnumerable<RuleProperty> properties, bool defaultValue = false) {
            return Boolean.Parse(AsConstrainedString(properties, new[] {Boolean.TrueString, Boolean.FalseString}, defaultValue.ToString()));
        }

        public static int AsInt(this IEnumerable<RuleProperty> properties, int defaultValue = 0) {
            try {
                return Int32.Parse(AsString(properties, defaultValue.ToString()));
            }
            catch {
                throw new EndUserPropertyException(properties.First(), "COAPP 102", "Property {0} must be an integer");
            }
        }

        public static T As<T>(this IEnumerable<RuleProperty> properties, T defaultValue = default(T)) where T : struct, IConvertible {
            if (!typeof (T).IsEnum) {
                throw new ArgumentException("T must be an Enum");
            }

            return (T) Enum.Parse(typeof (T), AsConstrainedString(properties, Enum.GetNames(typeof (T)), defaultValue.ToString()), true);
        }

        public static string AsConstrainedString(this IEnumerable<RuleProperty> properties, IEnumerable<string> possibleValues,
            string defaultValue, bool failOnNonCompoundValues = true) {

            if(!failOnNonCompoundValues) {
                properties = from p in properties where !p.IsCompoundProperty && !p.IsCompoundCollection select p;
            }

            var values = properties.SelectMany(each => each.Values);

            if (values.Any()) {
                if (values.Count() > 1) {
                    throw new EndUserPropertyException(properties.First(), "COAPP 100", "Property {0} must only have one value",
                        properties.Name());
                }

                if (properties.First().IsCompoundProperty) {
                    throw new EndUserPropertyException(properties.First(), "COAPP 103", "Property {0} must not be a compound value",
                        properties.Name());
                }

                var value = values.First();
                if (possibleValues.ContainsIgnoreCase(value)) {
                    return value;
                }

                throw new EndUserPropertyException(properties.First(), "COAPP 101", "Property {0} value must be one of {{{1}{2}}}",
                    properties.Name(), possibleValues.Take(10).Aggregate((result, each) => result + ", " + each),
                    possibleValues.Count() < 10 ? "" : "...");
            }
            return defaultValue;
        }

        public static string AsString(this IEnumerable<RuleProperty> properties, string defaultValue = null) {
            defaultValue = defaultValue ?? string.Empty;

            var values = properties.SelectMany(each => each.Values);

            if (values.Any()) {
                if (values.Count() > 1) {
                    throw new EndUserPropertyException(properties.First(), "COAPP 100", "Property {0} must only have one value",
                        properties.Name());
                }

                if (properties.First().IsCompoundProperty) {
                    throw new EndUserPropertyException(properties.First(), "COAPP 103", "Property {0} must not be a compound value",
                        properties.Name());
                }

                return values.First();
            }
            return defaultValue;
        }

        public static IEnumerable<string> AsStrings(this IEnumerable<RuleProperty> properties) {
            var values = properties.SelectMany(each => each.Values);

            if (values.Any()) {
                if (properties.First().IsCompoundProperty) {
                    throw new EndUserPropertyException(properties.First(), "COAPP 103", "Property {0} must not be a compound value",
                        properties.Name());
                }
            }
            return values;
        }

        public static IDictionary<string, IEnumerable<string>> AsDictionary(this IEnumerable<RuleProperty> properties,
            bool failOnNonCompoundValues = true) {
            var nonCompoundProperties = from p in properties where !p.IsCompoundProperty && !p.IsCompoundCollection select p;


            if (failOnNonCompoundValues && nonCompoundProperties.Any()) {
                throw new EndUserPropertyException(nonCompoundProperties.First(), "COAPP 104", "Property {0} must be a compound value ",
                    nonCompoundProperties.Name());
            }

            var compoundProperties = from p in properties where p.IsCompoundProperty || p.IsCompoundCollection select p;

            return (from prop in compoundProperties select prop.LValue).Distinct().ToDictionary(key => key,
                key => compoundProperties.Where(each => each.LValue == key).SelectMany(each => each.Values));
        }
    }
}