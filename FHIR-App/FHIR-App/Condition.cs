using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace FHIR_App
{
    public interface Condition
    {
        bool CheckCondition(JToken input);

    }

    public class PathCondition : Condition
    {
        private Condition SubCondition;
        private String PathStep;

        public PathCondition(String pathStep, Condition subCondition)
        {
            SubCondition = subCondition;
            PathStep = pathStep;
        }

        public bool CheckCondition(JToken input)
        {
            char[] seperators = new char[] { '/' };
            String[] paths = PathStep.Split(seperators,StringSplitOptions.RemoveEmptyEntries);
            dynamic subPath = input;
            foreach(String path in paths) subPath = subPath?[path];
            if (subPath is null) return false;
            return SubCondition.CheckCondition(subPath);
        }
    }

    public class OrCondition : Condition
    {
        private List<Condition> SubConditions;

        public OrCondition(Condition subCondition)
        {
            SubConditions = new List<Condition>();
            SubConditions.Add(subCondition);
        }

        public OrCondition addCondition(Condition subCondition)
        {
            SubConditions.Add(subCondition);
            return this;
        }

        public bool CheckCondition(JToken input)
        {
            bool bReturn = false;
            foreach (Condition c in SubConditions)
            {
                bReturn = bReturn || c.CheckCondition(input);
            }
            return bReturn;
        }
    }

    public class AndCondition : Condition
    {
        private List<Condition> SubConditions;

        public AndCondition(Condition subCondition)
        {
            SubConditions = new List<Condition>();
            SubConditions.Add(subCondition);
        }

        public AndCondition addCondition(Condition subCondition)
        {
            SubConditions.Add(subCondition);
            return this;
        }

        public bool CheckCondition(JToken input)
        {
            bool bReturn = true;
            foreach (Condition c in SubConditions)
            {
                bReturn = bReturn && c.CheckCondition(input);
            }
            return bReturn;
        }
    }

    public class NotCondition : Condition
    {
        private Condition SubCondition;

        public NotCondition(Condition subCondition)
        {
            SubCondition = subCondition;
        }

        public bool CheckCondition(JToken input)
        {
            return !SubCondition.CheckCondition(input);
        }
    }

    public class ValueCondition : Condition
    {
        private String Key;
        private String Value;

        public ValueCondition(String key, String value)
        {
            Key = key;
            Value = value;
        }

        public bool CheckCondition(JToken input)
        {
            dynamic value = input?[Key];
            if (value is null) return false;
            return value.ToString().Equals(Value);
        }
    }

    public class TypeCondition : Condition
    {
        String Type;
        String SubType;

        public TypeCondition(String type, String subType)
        {
            Type = type;
            SubType = subType;
        }

        public TypeCondition(String type)
        {
            Type = type;
            SubType = null;
        }

        public bool CheckCondition(JToken input)
        {
            dynamic resource = input?["resource"];
            if(resource?["type"]?["display"].ToString() != Type)
            {
                return false;
            }
            if(!(SubType is null)){
                Boolean tempb = false;
                foreach(dynamic temp in resource?["subtype"])
                {
                    tempb = tempb || (temp?["display"].ToString() == SubType);
                }
            
                return tempb;
            }
            return true;
        }
    }

    public class ValuesCondition : Condition
    {
        private String Key;
        private String[] Values;

        public ValuesCondition(String key, String[] values)
        {
            Key = key;
            Values = values;
        }

        public bool CheckCondition(JToken input)
        {
            dynamic value = input?[Key];
            if (value is null) return false;
            return Array.IndexOf(Values, value) > -1; //What the honest fuck, how is this the *correct* way to do this
        }
    }

    public class ExistsCondition : Condition
    {
        private String Key;

        public ExistsCondition(String key)
        {
            Key = key;
        }

        public bool CheckCondition(JToken input)
        {
            dynamic value = input?[Key];
            return !(value is null);
        }
    }



}
