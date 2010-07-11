/*   Copyright 2009 - 2010 Marcus Bratton

     Licensed under the Apache License, Version 2.0 (the "License");
     you may not use this file except in compliance with the License.
     You may obtain a copy of the License at

     http://www.apache.org/licenses/LICENSE-2.0

     Unless required by applicable law or agreed to in writing, software
     distributed under the License is distributed on an "AS IS" BASIS,
     WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
     See the License for the specific language governing permissions and
     limitations under the License.
*/

using System;
using System.Linq.Expressions;
using System.Reflection;
using Siege.DynamicTypeGeneration;
using Siege.Requisitions.AOP.Attributes;

namespace Siege.Requisitions.AOP.Interceptors.Methods.ProcessEncapsulating
{
	public class DefaultProcessEncapsulatingInterceptionStrategy : IProcessEncapsulatingInterceptionStrategy
	{
		public void Intercept(MethodInfo methodInfo, object attribute, GeneratedVariable processor, GeneratedVariable variable, GeneratedVariable encapsulating)
		{
			var defaultAttribute = attribute as IDefaultProcessEncapsulatingAttribute;

			var funcProcessor = GetMethodInfo(() => defaultAttribute.Process<object>(null), methodInfo.ReturnType);
			if(processor != null)
				variable.AssignFrom(() => encapsulating.Invoke(funcProcessor, processor));
			else
				variable.AssignFrom(() => encapsulating.Invoke(funcProcessor));
		}

		private MethodInfo GetMethodInfo(Expression<Action> method, Type returnType)
		{
			var body = method.Body as MethodCallExpression;
			var bodyMethod = body.Method;

			if (bodyMethod.IsGenericMethod)
			{
				bodyMethod = bodyMethod.GetGenericMethodDefinition().MakeGenericMethod(returnType);
			}

			return bodyMethod;
		}
	}
}