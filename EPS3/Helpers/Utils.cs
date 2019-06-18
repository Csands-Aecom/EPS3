using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Linq.Expressions;
using System.Globalization;

namespace EPS3.Helpers
{
    public static class Utils
    {
        // BuildOrExpression is used for LINQ expressions to select Where <value> is in (<list of values>)
        // Example: List<User> wpUsers = _context.Users.AsNoTracking().Where(Utils.BuildOrExpression<User, int>(u => u.UserID, wpUserIDs.ToArray<int>())).ToList();

        public static Expression<Func<TElement, bool>> BuildOrExpression<TElement, TValue>(
                   Expression<Func<TElement, TValue>> valueSelector,
                   IEnumerable<TValue> values
               )
        {
            if (null == valueSelector)
                throw new ArgumentNullException("valueSelector");


            if (null == values)
                throw new ArgumentNullException("values");


            ParameterExpression p = valueSelector.Parameters.Single();


            if (!values.Any())
                return e => false;


            var equals = values.Select(value =>
                (Expression)Expression.Equal(
                     valueSelector.Body,
                     Expression.Constant(
                         value,
                         typeof(TValue)
                     )
                )
            );

            var body = equals.Aggregate<Expression>(
                     (accumulate, equal) => Expression.Or(accumulate, equal)
             );

            return Expression.Lambda<Func<TElement, bool>>(body, p);
        }
        public static bool IsNullOrEmpty<T>(this IEnumerable<T> enumerable)
        {
            if (enumerable == null)
            {
                return true;
            }
            /* If this is a list, use the Count property for efficiency. 
             * The Count property is O(1) while IEnumerable.Count() is O(N). */
            var collection = enumerable as ICollection<T>;
            if (collection != null)
            {
                return collection.Count < 1;
            }
            return !enumerable.Any();
        }

        public static string FormatCurrency(string amountString)
        {
            amountString = amountString.Replace("$", "").Replace("(", "-").Replace(")", "").Replace(",", "");
            decimal amount = decimal.Parse(amountString);
            return FormatCurrency(amount);
        }
        public static string FormatCurrency(decimal amount)
        {
            CultureInfo culture = CultureInfo.CreateSpecificCulture("en-US");
            culture.NumberFormat.CurrencyNegativePattern = 1;
            String str = String.Format(culture, "{0:C}", amount);
            return str;
        }

    } // end class
} // end namespace
