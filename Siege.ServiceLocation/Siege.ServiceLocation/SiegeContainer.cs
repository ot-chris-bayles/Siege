﻿/*   Copyright 2009 - 2010 Marcus Bratton

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
using System.Collections;
using System.Collections.Generic;
using Siege.ServiceLocation.Bindings;
using Siege.ServiceLocation.Bindings.Action;
using Siege.ServiceLocation.Bindings.Conditional;
using Siege.ServiceLocation.Bindings.Default;
using Siege.ServiceLocation.Bindings.Named;
using Siege.ServiceLocation.Bindings.OpenGenerics;
using Siege.ServiceLocation.EventHandlers;
using Siege.ServiceLocation.Exceptions;
using Siege.ServiceLocation.Resolution;
using Siege.ServiceLocation.Stores;
using Siege.ServiceLocation.Stores.UseCases;
using Siege.ServiceLocation.Syntax;
using Siege.ServiceLocation.TypeBuilders;
using Siege.ServiceLocation.UseCases;
using Siege.ServiceLocation.UseCases.Actions;
using Siege.ServiceLocation.UseCases.Conditional;
using Siege.ServiceLocation.UseCases.Default;
using Siege.ServiceLocation.ExtensionMethods;

namespace Siege.ServiceLocation
{
    public class SiegeContainer : IContextualServiceLocator, ITypeResolver
    {
        private IServiceLocatorAdapter serviceLocator;
        private IServiceLocatorStore store;
        private UseCaseStore useCaseStore = new UseCaseStore();
        private readonly Hashtable factories = new Hashtable();

		public event TypeResolvedEventHandler TypeResolved;

        public SiegeContainer(IServiceLocatorAdapter serviceLocator, IServiceLocatorStore store, ITypeBuilder typeBuilder)
        {
            this.serviceLocator = serviceLocator;
            this.store = store;

			this.store.ExecutionStore.WireEvent(this);
            TypeHandler.Initialize(typeBuilder);

            serviceLocator.RegisterInstance(typeof(IServiceLocatorAdapter), serviceLocator);
            
            AddBinding<DefaultUseCaseBinding>();
            AddBinding<ConditionalUseCaseBinding>();
            AddBinding<NamedUseCaseBinding>();
            AddBinding<OpenGenericUseCaseBinding>();
            AddBinding<ActionUseCaseBinding>();

            Register(Given<IFactoryFetcher>.Then(this));
            Register(Given<IServiceLocator>.Then(this));
            Register(Given<IContextualServiceLocator>.Then(this));
        }

        public SiegeContainer(IServiceLocatorAdapter serviceLocator, IServiceLocatorStore store) : this(serviceLocator, store, new DefaultTypeBuilder())
        {
            
        }

        public void AddContext(object contextItem)
        {
            store.ContextStore.Add(contextItem);
        }

        private void AddBinding<TBinding>()
        {
			serviceLocator.Register(typeof(TBinding), typeof(TBinding));
        }

        public TService GetInstance<TService>()
        {
            return GetInstance<TService>(typeof (TService), new IResolutionArgument[] {});
        }

        public TService GetInstance<TService>(params IResolutionArgument[] arguments)
        {
            return GetInstance<TService>(typeof (TService), arguments);
        }

        public TService GetInstance<TService>(Type type, params IResolutionArgument[] arguments)
        {
            return (TService) GetInstance(type, arguments);
        }

        public object GetInstance(Type type)
        {
            return GetInstance(type, new IResolutionArgument[] {});
        }

        public object GetInstance(Type type, params IResolutionArgument[] arguments)
		{
            this.store.ResolutionStore.Add(new List<IResolutionArgument>(arguments));

            IList<IUseCase> selectedCase = this.useCaseStore.Conditional.ResolutionCases.GetUseCasesForType(type);

            if (selectedCase != null)
            {
                for (int i = 0; i < selectedCase.Count; i++)
                {
                    var useCase = selectedCase[i];
                    var value = Resolve((IGenericUseCase)useCase, new ConditionalResolutionStrategy(serviceLocator, this.store));

                    if (value != null)
                    {
                        return value;
                    }
                }
            }
            else if (this.useCaseStore.Default.ResolutionCases.Contains(type))
            {
                var useCase = (IGenericUseCase) this.useCaseStore.Default.ResolutionCases.GetUseCaseForType(type);
                var value = Resolve(useCase, new DefaultResolutionStrategy(serviceLocator, this.store));

                if (value != null)
                {
                    return value;
                }
            }

            if (HasTypeRegistered(type) || type.IsGenericType)
            {
                return serviceLocator.GetInstance(type, this.store.ResolutionStore.Items.OfType<ConstructorParameter,IResolutionArgument>());
            }

            throw new RegistrationNotFoundException(type);
        }

        public TService GetInstance<TService>(string key)
        {
            return (TService) GetInstance(typeof (TService), key, new IResolutionArgument[] {});
        }

        public TService GetInstance<TService>(string key, params IResolutionArgument[] arguments)
        {
            return (TService) GetInstance(typeof (TService), key, arguments);
        }

        public object GetInstance(Type type, string key, params IResolutionArgument[] arguments)
        {
			this.store.ResolutionStore.Add(new List<IResolutionArgument>(arguments));

            return serviceLocator.GetInstance(type, key, this.store.ResolutionStore.Items.OfType<ConstructorParameter, IResolutionArgument>());
        }

        public object GetService(Type serviceType)
        {
            return GetInstance(serviceType, new IResolutionArgument[] {});
        }

        public object GetInstance(Type type, string key)
        {
            return GetInstance(type, key, new IResolutionArgument[] {});
        }

        public IEnumerable<object> GetAllInstances(Type serviceType)
        {
            return serviceLocator.GetAllInstances(serviceType);
        }

        public IEnumerable<TService> GetAllInstances<TService>()
        {
            return serviceLocator.GetAllInstances<TService>();
        }

        public bool HasTypeRegistered(Type type)
        {
            return serviceLocator.HasTypeRegistered(type);
        }

        public IServiceLocator Register(IUseCase useCase)
        {
            if (useCase is IDefaultActionUseCase)
            {
                this.useCaseStore.Default.PostResolutionCases.Add(useCase.GetBoundType(), useCase);
            }
            else if (useCase is IConditionalActionUseCase)
            {
                this.useCaseStore.Conditional.PostResolutionCases.Add(useCase.GetBoundType(), useCase);
            }
            else if (useCase is IDefaultUseCase)
            {
                this.useCaseStore.Default.ResolutionCases.Add(useCase.GetBaseBindingType(), useCase);
            }
            else
            {
                this.useCaseStore.Conditional.ResolutionCases.Add(useCase.GetBaseBindingType(), useCase);
            }

            Type bindingType = useCase.GetUseCaseBindingType().IsGenericType ? 
                useCase.GetUseCaseBindingType().MakeGenericType(useCase.GetBaseBindingType()) 
                : useCase.GetUseCaseBindingType();

            if (useCase is IInstanceUseCase)
            {
                var binding = GetInstance<IInstanceUseCaseBinding>(bindingType);
                binding.BindInstance((IInstanceUseCase) useCase, this);
            }
            else
            {
                var binding = GetInstance<IUseCaseBinding>(bindingType);
                binding.Bind(useCase, this);
            }

            return this;
        }

        public IServiceLocatorStore Store
        {
            get { return this.store; }
        }

        public void Dispose()
        {
            serviceLocator.Dispose();
            this.store.Dispose();
        }

        private void RaiseTypeResolvedEvent(Type type)
        {
            if (this.TypeResolved != null) this.TypeResolved(type);
        }

        private object Resolve(IGenericUseCase useCase, IResolutionStrategy strategy)
        {
            var value = useCase.Resolve(strategy, this.store);

            if (value != null)
            {
                RaiseTypeResolvedEvent(useCase.GetBoundType());
                ExecutePostConditions(this.useCaseStore.Default, useCase, actionUseCase => value = actionUseCase.Invoke(value));
                ExecutePostConditions(this.useCaseStore.Conditional, useCase, actionUseCase =>
				{
					if (actionUseCase.IsValid(this.store)) 
						value = actionUseCase.Invoke(value);
				});
            }

            return value;
        }

        private void ExecutePostConditions(UseCaseGroup useCaseGroup, IUseCase useCase,
                                           Action<IActionUseCase> action)
        {
            IList<IUseCase> actions = useCaseGroup.PostResolutionCases.GetUseCasesForType(useCase.GetBoundType()) ??
                                      useCaseGroup.PostResolutionCases.GetUseCasesForType(useCase.GetBaseBindingType());

            if (actions != null)
            {
                for (int i = 0; i < actions.Count; i++)
                {
                    var actionUseCase = actions[i];
                    action((IActionUseCase)actionUseCase);
                }
            }
        }

        IGenericFactory IFactoryFetcher.GetFactory(Type type)
        {
            if (!factories.ContainsKey(type))
            {
                lock (factories.SyncRoot)
                {
                    if (!factories.ContainsKey(type))
                    {
                        var factory = new Factory(this);
                        Register(Given<Factory>.Then("Factory" + type, factory));

                        factories.Add(type, factory);
                    }
                }
            }

            return (Factory) factories[type];
        }
    }
}