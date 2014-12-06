using Newtonsoft.Json.Utilities;
using System;
using System.Dynamic;
using System.Runtime.CompilerServices;
namespace Newtonsoft.Json.Serialization
{
	internal class JsonDynamicContract : JsonContainerContract
	{
		private readonly ThreadSafeStore<string, CallSite<Func<CallSite, object, object>>> _callSiteGetters = new ThreadSafeStore<string, CallSite<Func<CallSite, object, object>>>(new Func<string, CallSite<Func<CallSite, object, object>>>(JsonDynamicContract.CreateCallSiteGetter));
		private readonly ThreadSafeStore<string, CallSite<Func<CallSite, object, object, object>>> _callSiteSetters = new ThreadSafeStore<string, CallSite<Func<CallSite, object, object, object>>>(new Func<string, CallSite<Func<CallSite, object, object, object>>>(JsonDynamicContract.CreateCallSiteSetter));
		internal JsonPropertyCollection Properties
		{
			get;
			private set;
		}
		internal Func<string, string> PropertyNameResolver
		{
			get;
			set;
		}
		private static CallSite<Func<CallSite, object, object>> CreateCallSiteGetter(string name)
		{
			GetMemberBinder getMemberBinder = (GetMemberBinder)DynamicUtils.BinderWrapper.GetMember(name, typeof(DynamicUtils));
			return CallSite<Func<CallSite, object, object>>.Create(new NoThrowGetBinderMember(getMemberBinder));
		}
		private static CallSite<Func<CallSite, object, object, object>> CreateCallSiteSetter(string name)
		{
			SetMemberBinder binder = (SetMemberBinder)DynamicUtils.BinderWrapper.SetMember(name, typeof(DynamicUtils));
			return CallSite<Func<CallSite, object, object, object>>.Create(new NoThrowSetBinderMember(binder));
		}
		internal JsonDynamicContract(Type underlyingType) : base(underlyingType)
		{
			this.ContractType = JsonContractType.Dynamic;
			this.Properties = new JsonPropertyCollection(base.UnderlyingType);
		}
		internal bool TryGetMember(IDynamicMetaObjectProvider dynamicProvider, string name, out object value)
		{
			ValidationUtils.ArgumentNotNull(dynamicProvider, "dynamicProvider");
			CallSite<Func<CallSite, object, object>> callSite = this._callSiteGetters.Get(name);
			object result = callSite.Target(callSite, dynamicProvider);
			if (!object.ReferenceEquals(result, NoThrowExpressionVisitor.ErrorResult))
			{
				value = result;
				return true;
			}
			value = null;
			return false;
		}
		internal bool TrySetMember(IDynamicMetaObjectProvider dynamicProvider, string name, object value)
		{
			ValidationUtils.ArgumentNotNull(dynamicProvider, "dynamicProvider");
			CallSite<Func<CallSite, object, object, object>> callSite = this._callSiteSetters.Get(name);
			object result = callSite.Target(callSite, dynamicProvider, value);
			return !object.ReferenceEquals(result, NoThrowExpressionVisitor.ErrorResult);
		}
	}
}
