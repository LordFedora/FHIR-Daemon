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
            dynamic subPath = input?[PathStep];
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




}
