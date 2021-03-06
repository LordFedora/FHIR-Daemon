using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace FHIR_App
{
    public class Filter
    {

        private List<Condition> Conditions;
        private FilterStates State = FilterStates.DEFAULT;

        public Filter(FilterStates state,Condition condition)
        {
            State = state;
            Conditions = new List<Condition>();
            Conditions.Add(condition);
        }

        public bool CheckConditions(JToken input)
        {
            bool bReturn = true;
            foreach(Condition c in Conditions)
            {
                bReturn = bReturn && c.CheckCondition(input);
            }
            return bReturn;
        }

        public FilterStates getState()
        {
            return State;
        }

    }

    public enum FilterStates
    {
        DEFAULT,
        HIDE,
        SHOW
    }
}
