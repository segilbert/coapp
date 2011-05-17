

namespace CoApp.Toolkit.Extensions
{

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using CoApp.Toolkit.Scripting.Languages.PropertySheet;

    public static class PropertySheetExtensions
    {
        public static Rule GetSingleRule(this PropertySheet sheet, string selector)
        {
            return sheet[selector].FirstOrDefault();
        }

        public static RuleProperty GetSingleProperty(this PropertySheet sheet, string selector, string property)
        {
            var rule = sheet.GetSingleRule(selector);
            if (rule == null)
                return null;
            else
                return (from p in rule.Properties
                        where p.Name == property
                        select p).FirstOrDefault();
        }
    }
}
