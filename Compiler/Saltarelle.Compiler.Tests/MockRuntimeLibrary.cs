using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Saltarelle.Compiler.JSModel.Expressions;
using Saltarelle.Compiler.Roslyn;

namespace Saltarelle.Compiler.Tests {
	public class MockRuntimeLibrary : IRuntimeLibrary {
		private enum TypeContext {
			GenericArgument,
			TypeOf,
			CastTarget,
			GetDefaultValue,
			UseStaticMember,
			BindBaseCall,
		}

		private string GetTypeContextShortName(TypeContext c) {
			switch (c) {
				case TypeContext.GenericArgument: return "ga";
				case TypeContext.TypeOf:          return "to";
				case TypeContext.UseStaticMember: return "sm";
				case TypeContext.CastTarget:      return "ct";
				case TypeContext.GetDefaultValue: return "def";
				case TypeContext.BindBaseCall:    return "bind";
				default: throw new ArgumentException("c");
			}
		}

		public MockRuntimeLibrary() {
			GetTypeOf                                       = (t, c)             => GetScriptType(t, TypeContext.TypeOf, c.ResolveTypeParameter);
			InstantiateType                                 = (t, c)             => GetScriptType(t, TypeContext.UseStaticMember, c.ResolveTypeParameter);
			InstantiateTypeForUseAsTypeArgumentInInlineCode = (t, c)             => GetScriptType(t, TypeContext.GenericArgument, c.ResolveTypeParameter);
			TypeIs                                          = (e, s, t, c)       => JsExpression.Invoke(JsExpression.Identifier("$TypeIs"), e, GetScriptType(t, TypeContext.CastTarget, c.ResolveTypeParameter));
			TryDowncast                                     = (e, s, d, c)       => JsExpression.Invoke(JsExpression.Identifier("$TryCast"), e, GetScriptType(d, TypeContext.CastTarget, c.ResolveTypeParameter));
			Downcast                                        = (e, s, d, c)       => JsExpression.Invoke(JsExpression.Identifier("$Cast"), e, GetScriptType(d, TypeContext.CastTarget, c.ResolveTypeParameter));
			Upcast                                          = (e, s, d, c)       => JsExpression.Invoke(JsExpression.Identifier("$Upcast"), e, GetScriptType(d, TypeContext.CastTarget, c.ResolveTypeParameter));
			ReferenceEquals                                 = (a, b, c)          => JsExpression.Invoke(JsExpression.Identifier("$ReferenceEquals"), a, b);
			ReferenceNotEquals                              = (a, b, c)          => JsExpression.Invoke(JsExpression.Identifier("$ReferenceNotEquals"), a, b);
			InstantiateGenericMethod                        = (m, a, c)          => JsExpression.Invoke(JsExpression.Identifier("$InstantiateGenericMethod"), new[] { m }.Concat(a.Select(x => GetScriptType(x, TypeContext.GenericArgument, c.ResolveTypeParameter))));
			MakeException                                   = (e, c)             => JsExpression.Invoke(JsExpression.Identifier("$MakeException"), e);
			IntegerDivision                                 = (n, d, c)          => JsExpression.Invoke(JsExpression.Identifier("$IntDiv"), n, d);
			NarrowingNumericConversion                      = (e, s, t, ch, c)   => JsExpression.Invoke(JsExpression.Identifier(ch ? "$NarrowChecked" : "$Narrow"), e, GetScriptType(t, TypeContext.CastTarget, c.ResolveTypeParameter));
			EnumerationConversion                           = (e, s, t, ch, c)   => JsExpression.Invoke(JsExpression.Identifier(ch ? "$EnumConvertChecked" : "$EnumConvert"), e, GetScriptType(t, TypeContext.CastTarget, c.ResolveTypeParameter));
			Coalesce                                        = (a, b, c)          => JsExpression.Invoke(JsExpression.Identifier("$Coalesce"), a, b);
			Lift                                            = (e, c)             => JsExpression.Invoke(JsExpression.Identifier("$Lift"), e);
			FromNullable                                    = (e, c)             => JsExpression.Invoke(JsExpression.Identifier("$FromNullable"), e);
			LiftedBooleanAnd                                = (a, b, c)          => JsExpression.Invoke(JsExpression.Identifier("$LiftedBooleanAnd"), a, b);
			LiftedBooleanOr                                 = (a, b, c)          => JsExpression.Invoke(JsExpression.Identifier("$LiftedBooleanOr"), a, b);
			Bind                                            = (f, t, c)          => JsExpression.Invoke(JsExpression.Identifier("$Bind"), f, t);
			BindFirstParameterToThis                        = (f, c)             => JsExpression.Invoke(JsExpression.Identifier("$BindFirstParameterToThis"), f);
			Default                                         = (t, c)             => t.TypeKind == TypeKind.Dynamic ? (JsExpression)JsExpression.Identifier("$DefaultDynamic") : JsExpression.Invoke(JsExpression.Identifier("$Default"), GetScriptType(t, TypeContext.GetDefaultValue, c.ResolveTypeParameter));
			CreateArray                                     = (t, dim, c)        => JsExpression.Invoke(JsExpression.Identifier("$CreateArray"), new[] { GetScriptType(t, TypeContext.GetDefaultValue, c.ResolveTypeParameter) }.Concat(dim));
			CloneDelegate                                   = (e, s, t, c)       => JsExpression.Invoke(JsExpression.Identifier("$CloneDelegate"), e);
			CallBase                                        = (m, a, c)          => JsExpression.Invoke(JsExpression.Identifier("$CallBase"), new[] { GetScriptType(m.ContainingType, TypeContext.BindBaseCall, c.ResolveTypeParameter), JsExpression.String("$" + m.Name), JsExpression.ArrayLiteral(m.TypeArguments.Select(x => GetScriptType(x, TypeContext.GenericArgument, c.ResolveTypeParameter))), JsExpression.ArrayLiteral(a) });
			GetBasePropertyValue                            = (p, t, c)          => JsExpression.Invoke(JsExpression.Identifier("$GetBaseProperty"), t, JsExpression.String(p.Name));
			SetBasePropertyValue                            = (p, t, v, c)       => JsExpression.Invoke(JsExpression.Identifier("$SetBaseProperty"), t, JsExpression.String(p.Name), v);
			BindBaseCall                                    = (m, a, c)          => JsExpression.Invoke(JsExpression.Identifier("$BindBaseCall"), new[] { GetScriptType(m.ContainingType, TypeContext.BindBaseCall, c.ResolveTypeParameter), JsExpression.String("$" + m.Name), JsExpression.ArrayLiteral(m.TypeArguments.Select(x => GetScriptType(x, TypeContext.GenericArgument, c.ResolveTypeParameter))), a });
			MakeEnumerator                                  = (yt, mn, gc, d, c) => JsExpression.Invoke(JsExpression.Identifier("$MakeEnumerator"), new[] { GetScriptType(yt, TypeContext.GenericArgument, c.ResolveTypeParameter), mn, gc, d ?? JsExpression.Null });
			MakeEnumerable                                  = (yt, ge, c)        => JsExpression.Invoke(JsExpression.Identifier("$MakeEnumerable"), new[] { GetScriptType(yt, TypeContext.GenericArgument, c.ResolveTypeParameter), ge });
			GetMultiDimensionalArrayValue                   = (a, i, c)          => JsExpression.Invoke(JsExpression.Identifier("$MultidimArrayGet"), new[] { a }.Concat(i));
			SetMultiDimensionalArrayValue                   = (a, i, v, c)       => JsExpression.Invoke(JsExpression.Identifier("$MultidimArraySet"), new[] { a }.Concat(i).Concat(new[] { v }));
			CreateTaskCompletionSource                      = (t, c)             => JsExpression.Invoke(JsExpression.Identifier("$CreateTaskCompletionSource"), t != null ? GetScriptType(t, TypeContext.GenericArgument, c.ResolveTypeParameter) : JsExpression.String("non-generic"));
			SetAsyncResult                                  = (t, v, c)          => JsExpression.Invoke(JsExpression.Identifier("$SetAsyncResult"), t, v ?? JsExpression.String("<<null>>"));
			SetAsyncException                               = (t, e, c)          => JsExpression.Invoke(JsExpression.Identifier("$SetAsyncException"), t, e);
			GetTaskFromTaskCompletionSource                 = (t, c)             => JsExpression.Invoke(JsExpression.Identifier("$GetTask"), t);
			ApplyConstructor                                = (c, a, x)          => JsExpression.Invoke(JsExpression.Identifier("$ApplyConstructor"), c, a);
			ShallowCopy                                     = (s, t, c)          => JsExpression.Invoke(JsExpression.Identifier("$ShallowCopy"), s, t);
			GetMember                                       = (m, c)             => JsExpression.Invoke(JsExpression.Identifier("$GetMember"), m is IMethodSymbol && ((IMethodSymbol)m).TypeArguments.Length > 0 ? new[] { GetScriptType(m.ContainingType, TypeContext.TypeOf, c.ResolveTypeParameter), JsExpression.String(m.MetadataName), JsExpression.ArrayLiteral(((IMethodSymbol)m).TypeArguments.Select(ta => GetScriptType(ta, TypeContext.GenericArgument, c.ResolveTypeParameter))) } : new[] { GetScriptType(m.ContainingType, TypeContext.TypeOf, c.ResolveTypeParameter), JsExpression.String(m.MetadataName) });
			GetAnonymousTypeInfo                            = (t, c)             => JsExpression.Invoke(JsExpression.Identifier("$GetAnonymousTypeInfo"), t.GetProperties().SelectMany(p => new[] { InstantiateType(p.Type, c), JsExpression.String(p.Name) }));
			GetTransparentTypeInfo                          = (m, c)             => JsExpression.Invoke(JsExpression.Identifier("$GetTransparentType"), m.SelectMany(x => new[] { x.Item1, JsExpression.String(x.Item2) }));
			GetExpressionForLocal                           = (n, a, t, c)       => JsExpression.Invoke(JsExpression.Identifier("$Local"), JsExpression.String(n), GetScriptType(t, TypeContext.TypeOf, c.ResolveTypeParameter), a);
			CloneValueType                                  = (v, t, c)          => JsExpression.Invoke(JsExpression.Identifier("$Clone"), v, GetScriptType(t, TypeContext.TypeOf, c.ResolveTypeParameter));
			InitializeField                                 = (t, n, m, v, c)    => JsExpression.Invoke(JsExpression.Identifier("$Init"), t, JsExpression.String(n), v);
		}

		public Func<ITypeSymbol, IRuntimeContext, JsExpression> GetTypeOf { get; set; }
		public Func<ITypeSymbol, IRuntimeContext, JsExpression> InstantiateType { get; set; }
		public Func<ITypeSymbol, IRuntimeContext, JsExpression> InstantiateTypeForUseAsTypeArgumentInInlineCode { get; set; }
		public Func<JsExpression, ITypeSymbol, ITypeSymbol, IRuntimeContext, JsExpression> TypeIs { get; set; }
		public Func<JsExpression, ITypeSymbol, ITypeSymbol, IRuntimeContext, JsExpression> TryDowncast { get; set; }
		public Func<JsExpression, ITypeSymbol, ITypeSymbol, IRuntimeContext, JsExpression> Downcast { get; set; }
		public Func<JsExpression, ITypeSymbol, ITypeSymbol, IRuntimeContext, JsExpression> Upcast { get; set; }
		public Func<JsExpression, IEnumerable<ITypeSymbol>, IRuntimeContext, JsExpression> InstantiateGenericMethod { get; set; }
		new public Func<JsExpression, JsExpression, IRuntimeContext, JsExpression> ReferenceEquals { get; set; }
		public Func<JsExpression, JsExpression, IRuntimeContext, JsExpression> ReferenceNotEquals { get; set; }
		public Func<JsExpression, IRuntimeContext, JsExpression> MakeException { get; set; }
		public Func<JsExpression, JsExpression, IRuntimeContext, JsExpression> IntegerDivision { get; set; }
		public Func<JsExpression, ITypeSymbol, ITypeSymbol, bool, IRuntimeContext, JsExpression> NarrowingNumericConversion { get; set; }
		public Func<JsExpression, ITypeSymbol, ITypeSymbol, bool, IRuntimeContext, JsExpression> EnumerationConversion { get; set; }
		public Func<JsExpression, JsExpression, IRuntimeContext, JsExpression> Coalesce { get; set; }
		public Func<JsExpression, IRuntimeContext, JsExpression> Lift { get; set; }
		public Func<JsExpression, IRuntimeContext, JsExpression> FromNullable { get; set; }
		public Func<JsExpression, JsExpression, IRuntimeContext, JsExpression> LiftedBooleanAnd { get; set; }
		public Func<JsExpression, JsExpression, IRuntimeContext, JsExpression> LiftedBooleanOr { get; set; }
		public Func<JsExpression, JsExpression, IRuntimeContext, JsExpression> Bind { get; set; }
		public Func<JsExpression, IRuntimeContext, JsExpression> BindFirstParameterToThis { get; set; }
		public Func<ITypeSymbol, IRuntimeContext, JsExpression> Default { get; set; }
		public Func<ITypeSymbol, IEnumerable<JsExpression>, IRuntimeContext, JsExpression> CreateArray { get; set; }
		public Func<JsExpression, ITypeSymbol, ITypeSymbol, IRuntimeContext, JsExpression> CloneDelegate { get; set; }
		public Func<IMethodSymbol, IEnumerable<JsExpression>, IRuntimeContext, JsExpression> CallBase { get; set; }
		public Func<IPropertySymbol, JsExpression, IRuntimeContext, JsExpression> GetBasePropertyValue { get; set; }
		public Func<IPropertySymbol, JsExpression, JsExpression, IRuntimeContext, JsExpression> SetBasePropertyValue { get; set; }
		public Func<IMethodSymbol, JsExpression, IRuntimeContext, JsExpression> BindBaseCall { get; set; }
		public Func<ITypeSymbol, JsExpression, JsExpression, JsExpression, IRuntimeContext, JsExpression> MakeEnumerator { get; set; }
		public Func<ITypeSymbol, JsExpression, IRuntimeContext, JsExpression> MakeEnumerable { get; set; }
		public Func<JsExpression, IEnumerable<JsExpression>, IRuntimeContext, JsExpression> GetMultiDimensionalArrayValue { get; set; }
		public Func<JsExpression, IEnumerable<JsExpression>, JsExpression, IRuntimeContext, JsExpression> SetMultiDimensionalArrayValue { get; set; }
		public Func<ITypeSymbol, IRuntimeContext, JsExpression> CreateTaskCompletionSource { get; set; }
		public Func<JsExpression, JsExpression, IRuntimeContext, JsExpression> SetAsyncResult { get; set; }
		public Func<JsExpression, JsExpression, IRuntimeContext, JsExpression> SetAsyncException { get; set; }
		public Func<JsExpression, IRuntimeContext, JsExpression> GetTaskFromTaskCompletionSource { get; set; }
		public Func<JsExpression, JsExpression, IRuntimeContext, JsExpression> ApplyConstructor { get; set; }
		public Func<JsExpression, JsExpression, IRuntimeContext, JsExpression> ShallowCopy { get; set; }
		public Func<ISymbol, IRuntimeContext, JsExpression> GetMember { get; set; }
		public Func<INamedTypeSymbol, IRuntimeContext, JsExpression> GetAnonymousTypeInfo { get; set; }
		public Func<IEnumerable<Tuple<JsExpression, string>>, IRuntimeContext, JsExpression> GetTransparentTypeInfo { get; set; }
		public Func<string, JsExpression, ITypeSymbol, IRuntimeContext, JsExpression> GetExpressionForLocal { get; set; }
		public Func<JsExpression, ITypeSymbol, IRuntimeContext, JsExpression> CloneValueType { get; set; }
		public Func<JsExpression, string, ISymbol, JsExpression, IRuntimeContext, JsExpression> InitializeField { get; set; }

		private JsExpression GetScriptType(ITypeSymbol type, TypeContext context, Func<ITypeParameterSymbol, JsExpression> resolveTypeParameter) {
			string contextName = GetTypeContextShortName(context);
			if (type.IsAnonymousType) {
				return JsExpression.Identifier(contextName + "_$Anonymous");
			}
			else if (type.TypeKind == TypeKind.Array) {
				return JsExpression.Invoke(JsExpression.Identifier(contextName + "_$Array"), GetScriptType(((IArrayTypeSymbol)type).ElementType, TypeContext.GenericArgument, resolveTypeParameter));
			}
			else if (type is INamedTypeSymbol) {
				var nt = (INamedTypeSymbol)type;
				if (nt.IsUnboundGenericType) {
					return new JsTypeReferenceExpression(Common.CreateMockTypeDefinition(contextName + "_" + type.Name, Common.CreateMockAssembly()));
				}
				else {
					var allTypeArguments = nt.GetAllTypeArguments();
					if (allTypeArguments.Count > 0) {
						return JsExpression.Invoke(JsExpression.Identifier(contextName + "_$InstantiateGenericType"), new[] { new JsTypeReferenceExpression(Common.CreateMockTypeDefinition(type.Name, Common.CreateMockAssembly())) }.Concat(allTypeArguments.Select(a => GetScriptType(a, TypeContext.GenericArgument, resolveTypeParameter))));
					}
					else {
						return new JsTypeReferenceExpression(Common.CreateMockTypeDefinition(contextName + "_" + type.Name, Common.CreateMockAssembly()));
					}
				}
			}
			else if (type is ITypeParameterSymbol) {
				return resolveTypeParameter((ITypeParameterSymbol)type);
			}
			else {
				throw new ArgumentException("Unsupported type + " + type);
			}
		}

		JsExpression IRuntimeLibrary.TypeOf(ITypeSymbol type, IRuntimeContext context) {
			return GetTypeOf(type, context);
		}

		JsExpression IRuntimeLibrary.InstantiateType(ITypeSymbol type, IRuntimeContext context) {
			return InstantiateType(type, context);
		}

		JsExpression IRuntimeLibrary.InstantiateTypeForUseAsTypeArgumentInInlineCode(ITypeSymbol type, IRuntimeContext context) {
			return InstantiateTypeForUseAsTypeArgumentInInlineCode(type, context);
		}

		JsExpression IRuntimeLibrary.TypeIs(JsExpression expression, ITypeSymbol sourceType, ITypeSymbol targetType, IRuntimeContext context) {
			return TypeIs(expression, sourceType, targetType, context);
		}

		JsExpression IRuntimeLibrary.TryDowncast(JsExpression expression, ITypeSymbol sourceType, ITypeSymbol targetType, IRuntimeContext context) {
			return TryDowncast(expression, sourceType, targetType, context);
		}

		JsExpression IRuntimeLibrary.Downcast(JsExpression expression, ITypeSymbol sourceType, ITypeSymbol targetType, IRuntimeContext context) {
			return Downcast(expression, sourceType, targetType, context);
		}

		JsExpression IRuntimeLibrary.Upcast(JsExpression expression, ITypeSymbol sourceType, ITypeSymbol targetType, IRuntimeContext context) {
			return Upcast(expression, sourceType, targetType, context);
		}

		JsExpression IRuntimeLibrary.ReferenceEquals(JsExpression a, JsExpression b, IRuntimeContext context) {
			return ReferenceEquals(a, b, context);
		}

		JsExpression IRuntimeLibrary.ReferenceNotEquals(JsExpression a, JsExpression b, IRuntimeContext context) {
			return ReferenceNotEquals(a, b, context);
		}

		JsExpression IRuntimeLibrary.InstantiateGenericMethod(JsExpression type, IEnumerable<ITypeSymbol> typeArguments, IRuntimeContext context) {
			return InstantiateGenericMethod(type, typeArguments, context);
		}

		JsExpression IRuntimeLibrary.MakeException(JsExpression operand, IRuntimeContext context) {
			return MakeException(operand, context);
		}

		JsExpression IRuntimeLibrary.IntegerDivision(JsExpression numerator, JsExpression denominator, IRuntimeContext context) {
			return IntegerDivision(numerator, denominator, context);
		}

		JsExpression IRuntimeLibrary.NarrowingNumericConversion(JsExpression expression, ITypeSymbol sourceType, ITypeSymbol targetType, bool isChecked, IRuntimeContext context) {
			return NarrowingNumericConversion(expression, sourceType, targetType, isChecked, context);
		}

		JsExpression IRuntimeLibrary.EnumerationConversion(JsExpression expression, ITypeSymbol sourceType, ITypeSymbol targetType, bool isChecked, IRuntimeContext context) {
			return EnumerationConversion(expression, sourceType, targetType, isChecked, context);
		}

		JsExpression IRuntimeLibrary.Coalesce(JsExpression a, JsExpression b, IRuntimeContext context) {
			return Coalesce(a, b, context);
		}

		JsExpression IRuntimeLibrary.Lift(JsExpression expression, IRuntimeContext context) {
			return Lift(expression, context);
		}

		JsExpression IRuntimeLibrary.FromNullable(JsExpression expression, IRuntimeContext context) {
			return FromNullable(expression, context);
		}

		JsExpression IRuntimeLibrary.LiftedBooleanAnd(JsExpression a, JsExpression b, IRuntimeContext context) {
			return LiftedBooleanAnd(a, b, context);
		}

		JsExpression IRuntimeLibrary.LiftedBooleanOr(JsExpression a, JsExpression b, IRuntimeContext context) {
			return LiftedBooleanOr(a, b, context);
		}

		JsExpression IRuntimeLibrary.Bind(JsExpression function, JsExpression target, IRuntimeContext context) {
			return Bind(function, target, context);
		}

		JsExpression IRuntimeLibrary.BindFirstParameterToThis(JsExpression function, IRuntimeContext context) {
			return BindFirstParameterToThis(function, context);
		}

		JsExpression IRuntimeLibrary.Default(ITypeSymbol type, IRuntimeContext context) {
			return Default(type, context);
		}

		JsExpression IRuntimeLibrary.CreateArray(ITypeSymbol elementType, IEnumerable<JsExpression> size, IRuntimeContext context) {
			return CreateArray(elementType, size, context);
		}

		JsExpression IRuntimeLibrary.CloneDelegate(JsExpression source, ITypeSymbol sourceType, ITypeSymbol targetType, IRuntimeContext context) {
			return CloneDelegate(source, sourceType, targetType, context);
		}

		JsExpression IRuntimeLibrary.CallBase(IMethodSymbol method, IEnumerable<JsExpression> thisAndArguments, IRuntimeContext context) {
			return CallBase(method, thisAndArguments, context);
		}

		JsExpression IRuntimeLibrary.GetBasePropertyValue(IPropertySymbol property, JsExpression @this, IRuntimeContext context) {
			return GetBasePropertyValue(property, @this, context);
		}

		JsExpression IRuntimeLibrary.SetBasePropertyValue(IPropertySymbol property, JsExpression @this, JsExpression value, IRuntimeContext context) {
			return SetBasePropertyValue(property, @this, value, context);
		}

		JsExpression IRuntimeLibrary.BindBaseCall(IMethodSymbol method, JsExpression @this, IRuntimeContext context) {
			return BindBaseCall(method, @this, context);
		}

		JsExpression IRuntimeLibrary.MakeEnumerator(ITypeSymbol yieldType, JsExpression moveNext, JsExpression getCurrent, JsExpression dispose, IRuntimeContext context) {
			return MakeEnumerator(yieldType, moveNext, getCurrent, dispose, context);
		}

		JsExpression IRuntimeLibrary.MakeEnumerable(ITypeSymbol yieldType, JsExpression getEnumerator, IRuntimeContext context) {
			return MakeEnumerable(yieldType, getEnumerator, context);
		}

		JsExpression IRuntimeLibrary.GetMultiDimensionalArrayValue(JsExpression array, IEnumerable<JsExpression> indices, IRuntimeContext context) {
			return GetMultiDimensionalArrayValue(array, indices, context);
		}

		JsExpression IRuntimeLibrary.SetMultiDimensionalArrayValue(JsExpression array, IEnumerable<JsExpression> indices, JsExpression value, IRuntimeContext context) {
			return SetMultiDimensionalArrayValue(array, indices, value, context);
		}

		JsExpression IRuntimeLibrary.CreateTaskCompletionSource(ITypeSymbol taskGenericArgument, IRuntimeContext context) {
			return CreateTaskCompletionSource(taskGenericArgument, context);
		}

		JsExpression IRuntimeLibrary.SetAsyncResult(JsExpression taskCompletionSource, JsExpression value, IRuntimeContext context) {
			return SetAsyncResult(taskCompletionSource, value, context);
		}

		JsExpression IRuntimeLibrary.SetAsyncException(JsExpression taskCompletionSource, JsExpression exception, IRuntimeContext context) {
			return SetAsyncException(taskCompletionSource, exception, context);
		}

		JsExpression IRuntimeLibrary.GetTaskFromTaskCompletionSource(JsExpression taskCompletionSource, IRuntimeContext context) {
			return GetTaskFromTaskCompletionSource(taskCompletionSource, context);
		}

		JsExpression IRuntimeLibrary.ApplyConstructor(JsExpression constructor, JsExpression argumentsArray, IRuntimeContext context) {
			return ApplyConstructor(constructor, argumentsArray, context);
		}

		JsExpression IRuntimeLibrary.ShallowCopy(JsExpression source, JsExpression target, IRuntimeContext context) {
			return ShallowCopy(source, target, context);
		}

		JsExpression IRuntimeLibrary.GetMember(ISymbol member, IRuntimeContext context) {
			return GetMember(member, context);
		}

		JsExpression IRuntimeLibrary.GetAnonymousTypeInfo(INamedTypeSymbol anonymousType, IRuntimeContext context) {
			return GetAnonymousTypeInfo(anonymousType, context);
		}

		JsExpression IRuntimeLibrary.GetTransparentTypeInfo(IEnumerable<Tuple<JsExpression, string>> members, IRuntimeContext context) {
			return GetTransparentTypeInfo(members, context);
		}

		JsExpression IRuntimeLibrary.GetExpressionForLocal(string name, JsExpression accessor, ITypeSymbol type, IRuntimeContext context) {
			return GetExpressionForLocal(name, accessor, type, context);
		}

		JsExpression IRuntimeLibrary.CloneValueType(JsExpression value, ITypeSymbol type, IRuntimeContext context) {
			return CloneValueType(value, type, context);
		}

		JsExpression IRuntimeLibrary.InitializeField(JsExpression jsMember, string scriptName, ISymbol member, JsExpression initialValue, IRuntimeContext context) {
			return InitializeField(jsMember, scriptName, member, initialValue, context);
		}
	}
}