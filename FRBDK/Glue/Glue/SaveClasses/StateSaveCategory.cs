﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.Glue.SaveClasses
{
    public class StateSaveCategory
    {
        public List<StateSave> States = new List<StateSave>();

        public List<string> ExcludedVariables { get; set; } = new List<string>();

        public string Name
        {
            get;
            set;
        }

        public bool SharesVariablesWithOtherCategories
        {
            get;
            set;
        }

        public StateSaveCategory()
        {
            SharesVariablesWithOtherCategories = true;
        }

        public StateSave GetState(string stateName)
        {
            foreach (StateSave stateSave in States)
            {
                if (stateSave.Name == stateName)
                {
                    return stateSave;
                }
            }
            return null;
        }

        public override string ToString()
        {
            return Name + " (State Category)";
        }
    }
}
