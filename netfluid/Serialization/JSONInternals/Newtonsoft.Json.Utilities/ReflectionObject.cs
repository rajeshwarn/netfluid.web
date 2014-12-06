using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
namespace Newtonsoft.Json.Utilities
{
	internal class ReflectionObject
	{
		internal ObjectConstructor<object> Creator
		{
			get;
			private set;
		}
		internal IDictionary<string, ReflectionMember> Members
		{
			get;
			private set;
		}
		internal ReflectionObject()
		{
			this.Members = new Dictionary<string, ReflectionMember>();
		}
		internal object GetValue(object target, string member)
		{
			Func<object, object> getter = this.Members[member].Getter;
			return getter(target);
		}
		internal void SetValue(object target, string member, object value)
		{
			Action<object, object> setter = this.Members[member].Setter;
			setter(target, value);
		}
		internal Type GetType(string member)
		{
			return this.Members[member].MemberType;
		}
		internal static ReflectionObject Create(Type t, params string[] memberNames)
		{
			return ReflectionObject.Create(t, null, memberNames);
		}
		internal static ReflectionObject Create(Type t, MethodBase creator, params string[] memberNames)
		{
			ReflectionObject d = new ReflectionObject();
			ReflectionDelegateFactory delegateFactory = JsonTypeReflector.ReflectionDelegateFactory;
			if (creator != null)
			{
				d.Creator = delegateFactory.CreateParametrizedConstructor(creator);
			}
			else
			{
				if (ReflectionUtils.HasDefaultConstructor(t, false))
				{
					Func<object> ctor = delegateFactory.CreateDefaultConstructor<object>(t);
					d.Creator = ((object[] args) => ctor());
				}
			}
			int i = 0;
			while (i < memberNames.Length)
			{
				string memberName = memberNames[i];
				MemberInfo[] members = t.GetMember(memberName, BindingFlags.Instance | BindingFlags.Public);
				if (members.Length != 1)
				{
					throw new ArgumentException("Expected a single member with the name '{0}'.".FormatWith(CultureInfo.InvariantCulture, memberName));
				}
				MemberInfo member = members.Single<MemberInfo>();
				ReflectionMember reflectionMember = new ReflectionMember();
				switch (member.MemberType())
				{
				case MemberTypes.Property:
				case MemberTypes.Field:
					if (ReflectionUtils.CanReadMemberValue(member, false))
					{
						reflectionMember.Getter = delegateFactory.CreateGet<object>(member);
					}
					if (ReflectionUtils.CanSetMemberValue(member, false, false))
					{
						reflectionMember.Setter = delegateFactory.CreateSet<object>(member);
					}
					break;
				case MemberTypes.Event:
					goto IL_19E;
				case MemberTypes.Method:
				{
					MethodInfo method = (MethodInfo)member;
					if (method.IsPublic)
					{
						ParameterInfo[] parameters = method.GetParameters();
						if (parameters.Length == 0 && method.ReturnType != typeof(void))
						{
							MethodCall<object, object> call = delegateFactory.CreateMethodCall<object>(method);
							reflectionMember.Getter = ((object target) => call(target, new object[0]));
						}
						else
						{
							if (parameters.Length == 1 && method.ReturnType == typeof(void))
							{
								MethodCall<object, object> call = delegateFactory.CreateMethodCall<object>(method);
								reflectionMember.Setter = delegate(object target, object arg)
								{
									call(target, new object[]
									{
										arg
									});
								};
							}
						}
					}
					break;
				}
				default:
					goto IL_19E;
				}
				if (ReflectionUtils.CanReadMemberValue(member, false))
				{
					reflectionMember.Getter = delegateFactory.CreateGet<object>(member);
				}
				if (ReflectionUtils.CanSetMemberValue(member, false, false))
				{
					reflectionMember.Setter = delegateFactory.CreateSet<object>(member);
				}
				reflectionMember.MemberType = ReflectionUtils.GetMemberUnderlyingType(member);
				d.Members[memberName] = reflectionMember;
				i++;
				continue;
				IL_19E:
				throw new ArgumentException("Unexpected member type '{0}' for member '{1}'.".FormatWith(CultureInfo.InvariantCulture, member.MemberType(), member.Name));
			}
			return d;
		}
	}
}
