﻿using System.Collections.Generic;

namespace Roomies.WebAPI.Responses
{
    public class RoommateExpenses
    {
        public IEnumerable<ExpenseResult> Expenses { get; set; }
        public decimal YourTotal { get; set; }
    }
}
