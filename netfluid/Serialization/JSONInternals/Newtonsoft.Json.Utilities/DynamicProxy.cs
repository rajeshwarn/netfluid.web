using System;
using System.Collections.Generic;
using System.Dynamic;
namespace Newtonsoft.Json.Utilities
{
	internal class DynamicProxy<T>
	{
		internal virtual IEnumerable<string> GetDynamicMemberNames(T instance)
		{
			return new string[0];
		}
		internal virtual bool TryBinaryOperation(T instance, BinaryOperationBinder binder, object arg, out object result)
		{
			result = null;
			return false;
		}
		internal virtual bool TryConvert(T instance, ConvertBinder binder, out object result)
		{
			result = null;
			return false;
		}
		internal virtual bool TryCreateInstance(T instance, CreateInstanceBinder binder, object[] args, out object result)
		{
			result = null;
			return false;
		}
		internal virtual bool TryDeleteIndex(T instance, DeleteIndexBinder binder, object[] indexes)
		{
			return false;
		}
		internal virtual bool TryDeleteMember(T instance, DeleteMemberBinder binder)
		{
			return false;
		}
		internal virtual bool TryGetIndex(T instance, GetIndexBinder binder, object[] indexes, out object result)
		{
			result = null;
			return false;
		}
		internal virtual bool TryGetMember(T instance, GetMemberBinder binder, out object result)
		{
			result = null;
			return false;
		}
		internal virtual bool TryInvoke(T instance, InvokeBinder binder, object[] args, out object result)
		{
			result = null;
			return false;
		}
		internal virtual bool TryInvokeMember(T instance, InvokeMemberBinder binder, object[] args, out object result)
		{
			result = null;
			return false;
		}
		internal virtual bool TrySetIndex(T instance, SetIndexBinder binder, object[] indexes, object value)
		{
			return false;
		}
		internal virtual bool TrySetMember(T instance, SetMemberBinder binder, object value)
		{
			return false;
		}
		internal virtual bool TryUnaryOperation(T instance, UnaryOperationBinder binder, out object result)
		{
			result = null;
			return false;
		}
	}
}
