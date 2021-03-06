﻿#region

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq.Expressions;
using SubSonic.Extensions;
using SubSonic.Linq.Structure;

#endregion

namespace SubSonic.DataProviders.Oracle
{
	/// <summary>
	/// Formats a query expression into Oracle language syntax
	/// </summary>
	public class OracleFormatter : TSqlFormatter
	{

		protected override Expression VisitNamedValue(NamedValueExpression value)
		{
			sb.Append(":" + value.Name);
			return value;
		}

		protected override string GetPrepositionUsedBeforeNamingTablesAndColumns {
			get {
				return "";
			}
		}

		protected override Expression VisitMemberAccess(MemberExpression m) {
			if (m.Member.DeclaringType == typeof(string)) {
				switch (m.Member.Name) {
					case "Length":
						sb.Append("LENGTH(");
						this.Visit(m.Expression);
						sb.Append(")");
						return m;
				}
			} else if (m.Member.DeclaringType == typeof(DateTime) || m.Member.DeclaringType == typeof(DateTimeOffset)) {
				switch (m.Member.Name) {
					case "Day":
						sb.Append("TO_CHAR(");
						this.Visit(m.Expression);
						sb.Append(", 'DD')");
						return m;
					case "Month":
						sb.Append("TO_CHAR(");
						this.Visit(m.Expression);
						sb.Append(", 'MM')");
						return m;
					case "Year":
						sb.Append("TO_CHAR(");
						this.Visit(m.Expression);
						sb.Append(", 'YYYY')");
						return m;
					case "Hour":
						sb.Append("TO_CHAR( ");
						this.Visit(m.Expression);
						sb.Append(", 'HH24')");
						return m;
					case "Minute":
						sb.Append("TO_CHAR( ");
						this.Visit(m.Expression);
						sb.Append(", 'MI')");
						return m;
					case "Second":
						sb.Append("TO_CHAR( ");
						this.Visit(m.Expression);
						sb.Append(", 'SS')");
						return m;
					case "Millisecond":
						sb.Append("TO_CHAR( ");
						this.Visit(m.Expression);
						sb.Append(", 'FF')");
						return m;
					case "DayOfWeek":
						sb.Append("(TO_CHAR(");
						this.Visit(m.Expression);
						sb.Append(", 'D') - 1)");
						return m;
					case "DayOfYear":
						sb.Append("(TO_CHAR( ");
						this.Visit(m.Expression);
						sb.Append(", 'DDD') - 1)");
						return m;
				}
			}
			throw new NotSupportedException(string.Format("The member '{0}' is not supported", m.Member.Name));
		}

		protected override Expression VisitMethodCall(MethodCallExpression m) {
			if (m.Method.DeclaringType == typeof(string)) {
				switch (m.Method.Name) {
					case "StartsWith":
						sb.Append("(");
						this.Visit(m.Object);
						sb.Append(" LIKE CONCAT(");
						this.Visit(m.Arguments[0]);
						sb.Append(",'%'))");
						return m;
					case "EndsWith":
						sb.Append("(");
						this.Visit(m.Object);
						sb.Append(" LIKE CONCAT('%',");
						this.Visit(m.Arguments[0]);
						sb.Append("))");
						return m;
					case "Contains":
						sb.Append("(");
						Visit(m.Object);
						sb.Append(" LIKE '%' || ");
						Visit(m.Arguments[0]);
						sb.Append(" || '%')");
						return m;
					case "Concat":
						IList<Expression> args = m.Arguments;
						if (args.Count == 1 && args[0].NodeType == ExpressionType.NewArrayInit) {
							args = ((NewArrayExpression)args[0]).Expressions;
						}
						for (int i = 0, n = args.Count; i < n; i++) {
							if (i > 0) sb.Append(" + ");
							this.Visit(args[i]);
						}
						return m;
					case "IsNullOrEmpty":
						sb.Append("(");
						this.Visit(m.Arguments[0]);
						sb.Append(" IS NULL OR ");
						this.Visit(m.Arguments[0]);
						sb.Append(" = '')");
						return m;
					case "ToUpper":
						sb.Append("UPPER(");
						this.Visit(m.Object);
						sb.Append(")");
						return m;
					case "ToLower":
						sb.Append("LOWER(");
						this.Visit(m.Object);
						sb.Append(")");
						return m;
					case "Replace":
						sb.Append("REPLACE(");
						this.Visit(m.Object);
						sb.Append(", ");
						this.Visit(m.Arguments[0]);
						sb.Append(", ");
						this.Visit(m.Arguments[1]);
						sb.Append(")");
						return m;
					case "Substring":
						sb.Append("SUBSTRING(");
						this.Visit(m.Object);
						sb.Append(", ");
						this.Visit(m.Arguments[0]);
						sb.Append(" + 1, ");
						if (m.Arguments.Count == 2) {
							this.Visit(m.Arguments[1]);
						} else {
							sb.Append("8000");
						}
						sb.Append(")");
						return m;
					case "Remove":
						sb.Append("STUFF(");
						this.Visit(m.Object);
						sb.Append(", ");
						this.Visit(m.Arguments[0]);
						sb.Append(" + 1, ");
						if (m.Arguments.Count == 2) {
							this.Visit(m.Arguments[1]);
						} else {
							sb.Append("8000");
						}
						sb.Append(", '')");
						return m;
					case "IndexOf":
						sb.Append("(LOCATE(");
						this.Visit(m.Arguments[0]);
						sb.Append(", ");
						this.Visit(m.Object);
						if (m.Arguments.Count == 2 && m.Arguments[1].Type == typeof(int)) {
							sb.Append(", ");
							this.Visit(m.Arguments[1]);
						}
						sb.Append(") - 1)");
						return m;
					case "Trim":
						sb.Append("RTRIM(LTRIM(");
						this.Visit(m.Object);
						sb.Append("))");
						return m;
				}

			}
            else if (m.Method.DeclaringType == typeof(SubSonic.Extensions.Strings))
            {
                switch (m.Method.Name)
                {
                    case "Wildcard":
                        sb.Append("(");
                        this.Visit(m.Arguments[0]);
                        sb.Append(" LIKE ");
                        this.Visit(m.Arguments[1]);
                        sb.Append(" )");
                        return m;
                }
            }
            else if (m.Method.DeclaringType == typeof(DateTime)) {
				switch (m.Method.Name) {
					case "op_Subtract":
						if (m.Arguments[1].Type == typeof(DateTime)) {
							sb.Append("DATEDIFF(");
							this.Visit(m.Arguments[0]);
							sb.Append(", ");
							this.Visit(m.Arguments[1]);
							sb.Append(")");
							return m;
						}
						break;
				}
			} else if (m.Method.DeclaringType == typeof(Decimal)) {
				switch (m.Method.Name) {
					case "Add":
					case "Subtract":
					case "Multiply":
					case "Divide":
					case "Remainder":
						sb.Append("(");
						this.VisitValue(m.Arguments[0]);
						sb.Append(" ");
						sb.Append(GetOperator(m.Method.Name));
						sb.Append(" ");
						this.VisitValue(m.Arguments[1]);
						sb.Append(")");
						return m;
					case "Negate":
						sb.Append("-");
						this.Visit(m.Arguments[0]);
						sb.Append("");
						return m;
					case "Ceiling":
					case "Floor":
						sb.Append(m.Method.Name.ToUpper());
						sb.Append("(");
						this.Visit(m.Arguments[0]);
						sb.Append(")");
						return m;
					case "Round":
						//if (m.Arguments.Count == 1) {
							sb.Append("ROUND(");
							this.Visit(m.Arguments[0]);
							sb.Append(", 0)");
							return m;
						//} else if (m.Arguments.Count == 2 && m.Arguments[1].Type == typeof(int)) {
							//sb.Append("ROUND(");
							//this.Visit(m.Arguments[0]);
							//sb.Append(", ");
							//this.Visit(m.Arguments[1]);
							//sb.Append(")");
							//return m;
						//}
					case "Truncate":
						sb.Append("ROUND(");
						this.Visit(m.Arguments[0]);
						sb.Append(", 0)");
						return m;
				}
			} else if (m.Method.DeclaringType == typeof(Math)) {
				switch (m.Method.Name) {
					case "Abs":
					case "Acos":
					case "Asin":
					case "Atan":
					case "Cos":
					case "Exp":
					case "Log10":
					case "Sin":
					case "Tan":
					case "Sqrt":
					case "Sign":
					case "Ceiling":
					case "Floor":
						sb.Append(m.Method.Name.ToUpper());
						sb.Append("(");
						this.Visit(m.Arguments[0]);
						sb.Append(")");
						return m;
					case "Atan2":
						sb.Append("ATAN2(");
						this.Visit(m.Arguments[0]);
						sb.Append(", ");
						this.Visit(m.Arguments[1]);
						sb.Append(")");
						return m;
					case "Log":
						if (m.Arguments.Count == 1) {
							goto case "Log10";
						}
						break;
					case "Pow":
						sb.Append("POWER(");
						this.Visit(m.Arguments[0]);
						sb.Append(", ");
						this.Visit(m.Arguments[1]);
						sb.Append(")");
						return m;
					case "Round":
						//if (m.Arguments.Count == 1) {
							sb.Append("ROUND(");
							this.Visit(m.Arguments[0]);
							sb.Append(", 0)");
							return m;
						//} else if (m.Arguments.Count == 2 && m.Arguments[1].Type == typeof(int)) {
							//sb.Append("ROUND(");
							//this.Visit(m.Arguments[0]);
							//sb.Append(", ");
							//this.Visit(m.Arguments[1]);
							//sb.Append(")");
							//return m;
						//}
						//break;
					case "Truncate":
						sb.Append("ROUND(");
						this.Visit(m.Arguments[0]);
						sb.Append(", 0)");
						return m;
				}
			}
			if (m.Method.Name == "ToString") {
				if (m.Object.Type == typeof(string)) {
					this.Visit(m.Object);  // no op
				} else {
					sb.Append("CONVERT(VARCHAR(MAX), ");
					this.Visit(m.Object);
					sb.Append(")");
				}
				return m;
			} 
			if (m.Method.Name == "Equals") {
				if (m.Method.IsStatic && m.Method.DeclaringType == typeof(object)) {
					sb.Append("(");
					this.Visit(m.Arguments[0]);
					sb.Append(" = ");
					this.Visit(m.Arguments[1]);
					sb.Append(")");
					return m;
				} 
				if (!m.Method.IsStatic && m.Arguments.Count == 1 && m.Arguments[0].Type == m.Object.Type) {
					sb.Append("(");
					this.Visit(m.Object);
					sb.Append(" = ");
					this.Visit(m.Arguments[0]);
					sb.Append(")");
					return m;
				}
			}
			throw new NotSupportedException(string.Format("The method '{0}' is not supported", m.Method.Name));
		}

		protected override Expression VisitSource(Expression source)
		{
			
			bool saveIsNested = this.isNested;
			this.isNested = true;
			switch ((DbExpressionType)source.NodeType)
			{
				case DbExpressionType.Table:
					TableExpression table = (TableExpression)source;
					sb.Append(table.Name.Replace("[", "").Replace("]", ""));
					sb.Append(" ");
					sb.Append(GetAliasName(table.Alias));
					break;
				case DbExpressionType.Select:
					SelectExpression select = (SelectExpression)source;
					sb.Append("(");
					this.AppendNewLine(Indentation.Inner);
					this.Visit(select);
					this.AppendNewLine(Indentation.Same);
					sb.Append(")");
					sb.Append(" ");
					sb.Append(GetAliasName(select.Alias));
					this.Indent(Indentation.Outer);
					break;
				case DbExpressionType.Join:
					this.VisitJoin((JoinExpression)source);
					break;
				default:
					throw new InvalidOperationException("Select source is not valid type");
			}
			this.isNested = saveIsNested;
			return source;
		
		}

		
		protected override Expression VisitSelect(SelectExpression select)
		{
			sb.Append("SELECT ");
			if (select.IsDistinct)
			{
				sb.Append("DISTINCT ");
			}

			if (select.Columns.Count > 0)
			{
				for (int i = 0, n = select.Columns.Count; i < n; i++)
				{
					ColumnDeclaration column = select.Columns[i];
					if (i > 0)
					{
						sb.Append(", ");
					}
					ColumnExpression c = VisitValue(column.Expression) as ColumnExpression;
					if (!string.IsNullOrEmpty(column.Name) && (c == null || c.Name != column.Name))
					{
						sb.Append(" ");
						sb.Append(column.Name);
					}
				}
			}
			else
			{
				sb.Append("NULL ");
				if (isNested)
				{
					sb.Append(" tmp ");
				}
			}
            if (select.From != null)
            {
                AppendNewLine(Indentation.Same);
                sb.Append("FROM ");
                VisitSource(select.From);
            }
            else
            {
                AppendNewLine(Indentation.Same);
                sb.Append("FROM DUAL");
            }
			if (select.Where != null)
			{
				AppendNewLine(Indentation.Same);
				sb.Append("WHERE ");
				VisitPredicate(select.Where);
			}
			if (select.GroupBy != null && select.GroupBy.Count > 0)
			{
				AppendNewLine(Indentation.Same);
				sb.Append("GROUP BY ");
				for (int i = 0, n = select.GroupBy.Count; i < n; i++)
				{
					if (i > 0)
					{
						sb.Append(", ");
					}
					VisitValue(select.GroupBy[i]);
				}
			}
			if (select.OrderBy != null && select.OrderBy.Count > 0)
			{
				AppendNewLine(Indentation.Same);
				sb.Append("ORDER BY ");
				for (int i = 0, n = select.OrderBy.Count; i < n; i++)
				{
					OrderExpression exp = select.OrderBy[i];
					if (i > 0)
					{
						sb.Append(", ");
					}
					VisitValue(exp.Expression);
					if (exp.OrderType != OrderType.Ascending)
					{
						sb.Append(" DESC");
					}
				}
			}

			int skip = select.Skip == null ? 0 : (int) select.Skip.GetConstantValue();
			int take = select.Take == null ? 0 : (int) select.Take.GetConstantValue();

			if (take > 0)
			{
				AppendNewLine(Indentation.Same);
				sb.Insert(0, "select * from  ( select t1.*, ROWNUM rn from (");
				sb.Append(") t1 ) where" );
				sb.AppendFormat(" rn >= {0} AND rn <= {1}", skip, take+skip);
			}
			return select;
		}

		protected override Expression VisitColumn(ColumnExpression column)
		{
			if (column.Alias != null)
			{
				sb.AppendFormat("{0}", GetAliasName(column.Alias));
				sb.Append("."); //MAA,20091127:  remove quotes in column names
				//sb.Append(".\"");
			}
			sb.AppendFormat("{0}", column.Name);
			//sb.Append("\"");//MAA,20091127:  remove quotes in column names
			return column;
		}

		public static string FormatExpression(Expression expression)
		{
			OracleFormatter formatter = new OracleFormatter();
			formatter.Visit(expression);
			return formatter.sb.ToString();
		}

        protected override Expression VisitValue(Expression expr)
        {
            if (IsPredicate(expr))
            {
                sb.Append("(CASE WHEN (");
                this.Visit(expr);
                sb.Append(") THEN 1 ELSE 0 END)");
            }
            else
            {
                Visit(expr);
            }
            return expr;
        }

        protected override void WriteValue(object value)
        {
            if (Type.GetTypeCode(value.GetType()) == TypeCode.DateTime)
            {
                sb.Append("to_date('");
                sb.Append(((DateTime) value).ToString("yyyy-MM-ddTHH:mm:ss"));
                sb.Append("','YYYY-MM-DD\"T\"HH24:MI:SS')");
                return;
            }
            base.WriteValue(value);
        }

	    protected override Expression VisitBinary(BinaryExpression b)
        {
            if (b.NodeType == ExpressionType.Equal
                || b.NodeType == ExpressionType.NotEqual)
                return ExploreSpecializedEqualityExpression(b);
            return base.VisitBinary(b);
        }

	    private Expression ExploreSpecializedEqualityExpression(BinaryExpression binaryExpression)
	    {
	        var left = binaryExpression.Left;
	        var right = binaryExpression.Right;

	        if (left.NodeType == (ExpressionType) DbExpressionType.Column)
	        {
	            switch (right.NodeType)
	            {
	                case ExpressionType.Constant:
	                    {
	                        var constantExpresison = (ConstantExpression) right;
	                        if (constantExpresison.Value != null
	                            && constantExpresison.Type == typeof (Guid))
	                        {
	                            var columnExpression = (ColumnExpression) left;
	                            if (columnExpression.Column != null)
	                            {
	                                sb.Append("( LOWER(");
	                                Visit(left);
	                                sb.Append(") ");
	                                sb.Append(GetOperator(binaryExpression));
	                                sb.Append(" LOWER('");
	                                sb.Append(OracleSchema.ConvertGuid((Guid) constantExpresison.Value,
	                                                                   columnExpression.Column.DataType,
	                                                                   columnExpression.Column.MaxLength));
	                                sb.Append("') ");
	                                sb.Append(" )");
	                                return binaryExpression;
	                            }
	                        }
	                    }
	                    break;
	                case (ExpressionType) DbExpressionType.NamedValue:
	                    var namedValueExpression = (NamedValueExpression) right;
                        if (namedValueExpression.Type == typeof (Guid))
                        {
                            var columnExpression = (ColumnExpression) left;
                            if (columnExpression.Column != null)
                            {
                                namedValueExpression.ParameterBindingAction =
                                    (constantExpression) => Expression.Call(typeof (OracleSchema),
                                                               "ConvertGuid",
                                                               null,
                                                               new Expression[]
                                                                   {
                                                                       Expression.Constant((Guid)constantExpression.Value),
                                                                       Expression.Constant(
                                                                           columnExpression.Column.DataType),
                                                                       Expression.Constant(
                                                                           columnExpression.Column.MaxLength)
                                                                   });

                                sb.Append("( LOWER(");
                                Visit(left);
                                sb.Append(") ");
                                sb.Append(GetOperator(binaryExpression));
                                sb.Append(" LOWER(");
                                Visit(namedValueExpression);
                                sb.Append(") ");
                                sb.Append(" )");
                                return binaryExpression;
                            }
                        }
	                    break;
	            }
	        }

	        return base.VisitBinary(binaryExpression);
	    }
	}
}