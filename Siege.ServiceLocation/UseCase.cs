using System;
using System.Collections;
using System.Collections.Generic;

namespace Siege.ServiceLocation
{
    public abstract class UseCase<TBaseType, TOutput> : IUseCase<TBaseType>
    {
        protected readonly List<IActivationRule> rules = new List<IActivationRule>();
        public abstract TOutput GetBinding();
        protected abstract IActivationStrategy GetActivationStrategy();

        public void AddActivationRule(IActivationRule rule)
        {
            rules.Add(rule);
        }

        public object Resolve(IServiceLocator locator, IList<object> context, IDictionary constructorArguments) 
        {
            foreach (IActivationRule rule in this.rules)
            {
                foreach (object contextItem in context)
                {
                    if (rule.Evaluate(contextItem)) return GetActivationStrategy().Resolve(locator, constructorArguments);
                }
            }

            return default(TBaseType);
        }

        public object Resolve(IServiceLocator locator, IDictionary constructorArguments)
        {
            return GetActivationStrategy().Resolve(locator, constructorArguments);
        }

        public abstract Type GetBoundType();

        protected interface IActivationStrategy
        {
            TBaseType Resolve(IServiceLocator locator, IDictionary constructorArguments);
        }
    }
}