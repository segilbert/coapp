

namespace CoApp.Toolkit.Extensions
{

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using CoApp.Toolkit.Scripting.Languages.PropertySheet;

    public static class PropertySheetExtensions
    {
        public static Rule Rule(this PropertySheet sheet, string selector)
        {
            return sheet[selector].FirstOrDefault();
        }

        public static RuleProperty Prop(this Rule rule, string prop)
        {
            return (from p in rule.Properties
                    where p.Name == prop
                    select p).FirstOrDefault();
        }

    }
}
